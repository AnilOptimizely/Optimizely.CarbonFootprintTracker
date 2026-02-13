using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Result of analyzing a page
/// </summary>
public class PageAnalysisResult
{
    /// <summary>
    /// Page URL that was analyzed
    /// </summary>
    public string PageUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp of analysis
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// All discovered resources
    /// </summary>
    public List<DiscoveredResource> Resources { get; set; } = new();
    
    /// <summary>
    /// Total transfer size in bytes
    /// </summary>
    public double TotalTransferSizeBytes { get; set; }
    
    /// <summary>
    /// Optimization suggestions from analyzers
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
    
    /// <summary>
    /// Whether analysis completed successfully
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// Error message if analysis failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
