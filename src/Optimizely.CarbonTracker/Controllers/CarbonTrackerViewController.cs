using Microsoft.AspNetCore.Mvc;

namespace Optimizely.CarbonTracker.Controllers;

/// <summary>
/// Controller for serving the Carbon Tracker IFrame UI
/// </summary>
[Route("carbon-tracker/ui")]
public class CarbonTrackerViewController : Controller
{
    /// <summary>
    /// Render the Carbon Tracker UI panel
    /// </summary>
    /// <param name="contentLink">Content link/ID from CMS</param>
    /// <param name="pageUrl">Page URL to analyze</param>
    [HttpGet]
    public IActionResult Index(string? contentLink = null, string? pageUrl = null)
    {
        ViewBag.ContentLink = contentLink;
        ViewBag.PageUrl = pageUrl;
        return View();
    }
}
