using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;

namespace Optimizely.CarbonTracker;

/// <summary>
/// Extension methods for IApplicationBuilder to register Carbon Tracker middleware
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Registers static file serving for Carbon Tracker embedded assets.
    /// Call this in the application pipeline to serve CSS/JS from the NuGet package.
    /// </summary>
    public static IApplicationBuilder UseCarbonTracker(this IApplicationBuilder app)
    {
        // Serve embedded static files (CSS, JS) from the assembly
        var assembly = typeof(ApplicationBuilderExtensions).Assembly;
        var embeddedProvider = new EmbeddedFileProvider(
            assembly,
            "Optimizely.CarbonTracker.wwwroot");

        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = embeddedProvider,
            RequestPath = "/carbon-tracker-assets"
        });

        return app;
    }
}
