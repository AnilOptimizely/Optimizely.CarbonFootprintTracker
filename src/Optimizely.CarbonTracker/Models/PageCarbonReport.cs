namespace Optimizely.CarbonTracker.Models;

/// <summary>
/// Complete carbon footprint report for a page
/// </summary>
public class PageCarbonReport
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }
    
    /// <summary>
    /// Optimizely content GUID reference
    /// </summary>
    public Guid ContentGuid { get; set; }
    
    /// <summary>
    /// Page URL that was analyzed
    /// </summary>
    public string PageUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// Timestamp when analysis was performed
    /// </summary>
    public DateTime AnalyzedAt { get; set; }
    
    /// <summary>
    /// Total transfer size in bytes
    /// </summary>
    public double TotalTransferSizeBytes { get; set; }
    
    /// <summary>
    /// Estimated CO₂ emissions in grams
    /// </summary>
    public double EstimatedCO2Grams { get; set; }
    
    /// <summary>
    /// Green score rating (A-F)
    /// </summary>
    public GreenScore Score { get; set; }
    
    /// <summary>
    /// Breakdown by asset category
    /// </summary>
    public List<AssetBreakdown> Assets { get; set; } = new();
    
    /// <summary>
    /// Optimization suggestions
    /// </summary>
    public List<OptimizationSuggestion> Suggestions { get; set; } = new();
}
