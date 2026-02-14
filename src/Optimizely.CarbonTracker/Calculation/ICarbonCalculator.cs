using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Calculation;

/// <summary>
/// Interface for calculating CO₂ emissions from web page data transfer
/// </summary>
public interface ICarbonCalculator
{
    /// <summary>
    /// Calculate CO₂ emissions for a page based on transfer size
    /// </summary>
    /// <param name="transferSizeBytes">Total transfer size in bytes</param>
    /// <param name="isReturningVisit">Whether this is a returning visitor (uses cache)</param>
    /// <returns>CO₂ emissions in grams</returns>
    double CalculateCO2Grams(double transferSizeBytes, bool isReturningVisit = false);
    
    /// <summary>
    /// Map CO₂ emissions to a Green Score rating
    /// </summary>
    /// <param name="co2Grams">CO₂ emissions in grams</param>
    /// <returns>Green Score rating (A-F)</returns>
    GreenScore CalculateGreenScore(double co2Grams);
    
    /// <summary>
    /// Calculate CO₂ for a specific asset category
    /// </summary>
    /// <param name="transferSizeBytes">Transfer size in bytes for this category</param>
    /// <param name="isReturningVisit">Whether this is a returning visitor</param>
    /// <returns>CO₂ emissions in grams</returns>
    double CalculateCategoryCO2(double transferSizeBytes, bool isReturningVisit = false);
}
