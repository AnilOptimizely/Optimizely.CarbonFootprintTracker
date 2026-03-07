using EPiServer.Shell.ViewComposition;
using Microsoft.AspNetCore.Mvc;

namespace Optimizely.CarbonTracker.Controllers;

[IFrameComponent(
      Url = "/CarbonTracker/Index",
      Title = "Carbon Tracker",
      Description = "Shows estimated carbon footprint for the current content",
      Categories = "content",
      PlugInAreas = "/episerver/cms/assets",
      MinHeight = 200,
      MaxHeight = 400,
      ReloadOnContextChange = true)]
public class CarbonTrackerViewController : Controller
{
    /// <summary>
    /// Render the Carbon Tracker UI panel inside the CMS iframe.
    /// The CMS shell passes contentLink as a query parameter automatically.
    /// </summary>
    /// <param name="contentLink">Content link/ID from CMS (e.g., "123_456")</param>
    /// <param name="pageUrl">Optional page URL to analyze</param>
    [HttpGet]
    [Route("CarbonTracker/Index")]
    public IActionResult Index(string? contentLink = null, string? pageUrl = null)
    {
        ViewBag.ContentLink = contentLink;
        ViewBag.PageUrl = pageUrl;
        return View("~/Views/CarbonTracker/Index.cshtml");
    }
}
