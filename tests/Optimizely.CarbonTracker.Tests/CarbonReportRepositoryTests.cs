using Moq;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Persistence;

public class CarbonReportRepositoryTests
{
    private readonly Mock<ICarbonCalculator> _mockCalculator;
    private readonly CarbonReportRepository _repository;

    public CarbonReportRepositoryTests()
    {
        _mockCalculator = new Mock<ICarbonCalculator>();
        _mockCalculator.Setup(c => c.CalculateGreenScore(It.IsAny<double>()))
            .Returns(GreenScore.C);
        _repository = new CarbonReportRepository(_mockCalculator.Object);
    }

    [Fact]
    public async Task SaveReport_AssignsId()
    {
        var report = CreateReport(Guid.NewGuid());

        await _repository.SaveReportAsync(report);

        Assert.True(report.Id > 0);
    }

    [Fact]
    public async Task GetLatestReport_ReturnsLatest()
    {
        var guid = Guid.NewGuid();
        var older = CreateReport(guid, DateTime.UtcNow.AddHours(-2));
        var newer = CreateReport(guid, DateTime.UtcNow);

        await _repository.SaveReportAsync(older);
        await _repository.SaveReportAsync(newer);

        var result = await _repository.GetLatestReportAsync(guid);

        Assert.NotNull(result);
        Assert.Equal(newer.AnalyzedAt, result!.AnalyzedAt);
    }

    [Fact]
    public async Task GetLatestReport_ReturnsNull_WhenNotFound()
    {
        var result = await _repository.GetLatestReportAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetHistory_ReturnsReportsWithinDateRange()
    {
        var guid = Guid.NewGuid();
        var old = CreateReport(guid, DateTime.UtcNow.AddDays(-60));
        var recent = CreateReport(guid, DateTime.UtcNow.AddDays(-5));
        var today = CreateReport(guid, DateTime.UtcNow);

        await _repository.SaveReportAsync(old);
        await _repository.SaveReportAsync(recent);
        await _repository.SaveReportAsync(today);

        var history = await _repository.GetHistoryAsync(guid, days: 30);

        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task GetSiteSummary_AggregatesCorrectly()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var report1 = CreateReport(guid1, co2: 0.5);
        var report2 = CreateReport(guid2, co2: 1.5);

        await _repository.SaveReportAsync(report1);
        await _repository.SaveReportAsync(report2);

        var summary = await _repository.GetSiteSummaryAsync();

        Assert.Equal(2, summary.TotalPagesAnalyzed);
        Assert.Equal(1.0, summary.AverageCO2PerPage);
        Assert.Equal(2.0, summary.TotalEstimatedCO2Grams);
    }

    [Fact]
    public async Task CleanupOldReports_RemovesExpiredReports()
    {
        var guid = Guid.NewGuid();
        var old = CreateReport(guid, DateTime.UtcNow.AddDays(-400));
        var recent = CreateReport(guid, DateTime.UtcNow);

        await _repository.SaveReportAsync(old);
        await _repository.SaveReportAsync(recent);

        await _repository.CleanupOldReportsAsync(365);

        var history = await _repository.GetHistoryAsync(guid, days: 500);
        Assert.Single(history);
    }

    [Fact]
    public async Task GetAllTrackedContentGuids_ReturnsDistinctGuids()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        await _repository.SaveReportAsync(CreateReport(guid1));
        await _repository.SaveReportAsync(CreateReport(guid1));
        await _repository.SaveReportAsync(CreateReport(guid2));

        var guids = await _repository.GetAllTrackedContentGuidsAsync();

        Assert.Equal(2, guids.Count);
        Assert.Contains(guid1, guids);
        Assert.Contains(guid2, guids);
    }

    private static PageCarbonReport CreateReport(Guid contentGuid, DateTime? analyzedAt = null, double co2 = 0.5)
    {
        return new PageCarbonReport
        {
            ContentGuid = contentGuid,
            PageUrl = "https://example.com/page",
            AnalyzedAt = analyzedAt ?? DateTime.UtcNow,
            TotalTransferSizeBytes = 500_000,
            EstimatedCO2Grams = co2,
            Score = GreenScore.B,
            Assets = new List<AssetBreakdown>(),
            Suggestions = new List<OptimizationSuggestion>()
        };
    }
}
