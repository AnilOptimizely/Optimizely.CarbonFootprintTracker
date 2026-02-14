using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;

namespace Optimizely.CarbonTracker.Controllers;

/// <summary>
/// API controller for carbon footprint analysis.
/// All endpoints are intended to be protected behind Optimizely's CMS editor role.
/// </summary>
[ApiController]
[Route("api/carbon-tracker")]
public class CarbonTrackerApiController : ControllerBase
{
    private readonly ICarbonReportService _reportService;
    private readonly ICarbonReportRepository _repository;
    private readonly ILogger<CarbonTrackerApiController> _logger;

    public CarbonTrackerApiController(
        ICarbonReportService reportService,
        ICarbonReportRepository repository,
        ILogger<CarbonTrackerApiController> logger)
    {
        _reportService = reportService;
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// Analyze a page by URL and return its carbon footprint report
    /// </summary>
    [HttpGet("analyze")]
    [ProducesResponseType(typeof(PageCarbonReport), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PageCarbonReport>> Analyze(
        [FromQuery] string pageUrl,
        [FromQuery] Guid? contentGuid,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pageUrl))
        {
            return BadRequest("Page URL is required");
        }

        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out _))
        {
            return BadRequest("Invalid page URL format");
        }

        try
        {
            _logger.LogInformation("Analyzing page: {PageUrl}", pageUrl);

            var report = await _reportService.GenerateReportAsync(
                pageUrl,
                contentGuid ?? Guid.Empty,
                cancellationToken);

            await _repository.SaveReportAsync(report, cancellationToken);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing page: {PageUrl}", pageUrl);
            return StatusCode(500, new { error = "Failed to analyze page", message = ex.Message });
        }
    }

    /// <summary>
    /// Trigger analysis for a specific content item by its GUID and return the report
    /// </summary>
    [HttpGet("analyze/{contentId}")]
    [ProducesResponseType(typeof(PageCarbonReport), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<PageCarbonReport>> AnalyzeByContentId(
        string contentId,
        [FromQuery] string? pageUrl,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(contentId, out var contentGuid))
        {
            return BadRequest("Invalid content ID format");
        }

        try
        {
            // If no URL provided, try to get from latest report
            var url = pageUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                var existing = await _repository.GetLatestReportAsync(contentGuid, cancellationToken);
                url = existing?.PageUrl;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest("Page URL is required for first-time analysis. Provide ?pageUrl= parameter.");
            }

            _logger.LogInformation("Analyzing content {ContentId}: {PageUrl}", contentId, url);

            var report = await _reportService.GenerateReportAsync(url, contentGuid, cancellationToken);
            await _repository.SaveReportAsync(report, cancellationToken);

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing content: {ContentId}", contentId);
            return StatusCode(500, new { error = "Failed to analyze content", message = ex.Message });
        }
    }

    /// <summary>
    /// Get the most recent cached report for a content item
    /// </summary>
    [HttpGet("report/{contentId}")]
    [ProducesResponseType(typeof(PageCarbonReport), 200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<PageCarbonReport>> GetReport(
        string contentId,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(contentId, out var contentGuid))
        {
            return BadRequest("Invalid content ID format");
        }

        var report = await _repository.GetLatestReportAsync(contentGuid, cancellationToken);
        if (report == null)
        {
            return NotFound(new { message = "No report found for this content item" });
        }

        return Ok(report);
    }

    /// <summary>
    /// Get historical Green Scores for a content item
    /// </summary>
    [HttpGet("history/{contentId}")]
    [ProducesResponseType(typeof(List<PageCarbonReport>), 200)]
    public async Task<ActionResult<List<PageCarbonReport>>> GetHistory(
        string contentId,
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(contentId, out var contentGuid))
        {
            return BadRequest("Invalid content ID format");
        }

        var history = await _repository.GetHistoryAsync(contentGuid, days, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Get aggregate site-wide carbon stats
    /// </summary>
    [HttpGet("site-summary")]
    [ProducesResponseType(typeof(SiteCarbonSummary), 200)]
    public async Task<ActionResult<SiteCarbonSummary>> GetSiteSummary(
        CancellationToken cancellationToken = default)
    {
        var summary = await _repository.GetSiteSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Get summary statistics and system info
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(200)]
    public ActionResult GetSummary()
    {
        return Ok(new
        {
            message = "Carbon Tracker is active",
            version = "1.0.0",
            standards = new[]
            {
                "Sustainable Web Design v4",
                "CO2.js methodology"
            }
        });
    }
}
