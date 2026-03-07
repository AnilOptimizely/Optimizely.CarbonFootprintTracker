using Microsoft.Extensions.Options;
using Optimizely.CarbonTracker.Configuration;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Services;

/// <summary>
/// Interface for calculating CO₂ emissions from web page data transfer
/// </summary>
public interface ICarbonCalculatorService
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

/// <summary>
/// Implements the Sustainable Web Design (SWD) v4 model for calculating CO₂ emissions
/// Based on: https://sustainablewebdesign.org/estimating-digital-emissions/
/// </summary>
public class CarbonCalculatorService : ICarbonCalculatorService
{
    // SWD v4 Constants
    private const double EnergyPerGB = 0.81; // kWh/GB
    private const double CacheRatio = 0.02; // 2% of data re-transferred for returning visits
    
    // System segments (percentage of total energy)
    private const double DataCenterSegment = 0.15; // 15%
    private const double NetworkSegment = 0.14; // 14%
    private const double UserDeviceSegment = 0.52; // 52%
    private const double ProductionSegment = 0.19; // 19%
    
    private readonly CarbonTrackerOptions _options;
    
    public CarbonCalculatorService(IOptions<CarbonTrackerOptions> options)
    {
        _options = options.Value;
    }
    
    /// <inheritdoc/>
    public double CalculateCO2Grams(double transferSizeBytes, bool isReturningVisit = false)
    {
        if (transferSizeBytes <= 0)
            return 0;
        
        // Adjust for cache on returning visits
        var effectiveBytes = isReturningVisit ? transferSizeBytes * CacheRatio : transferSizeBytes;
        
        // Convert bytes to GB
        var transferSizeGB = effectiveBytes / (1024.0 * 1024.0 * 1024.0);
        
        // Calculate energy consumption in kWh
        var energyKWh = transferSizeGB * EnergyPerGB;
        
        // Calculate CO₂ for each system segment
        var dataCenterCO2 = CalculateSegmentCO2(energyKWh, DataCenterSegment, useGreenHosting: true);
        var networkCO2 = CalculateSegmentCO2(energyKWh, NetworkSegment, useGreenHosting: false);
        var deviceCO2 = CalculateSegmentCO2(energyKWh, UserDeviceSegment, useGreenHosting: false);
        var productionCO2 = CalculateSegmentCO2(energyKWh, ProductionSegment, useGreenHosting: false);
        
        return dataCenterCO2 + networkCO2 + deviceCO2 + productionCO2;
    }
    
    /// <inheritdoc/>
    public GreenScore CalculateGreenScore(double co2Grams)
    {
        return co2Grams switch
        {
            <= 0.20 => GreenScore.A,
            <= 0.50 => GreenScore.B,
            <= 1.00 => GreenScore.C,
            <= 2.00 => GreenScore.D,
            _ => GreenScore.F
        };
    }
    
    /// <inheritdoc/>
    public double CalculateCategoryCO2(double transferSizeBytes, bool isReturningVisit = false)
    {
        return CalculateCO2Grams(transferSizeBytes, isReturningVisit);
    }
    
    /// <summary>
    /// Calculate CO₂ for a specific system segment
    /// </summary>
    private double CalculateSegmentCO2(double energyKWh, double segmentPercentage, bool useGreenHosting)
    {
        var segmentEnergy = energyKWh * segmentPercentage;
        
        // If green hosting is enabled and this is the data center segment, use 0 carbon intensity
        var carbonIntensity = (useGreenHosting && _options.GreenHosting) 
            ? 0 
            : _options.GridIntensityGramsCO2PerKWh;
        
        return segmentEnergy * carbonIntensity;
    }
}
