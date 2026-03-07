namespace Optimizely.CarbonTracker.Models;

/// <summary>
/// Breakdown of a specific asset category's contribution to page carbon footprint
/// </summary>
public class AssetBreakdown
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to the parent report
    /// </summary>
    public int PageCarbonReportId { get; set; }

    /// <summary>
    /// Asset category type
    /// </summary>
    public AssetCategory Category { get; set; }
    
    /// <summary>
    /// Total transfer size in bytes for this category
    /// </summary>
    public double TransferSizeBytes { get; set; }
    
    /// <summary>
    /// Percentage of total page transfer size
    /// </summary>
    public double Percentage { get; set; }
    
    /// <summary>
    /// Estimated CO₂ emissions in grams for this category
    /// </summary>
    public double EstimatedCO2Grams { get; set; }
    
    /// <summary>
    /// Number of resources in this category
    /// </summary>
    public int ResourceCount { get; set; }
}
