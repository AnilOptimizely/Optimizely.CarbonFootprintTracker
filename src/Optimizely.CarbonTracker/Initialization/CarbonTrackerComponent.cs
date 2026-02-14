namespace Optimizely.CarbonTracker.Initialization;

/// <summary>
/// IFrameComponent descriptor for the Carbon Footprint Tracker panel in Optimizely CMS.
///
/// In an Optimizely CMS environment with EPiServer.CMS.UI.Core, this would inherit from
/// EPiServer.Shell.ViewComposition.IFrameComponent and be decorated with [Component].
///
/// This class provides the component metadata so that the CMS shell module system
/// can register the Carbon Tracker as a panel in Edit Mode.
///
/// Usage in Optimizely CMS:
/// <code>
/// [Component]
/// public class CarbonTrackerComponent : IFrameComponent
/// {
///     public CarbonTrackerComponent() : base("/CarbonTrackerView/Index")
///     {
///         LanguagePath = "/carbon-tracker";
///         Title = "Carbon Footprint";
///         Description = "Real-time carbon footprint analysis and Green Score.";
///         SortOrder = 1000;
///         PlugInAreas = new[] { PlugInArea.AssetsDefaultGroup };
///         Categories = new[] { "content" };
///         IsAvailableForUserSelection = true;
///     }
/// }
/// </code>
/// </summary>
public class CarbonTrackerComponent
{
    /// <summary>
    /// URL loaded inside the iframe in the CMS panel
    /// </summary>
    public string Url { get; } = "/CarbonTrackerView/Index";

    /// <summary>
    /// Language path for localization
    /// </summary>
    public string LanguagePath { get; set; } = "/carbon-tracker";

    /// <summary>
    /// Display title in the CMS panel
    /// </summary>
    public string Title { get; set; } = "Carbon Footprint";

    /// <summary>
    /// Description shown in the CMS panel
    /// </summary>
    public string Description { get; set; } = "Real-time carbon footprint analysis and Green Score for the current page.";

    /// <summary>
    /// Sort order for the component in the panel
    /// </summary>
    public int SortOrder { get; set; } = 1000;

    /// <summary>
    /// Plugin areas where this component should appear
    /// </summary>
    public string[] PlugInAreas { get; set; } = new[] { "AssetsDefaultGroup" };

    /// <summary>
    /// Categories for the component
    /// </summary>
    public string[] Categories { get; set; } = new[] { "content" };

    /// <summary>
    /// Whether users can add/remove this component from their view
    /// </summary>
    public bool IsAvailableForUserSelection { get; set; } = true;
}
