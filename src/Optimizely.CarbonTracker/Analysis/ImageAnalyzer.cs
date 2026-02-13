using HtmlAgilityPack;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Analyzes images for optimization opportunities
/// </summary>
public class ImageAnalyzer : IImageAnalyzer
{
    private readonly ICarbonCalculator _carbonCalculator;
    
    // Modern formats that should be preferred
    private static readonly string[] ModernFormats = { "webp", "avif" };
    private static readonly string[] LegacyFormats = { "jpeg", "jpg", "png", "gif" };
    
    public ImageAnalyzer(ICarbonCalculator carbonCalculator)
    {
        _carbonCalculator = carbonCalculator;
    }
    
    public List<OptimizationSuggestion> Analyze(List<DiscoveredResource> resources, HtmlDocument htmlDoc)
    {
        var suggestions = new List<OptimizationSuggestion>();
        var images = resources.Where(r => r.Category == AssetCategory.Images).ToList();
        
        if (!images.Any()) return suggestions;
        
        // Check for images without lazy loading
        var imagesWithoutLazy = images.Where(img => 
            string.IsNullOrEmpty(img.Attributes.GetValueOrDefault("loading"))).ToList();
        
        if (imagesWithoutLazy.Any())
        {
            var potentialSavings = imagesWithoutLazy.Sum(img => img.TransferSizeBytes) * 0.5; // Estimate 50% could be lazy-loaded
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.Medium,
                Title = "Enable lazy loading for images",
                Description = $"{imagesWithoutLazy.Count} images found without loading=\"lazy\" attribute. Lazy loading can defer loading of off-screen images.",
                PotentialSavingsBytes = potentialSavings,
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(potentialSavings)
            });
        }
        
        // Check for images without responsive srcset
        var imagesWithoutSrcset = images.Where(img => 
            string.IsNullOrEmpty(img.Attributes.GetValueOrDefault("srcset"))).ToList();
        
        if (imagesWithoutSrcset.Any())
        {
            var potentialSavings = imagesWithoutSrcset.Sum(img => img.TransferSizeBytes) * 0.3; // Estimate 30% savings with responsive images
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.Medium,
                Title = "Use responsive images with srcset",
                Description = $"{imagesWithoutSrcset.Count} images found without srcset attribute. Responsive images can reduce transfer size on smaller screens.",
                PotentialSavingsBytes = potentialSavings,
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(potentialSavings)
            });
        }
        
        // Check for non-modern image formats
        var nonModernImages = images.Where(img =>
        {
            var url = img.Url.ToLowerInvariant();
            return LegacyFormats.Any(format => url.Contains($".{format}"));
        }).ToList();
        
        if (nonModernImages.Any())
        {
            var potentialSavings = nonModernImages.Sum(img => img.TransferSizeBytes) * 0.4; // WebP/AVIF can save ~40%
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.High,
                Title = "Convert images to modern formats (WebP/AVIF)",
                Description = $"{nonModernImages.Count} images are using legacy formats (JPEG/PNG). Modern formats like WebP or AVIF can reduce file size by 25-50%.",
                PotentialSavingsBytes = potentialSavings,
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(potentialSavings)
            });
        }
        
        // Check for oversized images
        var largeImages = images.Where(img => img.TransferSizeBytes > 200 * 1024).ToList(); // > 200KB
        if (largeImages.Any())
        {
            var totalSize = largeImages.Sum(img => img.TransferSizeBytes);
            var potentialSavings = totalSize * 0.5; // Could potentially compress/optimize by 50%
            
            suggestions.Add(new OptimizationSuggestion
            {
                Severity = SuggestionSeverity.High,
                Title = "Optimize large images",
                Description = $"{largeImages.Count} images are larger than 200KB. Consider compressing or resizing these images.",
                PotentialSavingsBytes = potentialSavings,
                PotentialCO2SavingsGrams = _carbonCalculator.CalculateCO2Grams(potentialSavings),
                AffectedAssetUrl = largeImages.OrderByDescending(i => i.TransferSizeBytes).First().Url
            });
        }
        
        return suggestions;
    }
}
