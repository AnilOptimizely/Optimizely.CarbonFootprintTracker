using HtmlAgilityPack;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Analyzes video resources for optimization opportunities
/// </summary>
public class VideoAnalyzer : IVideoAnalyzer
{
    private readonly ICarbonCalculator _carbonCalculator;
    
    public VideoAnalyzer(ICarbonCalculator carbonCalculator)
    {
        _carbonCalculator = carbonCalculator;
    }
    
    public List<OptimizationSuggestion> Analyze(List<DiscoveredResource> resources, HtmlDocument htmlDoc)
    {
        var suggestions = new List<OptimizationSuggestion>();
        var videos = resources.Where(r => r.Category == AssetCategory.Video).ToList();
        
        if (!videos.Any()) return suggestions;
        
        // Check for autoplay videos
        var autoplayVideos = videos.Where(v => 
            !string.IsNullOrEmpty(v.Attributes.GetValueOrDefault("autoplay"))).ToList();
        
        if (autoplayVideos.Any())
        {
            var totalSize = autoplayVideos.Sum(v => v.TransferSizeBytes);
            
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.Critical,
                Title = "Remove autoplay from videos",
                Description = $"{autoplayVideos.Count} videos are set to autoplay, forcing all users to download them. This significantly increases carbon footprint.",
                PotentialSavingsBytes = totalSize * 0.8, // Most users won't watch
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(totalSize * 0.8),
                AffectedAssetUrl = autoplayVideos.First().Url
            });
        }
        
        // Check for videos with preload="auto"
        var preloadAutoVideos = videos.Where(v => 
            v.Attributes.GetValueOrDefault("preload") == "auto").ToList();
        
        if (preloadAutoVideos.Any())
        {
            var totalSize = preloadAutoVideos.Sum(v => v.TransferSizeBytes);
            
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.High,
                Title = "Change video preload to metadata or none",
                Description = $"{preloadAutoVideos.Count} videos use preload=\"auto\". Use preload=\"metadata\" or preload=\"none\" to reduce initial page load.",
                PotentialSavingsBytes = totalSize * 0.7,
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(totalSize * 0.7)
            });
        }
        
        // General warning about video content
        if (videos.Any())
        {
            var totalVideoSize = videos.Sum(v => v.TransferSizeBytes);
            if (totalVideoSize > 1024 * 1024) // > 1MB
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    Severity = SuggestionSeverity.Medium,
                    Title = "Consider video file size optimization",
                    Description = $"Total video content: {FormatBytes(totalVideoSize)}. Video is carbon-intensive. Consider compression, shorter clips, or poster images with click-to-play.",
                    PotentialSavingsBytes = totalVideoSize * 0.3,
                    PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(totalVideoSize * 0.3)
                });
            }
        }
        
        return suggestions;
    }
    
    private string FormatBytes(double bytes)
    {
        if (bytes < 1024) return $"{bytes:F0}B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024:F1}KB";
        return $"{bytes / (1024 * 1024):F1}MB";
    }
}
