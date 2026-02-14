using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Optimizely.CarbonTracker.Analysis;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Configuration;

namespace Optimizely.CarbonTracker;

/// <summary>
/// Extension methods for registering Carbon Tracker services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add Carbon Tracker services to the service collection
    /// </summary>
    public static IServiceCollection AddCarbonTracker(
        this IServiceCollection services,
        Action<CarbonTrackerOptions>? configure = null)
    {
        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }
        else
        {
            services.Configure<CarbonTrackerOptions>(options => { });
        }
        
        // Register HttpClient for page analysis
        services.AddHttpClient<IPageAnalyzer, PageAnalyzer>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Optimizely-CarbonTracker/1.0");
            });
        
        // Register core services
        services.TryAddScoped<ICarbonCalculator, CarbonCalculator>();
        services.TryAddScoped<IImageAnalyzer, ImageAnalyzer>();
        services.TryAddScoped<IScriptAnalyzer, ScriptAnalyzer>();
        services.TryAddScoped<IVideoAnalyzer, VideoAnalyzer>();
        services.TryAddScoped<ICarbonReportService, CarbonReportService>();
        
        return services;
    }
}
