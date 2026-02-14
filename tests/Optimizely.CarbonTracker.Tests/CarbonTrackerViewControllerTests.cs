using Microsoft.AspNetCore.Mvc;
using Moq;
using Optimizely.CarbonTracker.Controllers;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Controllers;

public class CarbonTrackerViewControllerTests
{
    private readonly CarbonTrackerViewController _controller;

    public CarbonTrackerViewControllerTests()
    {
        _controller = new CarbonTrackerViewController();
    }

    [Fact]
    public async Task Index_ReturnsViewResult()
    {
        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/CarbonTracker/Index.cshtml", viewResult.ViewName);
    }

    [Fact]
    public async Task Index_WithContentLink_PassesContentLinkToViewBag()
    {
        // Arrange
        var contentLink = "123_456";

        // Act
        var result = await _controller.Index(contentLink: contentLink);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(contentLink, viewResult.ViewData["ContentLink"]);
    }

    [Fact]
    public async Task Index_WithPageUrl_PassesPageUrlToViewBag()
    {
        // Arrange
        var pageUrl = "https://example.com/my-page";

        // Act
        var result = await _controller.Index(pageUrl: pageUrl);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(pageUrl, viewResult.ViewData["PageUrl"]);
    }

    [Fact]
    public async Task Index_WithBothParameters_PassesBothToViewBag()
    {
        // Arrange
        var contentLink = "789_012";
        var pageUrl = "https://example.com/another-page";

        // Act
        var result = await _controller.Index(contentLink: contentLink, pageUrl: pageUrl);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(contentLink, viewResult.ViewData["ContentLink"]);
        Assert.Equal(pageUrl, viewResult.ViewData["PageUrl"]);
    }

    [Fact]
    public async Task Index_WithNoParameters_SetsNullViewBagValues()
    {
        // Act
        var result = await _controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewData["ContentLink"]);
        Assert.Null(viewResult.ViewData["PageUrl"]);
    }

    [Fact]
    public async Task Index_WithContentGuid_PreLoadsCachedReport()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var cachedReport = new PageCarbonReport
        {
            ContentGuid = guid,
            PageUrl = "https://example.com/page",
            Score = GreenScore.B,
            EstimatedCO2Grams = 0.45
        };

        var mockRepo = new Mock<ICarbonReportRepository>();
        mockRepo.Setup(r => r.GetLatestReportAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedReport);

        var controller = new CarbonTrackerViewController(mockRepo.Object);

        // Act
        var result = await controller.Index(
            contentLink: "123_456",
            contentGuid: guid.ToString());

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(cachedReport, viewResult.ViewData["CachedReport"]);
        Assert.Equal("B", viewResult.ViewData["CachedScore"]);
        Assert.Equal(0.45, viewResult.ViewData["CachedCO2"]);
    }

    [Fact]
    public async Task Index_WithInvalidContentGuid_DoesNotPreLoad()
    {
        // Arrange
        var mockRepo = new Mock<ICarbonReportRepository>();
        var controller = new CarbonTrackerViewController(mockRepo.Object);

        // Act
        var result = await controller.Index(contentGuid: "not-a-guid");

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewData["CachedReport"]);
        mockRepo.Verify(r => r.GetLatestReportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Index_WithContentGuidButNoReport_CachedReportIsNull()
    {
        // Arrange
        var guid = Guid.NewGuid();
        var mockRepo = new Mock<ICarbonReportRepository>();
        mockRepo.Setup(r => r.GetLatestReportAsync(guid, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PageCarbonReport?)null);

        var controller = new CarbonTrackerViewController(mockRepo.Object);

        // Act
        var result = await controller.Index(contentGuid: guid.ToString());

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewData["CachedReport"]);
    }

    [Fact]
    public async Task Index_WithoutRepository_StillReturnsView()
    {
        // Arrange — no repository injected (null)
        var controller = new CarbonTrackerViewController();

        // Act
        var result = await controller.Index(
            contentLink: "123",
            contentGuid: Guid.NewGuid().ToString());

        // Assert — should still work without pre-loading
        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("~/Views/CarbonTracker/Index.cshtml", viewResult.ViewName);
        Assert.Null(viewResult.ViewData["CachedReport"]);
    }
}
