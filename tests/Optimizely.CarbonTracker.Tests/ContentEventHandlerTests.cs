using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Optimizely.CarbonTracker.Configuration;
using Optimizely.CarbonTracker.Initialization;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Initialization;

public class ContentEventHandlerTests
{
    private readonly Mock<ICarbonReportService> _mockReportService;
    private readonly Mock<ICarbonReportRepository> _mockRepository;
    private readonly ContentEventHandler _handler;

    public ContentEventHandlerTests()
    {
        _mockReportService = new Mock<ICarbonReportService>();
        _mockRepository = new Mock<ICarbonReportRepository>();
        var options = Options.Create(new CarbonTrackerOptions { EnableRealTimeAnalysis = true });
        var logger = Mock.Of<ILogger<ContentEventHandler>>();
        _handler = new ContentEventHandler(_mockReportService.Object, _mockRepository.Object, options, logger);
    }

    [Fact]
    public async Task OnContentPublished_GeneratesAndSavesReport()
    {
        var guid = Guid.NewGuid();
        var url = "https://example.com/page";
        var report = new PageCarbonReport
        {
            ContentGuid = guid,
            PageUrl = url,
            EstimatedCO2Grams = 0.3,
            Score = GreenScore.B
        };

        _mockReportService.Setup(s => s.GenerateReportAsync(url, guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        await _handler.OnContentPublishedAsync(guid, url);

        _mockReportService.Verify(s => s.GenerateReportAsync(url, guid, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveReportAsync(report, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnContentPublished_DisabledRealTime_DoesNotAnalyze()
    {
        var disabledOptions = Options.Create(new CarbonTrackerOptions { EnableRealTimeAnalysis = false });
        var handler = new ContentEventHandler(
            _mockReportService.Object,
            _mockRepository.Object,
            disabledOptions,
            Mock.Of<ILogger<ContentEventHandler>>());

        await handler.OnContentPublishedAsync(Guid.NewGuid(), "https://example.com");

        _mockReportService.Verify(s => s.GenerateReportAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CheckCarbonThreshold_NoReport_ApprovesbyDefault()
    {
        _mockRepository.Setup(r => r.GetLatestReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PageCarbonReport?)null);

        var result = await _handler.CheckCarbonThresholdAsync(Guid.NewGuid());

        Assert.True(result.Approved);
    }

    [Fact]
    public async Task CheckCarbonThreshold_GoodScore_Approves()
    {
        var guid = Guid.NewGuid();
        var report = new PageCarbonReport
        {
            ContentGuid = guid,
            Score = GreenScore.B,
            EstimatedCO2Grams = 0.4
        };

        _mockRepository.Setup(r => r.GetLatestReportAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _mockRepository.Setup(r => r.GetHistoryAsync(guid, 90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageCarbonReport> { report });

        var result = await _handler.CheckCarbonThresholdAsync(guid);

        Assert.True(result.Approved);
        Assert.Equal(GreenScore.B, result.Score);
    }

    [Fact]
    public async Task CheckCarbonThreshold_BadScore_Rejects()
    {
        var guid = Guid.NewGuid();
        var report = new PageCarbonReport
        {
            ContentGuid = guid,
            Score = GreenScore.F,
            EstimatedCO2Grams = 3.5
        };

        _mockRepository.Setup(r => r.GetLatestReportAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        _mockRepository.Setup(r => r.GetHistoryAsync(guid, 90, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PageCarbonReport> { report });

        var result = await _handler.CheckCarbonThresholdAsync(guid);

        Assert.False(result.Approved);
        Assert.Equal(GreenScore.F, result.Score);
        Assert.Contains("D or F", result.Message);
    }
}
