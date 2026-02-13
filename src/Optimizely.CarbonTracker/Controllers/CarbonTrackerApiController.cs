using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Controllers;

/// <summary>
/// API controller for carbon footprint analysis
/// </summary>
[ApiController]
[Route("api/carbon-tracker")]
public class CarbonTrackerApiController : ControllerBase
{
    private readonly ICarbonReportService _reportService;
    private readonly ILogger<CarbonTrackerApiController> _logger;
    
    public CarbonTrackerApiController(
        ICarbonReportService reportService,
        ILogger<CarbonTrackerApiController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }
    
    /// <summary>
    /// Analyze a page and return its carbon footprint report
    /// </summary>
    /// <param name="pageUrl">URL of the page to analyze</param>
    /// <param name="contentGuid">Optional content GUID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Carbon footprint report</returns>
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
            
            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing page: {PageUrl}", pageUrl);
            return StatusCode(500, new { error = "Failed to analyze page", message = ex.Message });
        }
    }
    
    /// <summary>
    /// Get summary statistics
    /// </summary>
    /// <returns>Summary stats</returns>
    [HttpGet("summary")]
    [ProducesResponseType(200)]
    public ActionResult GetSummary()
    {
        // Placeholder for summary statistics
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
