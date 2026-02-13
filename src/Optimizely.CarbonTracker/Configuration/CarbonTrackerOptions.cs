namespace Optimizely.CarbonTracker.Configuration;

/// <summary>
/// Configuration options for the Carbon Tracker add-on
/// </summary>
public class CarbonTrackerOptions
{
    /// <summary>
    /// Whether hosting uses renewable energy (affects data center carbon emissions)
    /// Default: false
    /// </summary>
    public bool GreenHosting { get; set; } = false;
    
    /// <summary>
    /// Regional grid carbon intensity in grams CO₂ per kWh
    /// Default: 442 (global average)
    /// </summary>
    public double GridIntensityGramsCO2PerKWh { get; set; } = 442;
    
    /// <summary>
    /// Enable real-time analysis on content publish
    /// Default: true
    /// </summary>
    public bool EnableRealTimeAnalysis { get; set; } = true;
    
    /// <summary>
    /// Number of days to retain historical reports
    /// Default: 365
    /// </summary>
    public int HistoryRetentionDays { get; set; } = 365;
    
    /// <summary>
    /// Timeout in seconds for analyzing a single resource
    /// Default: 30
    /// </summary>
    public int ResourceTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Total timeout in seconds for analyzing a page
    /// Default: 120
    /// </summary>
    public int PageAnalysisTimeoutSeconds { get; set; } = 120;
}
