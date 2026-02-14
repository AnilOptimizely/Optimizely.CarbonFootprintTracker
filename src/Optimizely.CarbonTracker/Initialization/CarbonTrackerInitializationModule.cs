using Microsoft.Extensions.Logging;

namespace Optimizely.CarbonTracker.Initialization;

/// <summary>
/// Module initializer for the Carbon Tracker add-on.
///
/// In an Optimizely CMS environment, this would implement IConfigurableModule
/// to wire up services and event handlers during CMS startup.
///
/// Usage in Optimizely CMS:
/// <code>
/// [InitializableModule]
/// [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
/// public class CarbonTrackerInitializationModule : IConfigurableModule
/// {
///     public void ConfigureContainer(ServiceConfigurationContext context)
///     {
///         context.Services.AddCarbonTracker();
///     }
///
///     public void Initialize(InitializationEngine context)
///     {
///         var events = context.Locate.Advanced.GetInstance&lt;IContentEvents&gt;();
///         var handler = context.Locate.Advanced.GetInstance&lt;IContentEventHandler&gt;();
///         events.PublishedContent += (sender, args) =&gt; { /* trigger analysis */ };
///     }
///
///     public void Uninitialize(InitializationEngine context) { }
/// }
/// </code>
/// </summary>
public class CarbonTrackerInitializationModule
{
    private readonly IContentEventHandler _eventHandler;
    private readonly ILogger<CarbonTrackerInitializationModule> _logger;

    public CarbonTrackerInitializationModule(
        IContentEventHandler eventHandler,
        ILogger<CarbonTrackerInitializationModule> logger)
    {
        _eventHandler = eventHandler;
        _logger = logger;
    }

    /// <summary>
    /// Initialize the module and register event handlers
    /// </summary>
    public void Initialize()
    {
        _logger.LogInformation("Carbon Tracker module initialized");
    }

    /// <summary>
    /// Get the content event handler for external wiring
    /// </summary>
    public IContentEventHandler EventHandler => _eventHandler;
}
