using Microsoft.AspNetCore.Mvc;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;

namespace Optimizely.CarbonTracker.Controllers;

/// <summary>
/// IFrame component controller for the Carbon Tracker panel in Optimizely CMS Edit Mode.
///
/// This controller serves the HTML loaded inside the editor sidebar iframe. The CMS shell
/// automatically appends <c>?contentLink=123_456</c> when the editor selects a page.
///
/// Capabilities exposed to editors:
/// <list type="bullet">
///   <item>Real-time <b>Green Score</b> (A–F) for the currently selected page</item>
///   <item><b>Asset breakdown</b> by type (HTML, CSS, JS, Images, Fonts, Video)</item>
///   <item>Actionable <b>optimization suggestions</b> with estimated CO₂ savings</item>
///   <item>Historical trend sparkline and re-analysis on demand</item>
/// </list>
///
/// When a cached report exists for the content GUID, the controller pre-loads it into
/// the view model so the panel renders instantly without waiting for a client-side fetch.
/// The client-side <c>app.js</c> then takes over for live re-analysis and interactions.
///
/// In production, protect with <c>[Authorize(Policy = "episerver:cmseditor")]</c>.
/// </summary>
[Route("[controller]")]
public class CarbonTrackerViewController : Controller
{
    private readonly ICarbonReportRepository? _repository;

    /// <summary>
    /// Creates a new instance of the IFrame component controller.
    /// </summary>
    /// <param name="repository">
    /// Optional. When provided, the controller pre-loads the latest cached report so the panel
    /// renders the Green Score instantly without waiting for a client-side API fetch.
    /// When null (e.g. before DI is configured), the panel still works — it simply falls back
    /// to the client-side <c>app.js</c> fetch on load.
    /// </param>
    public CarbonTrackerViewController(ICarbonReportRepository? repository = null)
    {
        _repository = repository;
    }

    /// <summary>
    /// Render the Carbon Tracker IFrame component UI.
    /// The CMS shell passes <paramref name="contentLink"/> automatically when an editor
    /// selects or navigates to a page in Edit Mode.
    /// </summary>
    /// <param name="contentLink">Content link/ID from CMS (e.g., "123_456")</param>
    /// <param name="pageUrl">Optional page URL to analyze</param>
    /// <param name="contentGuid">Optional content GUID for direct report lookup</param>
    [HttpGet]
    [Route("Index")]
    public async Task<IActionResult> Index(
        string? contentLink = null,
        string? pageUrl = null,
        string? contentGuid = null)
    {
        ViewBag.ContentLink = contentLink;
        ViewBag.PageUrl = pageUrl;

        // Try to pre-load the cached report so the panel renders immediately
        if (_repository != null
            && !string.IsNullOrWhiteSpace(contentGuid)
            && Guid.TryParse(contentGuid, out var guid))
        {
            var report = await _repository.GetLatestReportAsync(guid);
            if (report != null)
            {
                ViewBag.CachedReport = report;
                ViewBag.CachedScore = report.Score.ToString();
                ViewBag.CachedCO2 = report.EstimatedCO2Grams;
            }
        }

        return View("~/Views/CarbonTracker/Index.cshtml");
    }
}
