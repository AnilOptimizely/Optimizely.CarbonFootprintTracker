using EPiServer.Shell.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Optimizely.CarbonTracker.Analysis;
using Optimizely.CarbonTracker.Configuration;
using Optimizely.CarbonTracker.Data;
using Optimizely.CarbonTracker.Initialization;
using Optimizely.CarbonTracker.Services;

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
        Action<CarbonTrackerOptions>? configure = null,
        Action<DbContextOptionsBuilder>? configureDb = null)
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
        services.Configure<ProtectedModuleOptions>(o => o.Items.Add(new ModuleDetails { Name = "Optimizely.CarbonFootprintTracker" }));

        // Register EF Core DbContext
        services.AddDbContext<CarbonTrackerDbContext>(options =>
        {
            if (configureDb != null)
            {
                configureDb(options);
            }
        });

        // Register HttpClient for page analysis
        services.AddHttpClient<IPageAnalysisService, PageAnalysisService>()
            .ConfigureHttpClient(client =>
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Optimizely-CarbonTracker/1.0");
            });

        // Register core services
        services.TryAddScoped<ICarbonCalculatorService, CarbonCalculatorService>();
        services.TryAddScoped<IImageAnalyzer, ImageAnalyzer>();
        services.TryAddScoped<IScriptAnalyzer, ScriptAnalyzer>();
        services.TryAddScoped<IVideoAnalyzer, VideoAnalyzer>();
        services.TryAddScoped<ICarbonReportService, CarbonReportService>();

        // Register persistence (Scoped to match DbContext lifetime)
        services.TryAddScoped<ICarbonReportRepository, CarbonReportRepositoryService>();

        // Register CMS hooks
        services.TryAddScoped<IContentEventHandler, ContentEventHandler>();
        services.TryAddSingleton<CarbonTrackerInitializationModule>();

        return services;
    }
}
