namespace Optimizely.CarbonTracker.Initialization;

/// <summary>
/// Abstraction for an IFrame-based component in the CMS editor UI.
/// Mirrors EPiServer.Shell.ViewComposition.IFrameComponent so the add-on
/// can compile without a hard dependency on EPiServer.CMS.UI.Core.
///
/// When deploying to an Optimizely CMS environment, replace this with:
/// <code>
/// [Component]
/// public class CarbonTrackerComponent : IFrameComponent { ... }
/// </code>
/// </summary>
public interface IIFrameComponent
{
    /// <summary>URL loaded inside the iframe in the CMS panel</summary>
    string Url { get; }

    /// <summary>Display title in the CMS panel</summary>
    string Title { get; }

    /// <summary>Description shown in the CMS panel tooltip</summary>
    string Description { get; }

    /// <summary>Sort order for the component in the panel list</summary>
    int SortOrder { get; }

    /// <summary>Plugin areas where this component should appear (e.g. "AssetsDefaultGroup")</summary>
    string[] PlugInAreas { get; }

    /// <summary>Categories for the component (e.g. "content")</summary>
    string[] Categories { get; }

    /// <summary>Whether editors can add/remove this component from their view</summary>
    bool IsAvailableForUserSelection { get; }
}

/// <summary>
/// IFrameComponent descriptor for the Carbon Footprint Tracker panel in Optimizely CMS.
///
/// Provides editors with a real-time Green Score for every page, breaks down emissions
/// by asset type (HTML, CSS, JS, Images, Fonts, Video), and surfaces actionable
/// optimization suggestions — all inside the CMS Edit Mode sidebar.
///
/// In an Optimizely CMS environment with EPiServer.CMS.UI.Core, decorate with
/// <c>[Component]</c> and inherit from <c>IFrameComponent</c> so the CMS shell
/// auto-discovers it at startup:
/// <code>
/// [Component]
/// public class CarbonTrackerComponent : IFrameComponent
/// {
///     public CarbonTrackerComponent()
///         : base("/CarbonTrackerView/Index")
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
public class CarbonTrackerComponent : IIFrameComponent
{
    /// <inheritdoc/>
    public string Url { get; } = "/CarbonTrackerView/Index";

    /// <summary>
    /// Language path for localization
    /// </summary>
    public string LanguagePath { get; set; } = "/carbon-tracker";

    /// <inheritdoc/>
    public string Title { get; set; } = "Carbon Footprint";

    /// <inheritdoc/>
    public string Description { get; set; } = "Real-time carbon footprint analysis and Green Score for the current page.";

    /// <inheritdoc/>
    public int SortOrder { get; set; } = 1000;

    /// <inheritdoc/>
    public string[] PlugInAreas { get; set; } = new[] { "AssetsDefaultGroup" };

    /// <inheritdoc/>
    public string[] Categories { get; set; } = new[] { "content" };

    /// <inheritdoc/>
    public bool IsAvailableForUserSelection { get; set; } = true;
}
