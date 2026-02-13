namespace Optimizely.CarbonTracker.Models;

/// <summary>
/// Green Score rating from A (best) to F (worst) based on CO₂ emissions per page view
/// </summary>
public enum GreenScore
{
    /// <summary>≤0.20g CO₂ per page view</summary>
    A,
    
    /// <summary>0.21–0.50g CO₂ per page view</summary>
    B,
    
    /// <summary>0.51–1.00g CO₂ per page view</summary>
    C,
    
    /// <summary>1.01–2.00g CO₂ per page view</summary>
    D,
    
    /// <summary>&gt;2.00g CO₂ per page view</summary>
    F
}
