using HtmlAgilityPack;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Analyzes JavaScript resources for optimization opportunities
/// </summary>
public class ScriptAnalyzer : IScriptAnalyzer
{
    private readonly ICarbonCalculator _carbonCalculator;
    
    // Common third-party domains
    private static readonly string[] ThirdPartyDomains = 
    {
        "google-analytics.com", "googletagmanager.com", "facebook.net",
        "doubleclick.net", "googlesyndication.com", "amazon-adsystem.com"
    };
    
    public ScriptAnalyzer(ICarbonCalculator carbonCalculator)
    {
        _carbonCalculator = carbonCalculator;
    }
    
    public List<OptimizationSuggestion> Analyze(List<DiscoveredResource> resources, HtmlDocument htmlDoc)
    {
        var suggestions = new List<OptimizationSuggestion>();
        var scripts = resources.Where(r => r.Category == AssetCategory.JavaScript).ToList();
        
        if (!scripts.Any()) return suggestions;
        
        // Check for render-blocking scripts (no async/defer)
        var renderBlockingScripts = scripts.Where(script => 
            string.IsNullOrEmpty(script.Attributes.GetValueOrDefault("async")) &&
            string.IsNullOrEmpty(script.Attributes.GetValueOrDefault("defer"))).ToList();
        
        if (renderBlockingScripts.Any())
        {
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.High,
                Title = "Add async or defer to script tags",
                Description = $"{renderBlockingScripts.Count} scripts are render-blocking. Adding async or defer attributes can improve page load performance.",
                PotentialSavingsBytes = 0, // Performance benefit, not size reduction
                PotentialCO2SavingsGrams = 0
            });
        }
        
        // Check for large script bundles
        var largeBundles = scripts.Where(s => s.TransferSizeBytes > 100 * 1024).ToList(); // > 100KB
        if (largeBundles.Any())
        {
            var totalSize = largeBundles.Sum(s => s.TransferSizeBytes);
            var potentialSavings = totalSize * 0.3; // Code splitting could save ~30%
            
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.High,
                Title = "Split large JavaScript bundles",
                Description = $"{largeBundles.Count} scripts are larger than 100KB. Consider code splitting and lazy loading.",
                PotentialSavingsBytes = potentialSavings,
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(potentialSavings),
                AffectedAssetUrl = largeBundles.OrderByDescending(s => s.TransferSizeBytes).First().Url
            });
        }
        
        // Check for third-party scripts
        var thirdPartyScripts = scripts.Where(s => 
            ThirdPartyDomains.Any(domain => s.Url.Contains(domain, StringComparison.OrdinalIgnoreCase))).ToList();
        
        if (thirdPartyScripts.Any())
        {
            var totalSize = thirdPartyScripts.Sum(s => s.TransferSizeBytes);
            
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.Medium,
                Title = "Review third-party scripts",
                Description = $"{thirdPartyScripts.Count} third-party scripts detected ({FormatBytes(totalSize)}). Consider if all are necessary.",
                PotentialSavingsBytes = totalSize * 0.5, // Could potentially remove half
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(totalSize * 0.5)
            });
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
