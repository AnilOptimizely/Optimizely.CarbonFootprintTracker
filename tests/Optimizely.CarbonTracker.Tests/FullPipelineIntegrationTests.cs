using Microsoft.Extensions.Logging;
using Moq;
using Optimizely.CarbonTracker.Analysis;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Integration;

public class FullPipelineIntegrationTests
{
    [Fact]
    public async Task GenerateReport_WithMockPage_ProducesCorrectReport()
    {
        // Arrange — simulate a page with mixed asset types
        var contentGuid = Guid.NewGuid();
        var pageUrl = "https://example.com/test-page";

        var analysisResult = new PageAnalysisResult
        {
            PageUrl = pageUrl,
            AnalyzedAt = DateTime.UtcNow,
            Success = true,
            TotalTransferSizeBytes = 1_200_000, // ~1.2MB
            Resources = new List<DiscoveredResource>
            {
                new() { Url = pageUrl, Category = AssetCategory.HTML, TransferSizeBytes = 50_000 },
                new() { Url = "https://example.com/styles.css", Category = AssetCategory.CSS, TransferSizeBytes = 30_000 },
                new() { Url = "https://example.com/app.js", Category = AssetCategory.JavaScript, TransferSizeBytes = 120_000 },
                new() { Url = "https://example.com/hero.jpg", Category = AssetCategory.Images, TransferSizeBytes = 800_000 },
                new() { Url = "https://example.com/font.woff2", Category = AssetCategory.Fonts, TransferSizeBytes = 200_000 }
            },
            Suggestions = new List<OptimizationSuggestion>
            {
                new()
                {
                    Severity = SuggestionSeverity.High,
                    Title = "Convert images to modern formats (WebP/AVIF)",
                    Description = "Test suggestion",
                    PotentialSavingsBytes = 320_000,
                    PotentialCO2SavingsGrams = 0.1
                }
            }
        };

        var mockPageAnalyzer = new Mock<IPageAnalyzer>();
        mockPageAnalyzer.Setup(p => p.AnalyzeAsync(pageUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        var calculator = new CarbonCalculator(
            Microsoft.Extensions.Options.Options.Create(new Configuration.CarbonTrackerOptions()));

        var logger = Mock.Of<ILogger<CarbonReportService>>();
        var reportService = new CarbonReportService(mockPageAnalyzer.Object, calculator, logger);

        // Act
        var report = await reportService.GenerateReportAsync(pageUrl, contentGuid);

        // Assert — report shape
        Assert.Equal(contentGuid, report.ContentGuid);
        Assert.Equal(pageUrl, report.PageUrl);
        Assert.True(report.EstimatedCO2Grams > 0);
        Assert.NotEqual(GreenScore.A, report.Score); // 1.2MB should not be A
        Assert.True(report.Assets.Count >= 5); // HTML, CSS, JS, Images, Fonts
        Assert.NotEmpty(report.Suggestions);

        // Assert — asset breakdown covers all categories
        Assert.Contains(report.Assets, a => a.Category == AssetCategory.HTML);
        Assert.Contains(report.Assets, a => a.Category == AssetCategory.CSS);
        Assert.Contains(report.Assets, a => a.Category == AssetCategory.JavaScript);
        Assert.Contains(report.Assets, a => a.Category == AssetCategory.Images);
        Assert.Contains(report.Assets, a => a.Category == AssetCategory.Fonts);

        // Assert — percentages sum to ~100%
        var totalPercentage = report.Assets.Sum(a => a.Percentage);
        Assert.InRange(totalPercentage, 99.0, 101.0);

        // Assert — Images should be the largest category
        var imageAsset = report.Assets.First(a => a.Category == AssetCategory.Images);
        Assert.True(imageAsset.Percentage > 50);
    }

    [Fact]
    public async Task PersistenceRoundtrip_SaveAndRetrieve_WorksCorrectly()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        mockCalculator.Setup(c => c.CalculateGreenScore(It.IsAny<double>()))
            .Returns(GreenScore.B);

        var repository = new CarbonReportRepository(mockCalculator.Object);
        var contentGuid = Guid.NewGuid();

        var report = new PageCarbonReport
        {
            ContentGuid = contentGuid,
            PageUrl = "https://example.com/test",
            AnalyzedAt = DateTime.UtcNow,
            TotalTransferSizeBytes = 500_000,
            EstimatedCO2Grams = 0.35,
            Score = GreenScore.B,
            Assets = new List<AssetBreakdown>
            {
                new() { Category = AssetCategory.HTML, TransferSizeBytes = 50_000, Percentage = 10 },
                new() { Category = AssetCategory.Images, TransferSizeBytes = 450_000, Percentage = 90 }
            },
            Suggestions = new List<OptimizationSuggestion>
            {
                new() { Severity = SuggestionSeverity.Medium, Title = "Test suggestion" }
            }
        };

        // Act — save and retrieve
        await repository.SaveReportAsync(report);
        var retrieved = await repository.GetLatestReportAsync(contentGuid);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(contentGuid, retrieved!.ContentGuid);
        Assert.Equal("https://example.com/test", retrieved.PageUrl);
        Assert.Equal(0.35, retrieved.EstimatedCO2Grams);
        Assert.Equal(GreenScore.B, retrieved.Score);
        Assert.Equal(2, retrieved.Assets.Count);
        Assert.Single(retrieved.Suggestions);
        Assert.True(retrieved.Id > 0);
    }

    [Fact]
    public async Task FullPipeline_AnalyzeAndPersist_EndToEnd()
    {
        // Arrange
        var contentGuid = Guid.NewGuid();
        var pageUrl = "https://example.com/full-test";

        var analysisResult = new PageAnalysisResult
        {
            PageUrl = pageUrl,
            AnalyzedAt = DateTime.UtcNow,
            Success = true,
            TotalTransferSizeBytes = 500_000,
            Resources = new List<DiscoveredResource>
            {
                new() { Url = pageUrl, Category = AssetCategory.HTML, TransferSizeBytes = 50_000 },
                new() { Url = "https://example.com/style.css", Category = AssetCategory.CSS, TransferSizeBytes = 20_000 },
                new() { Url = "https://example.com/photo.jpg", Category = AssetCategory.Images, TransferSizeBytes = 430_000 }
            },
            Suggestions = new List<OptimizationSuggestion>()
        };

        var mockPageAnalyzer = new Mock<IPageAnalyzer>();
        mockPageAnalyzer.Setup(p => p.AnalyzeAsync(pageUrl, It.IsAny<CancellationToken>()))
            .ReturnsAsync(analysisResult);

        var calculator = new CarbonCalculator(
            Microsoft.Extensions.Options.Options.Create(new Configuration.CarbonTrackerOptions()));

        var logger = Mock.Of<ILogger<CarbonReportService>>();
        var reportService = new CarbonReportService(mockPageAnalyzer.Object, calculator, logger);

        var repository = new CarbonReportRepository(calculator);

        // Act — generate report and persist
        var report = await reportService.GenerateReportAsync(pageUrl, contentGuid);
        await repository.SaveReportAsync(report);

        // Retrieve and verify
        var retrieved = await repository.GetLatestReportAsync(contentGuid);
        var history = await repository.GetHistoryAsync(contentGuid, days: 30);
        var summary = await repository.GetSiteSummaryAsync();

        // Assert — retrieved report matches
        Assert.NotNull(retrieved);
        Assert.Equal(report.EstimatedCO2Grams, retrieved!.EstimatedCO2Grams);
        Assert.Equal(report.Score, retrieved.Score);

        // Assert — history contains report
        Assert.Single(history);

        // Assert — site summary reflects the report
        Assert.Equal(1, summary.TotalPagesAnalyzed);
        Assert.Equal(report.EstimatedCO2Grams, summary.AverageCO2PerPage);
    }

    [Fact]
    public async Task GenerateReport_FailedAnalysis_ThrowsException()
    {
        // Arrange
        var mockPageAnalyzer = new Mock<IPageAnalyzer>();
        mockPageAnalyzer.Setup(p => p.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageAnalysisResult
            {
                Success = false,
                ErrorMessage = "Connection timeout"
            });

        var calculator = new CarbonCalculator(
            Microsoft.Extensions.Options.Options.Create(new Configuration.CarbonTrackerOptions()));

        var logger = Mock.Of<ILogger<CarbonReportService>>();
        var reportService = new CarbonReportService(mockPageAnalyzer.Object, calculator, logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => reportService.GenerateReportAsync("https://example.com/fail", Guid.NewGuid()));

        Assert.Contains("Connection timeout", exception.Message);
    }
}
