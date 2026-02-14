using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Optimizely.CarbonTracker.Controllers;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Controllers;

public class CarbonTrackerApiControllerTests
{
    private readonly Mock<ICarbonReportService> _mockReportService;
    private readonly Mock<ICarbonReportRepository> _mockRepository;
    private readonly CarbonTrackerApiController _controller;

    public CarbonTrackerApiControllerTests()
    {
        _mockReportService = new Mock<ICarbonReportService>();
        _mockRepository = new Mock<ICarbonReportRepository>();
        var logger = Mock.Of<ILogger<CarbonTrackerApiController>>();
        _controller = new CarbonTrackerApiController(_mockReportService.Object, _mockRepository.Object, logger);
    }

    [Fact]
    public async Task GetReport_ReturnsReport_WhenFound()
    {
        var guid = Guid.NewGuid();
        var report = new PageCarbonReport { ContentGuid = guid, Score = GreenScore.B, EstimatedCO2Grams = 0.4 };
        _mockRepository.Setup(r => r.GetLatestReportAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);

        var result = await _controller.GetReport(guid.ToString());

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(report, okResult.Value);
    }

    [Fact]
    public async Task GetReport_Returns404_WhenNotFound()
    {
        var guid = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetLatestReportAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PageCarbonReport?)null);

        var result = await _controller.GetReport(guid.ToString());

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetReport_ReturnsBadRequest_ForInvalidGuid()
    {
        var result = await _controller.GetReport("not-a-guid");

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetHistory_ReturnsReports()
    {
        var guid = Guid.NewGuid();
        var history = new List<PageCarbonReport>
        {
            new() { ContentGuid = guid, Score = GreenScore.B },
            new() { ContentGuid = guid, Score = GreenScore.C }
        };
        _mockRepository.Setup(r => r.GetHistoryAsync(guid, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = await _controller.GetHistory(guid.ToString());

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedHistory = Assert.IsType<List<PageCarbonReport>>(okResult.Value);
        Assert.Equal(2, returnedHistory.Count);
    }

    [Fact]
    public async Task GetSiteSummary_ReturnsSummary()
    {
        var summary = new SiteCarbonSummary
        {
            TotalPagesAnalyzed = 10,
            AverageCO2PerPage = 0.5,
            AverageGreenScore = GreenScore.B
        };
        _mockRepository.Setup(r => r.GetSiteSummaryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(summary);

        var result = await _controller.GetSiteSummary();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(summary, okResult.Value);
    }

    [Fact]
    public async Task AnalyzeByContentId_ReturnsBadRequest_ForInvalidGuid()
    {
        var result = await _controller.AnalyzeByContentId("invalid-guid", null);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AnalyzeByContentId_UsesExistingUrl_WhenNoUrlProvided()
    {
        var guid = Guid.NewGuid();
        var existingReport = new PageCarbonReport
        {
            ContentGuid = guid,
            PageUrl = "https://example.com/existing",
            Score = GreenScore.B
        };
        var newReport = new PageCarbonReport
        {
            ContentGuid = guid,
            PageUrl = "https://example.com/existing",
            Score = GreenScore.A,
            EstimatedCO2Grams = 0.1
        };

        _mockRepository.Setup(r => r.GetLatestReportAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReport);
        _mockReportService.Setup(s => s.GenerateReportAsync("https://example.com/existing", guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newReport);

        var result = await _controller.AnalyzeByContentId(guid.ToString(), null);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(newReport, okResult.Value);
    }

    [Fact]
    public void GetSummary_ReturnsActiveStatus()
    {
        var result = _controller.GetSummary();

        Assert.IsType<OkObjectResult>(result);
    }
}
