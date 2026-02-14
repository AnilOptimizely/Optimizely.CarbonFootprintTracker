using Microsoft.AspNetCore.Mvc;

namespace Optimizely.CarbonTracker.Controllers;

/// <summary>
/// Controller for serving the Carbon Tracker IFrame UI.
/// This is loaded inside an iframe in the Optimizely CMS Edit Mode panel.
/// In production, protect with [Authorize(Policy = "episerver:cmseditor")].
/// </summary>
[Route("[controller]")]
public class CarbonTrackerViewController : Controller
{
    /// <summary>
    /// Render the Carbon Tracker UI panel inside the CMS iframe.
    /// The CMS shell passes contentLink as a query parameter automatically.
    /// </summary>
    /// <param name="contentLink">Content link/ID from CMS (e.g., "123_456")</param>
    /// <param name="pageUrl">Optional page URL to analyze</param>
    [HttpGet]
    [Route("Index")]
    public IActionResult Index(string? contentLink = null, string? pageUrl = null)
    {
        ViewBag.ContentLink = contentLink;
        ViewBag.PageUrl = pageUrl;
        return View("~/Views/CarbonTracker/Index.cshtml");
    }
}
