namespace Optimizely.CarbonTracker.Models;

/// <summary>
/// Actionable optimization suggestion to reduce carbon footprint
/// </summary>
public class OptimizationSuggestion
{
    /// <summary>
    /// Severity level of this suggestion
    /// </summary>
    public SuggestionSeverity Severity { get; set; }
    
    /// <summary>
    /// Short title of the suggestion
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description of the optimization
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Potential data transfer savings in bytes
    /// </summary>
    public double PotentialSavingsBytes { get; set; }
    
    /// <summary>
    /// Potential CO₂ savings in grams
    /// </summary>
    public double PotentialCO2SavingsGrams { get; set; }
    
    /// <summary>
    /// URL or path of the affected asset
    /// </summary>
    public string? AffectedAssetUrl { get; set; }
}
