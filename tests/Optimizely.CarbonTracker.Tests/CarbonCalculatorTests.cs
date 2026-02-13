using Microsoft.Extensions.Options;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Configuration;
using Optimizely.CarbonTracker.Models;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Calculation;

public class CarbonCalculatorTests
{
    private readonly CarbonTrackerOptions _defaultOptions = new();
    
    [Fact]
    public void CalculateCO2Grams_WithZeroBytes_ReturnsZero()
    {
        // Arrange
        var calculator = CreateCalculator();
        
        // Act
        var result = calculator.CalculateCO2Grams(0);
        
        // Assert
        Assert.Equal(0, result);
    }
    
    [Theory]
    [InlineData(1024 * 1024, 0.36)] // 1MB should be ~0.36g CO₂
    [InlineData(2 * 1024 * 1024, 0.72)] // 2MB should be ~0.72g CO₂
    public void CalculateCO2Grams_FirstVisit_CalculatesCorrectly(double bytes, double expectedCO2)
    {
        // Arrange
        var calculator = CreateCalculator();
        
        // Act
        var result = calculator.CalculateCO2Grams(bytes, isReturningVisit: false);
        
        // Assert - Allow 10% tolerance for rounding
        Assert.InRange(result, expectedCO2 * 0.9, expectedCO2 * 1.1);
    }
    
    [Fact]
    public void CalculateCO2Grams_ReturningVisit_Uses2PercentCache()
    {
        // Arrange
        var calculator = CreateCalculator();
        var bytes = 1024 * 1024; // 1MB
        
        // Act
        var firstVisit = calculator.CalculateCO2Grams(bytes, isReturningVisit: false);
        var returningVisit = calculator.CalculateCO2Grams(bytes, isReturningVisit: true);
        
        // Assert - Returning visit should be ~2% of first visit
        Assert.InRange(returningVisit, firstVisit * 0.01, firstVisit * 0.03);
    }
    
    [Fact]
    public void CalculateCO2Grams_WithGreenHosting_ReducesDataCenterEmissions()
    {
        // Arrange
        var standardOptions = new CarbonTrackerOptions { GreenHosting = false };
        var greenOptions = new CarbonTrackerOptions { GreenHosting = true };
        
        var standardCalculator = CreateCalculator(standardOptions);
        var greenCalculator = CreateCalculator(greenOptions);
        
        var bytes = 1024 * 1024; // 1MB
        
        // Act
        var standardCO2 = standardCalculator.CalculateCO2Grams(bytes);
        var greenCO2 = greenCalculator.CalculateCO2Grams(bytes);
        
        // Assert - Green hosting should reduce emissions
        Assert.True(greenCO2 < standardCO2, "Green hosting should reduce CO2 emissions");
        
        // The reduction should be approximately 15% (data center segment)
        var expectedReduction = standardCO2 * 0.15;
        var actualReduction = standardCO2 - greenCO2;
        Assert.InRange(actualReduction, expectedReduction * 0.9, expectedReduction * 1.1);
    }
    
    [Fact]
    public void CalculateCO2Grams_WithCustomGridIntensity_UsesCustomValue()
    {
        // Arrange
        var lowCarbonOptions = new CarbonTrackerOptions { GridIntensityGramsCO2PerKWh = 100 }; // Clean grid
        var highCarbonOptions = new CarbonTrackerOptions { GridIntensityGramsCO2PerKWh = 800 }; // Dirty grid
        
        var lowCalculator = CreateCalculator(lowCarbonOptions);
        var highCalculator = CreateCalculator(highCarbonOptions);
        
        var bytes = 1024 * 1024; // 1MB
        
        // Act
        var lowCO2 = lowCalculator.CalculateCO2Grams(bytes);
        var highCO2 = highCalculator.CalculateCO2Grams(bytes);
        
        // Assert
        Assert.True(highCO2 > lowCO2, "Higher grid intensity should produce more CO2");
        
        // The ratio should approximately match the grid intensity ratio (800/100 = 8)
        var ratio = highCO2 / lowCO2;
        Assert.InRange(ratio, 7, 9);
    }
    
    [Theory]
    [InlineData(0.15, GreenScore.A)]
    [InlineData(0.20, GreenScore.A)]
    [InlineData(0.21, GreenScore.B)]
    [InlineData(0.50, GreenScore.B)]
    [InlineData(0.51, GreenScore.C)]
    [InlineData(1.00, GreenScore.C)]
    [InlineData(1.01, GreenScore.D)]
    [InlineData(2.00, GreenScore.D)]
    [InlineData(2.01, GreenScore.F)]
    [InlineData(10.00, GreenScore.F)]
    public void CalculateGreenScore_MapsCorrectly(double co2Grams, GreenScore expectedScore)
    {
        // Arrange
        var calculator = CreateCalculator();
        
        // Act
        var score = calculator.CalculateGreenScore(co2Grams);
        
        // Assert
        Assert.Equal(expectedScore, score);
    }
    
    [Fact]
    public void CalculateCategoryCO2_SameAsMainCalculation()
    {
        // Arrange
        var calculator = CreateCalculator();
        var bytes = 500 * 1024; // 500KB
        
        // Act
        var mainResult = calculator.CalculateCO2Grams(bytes);
        var categoryResult = calculator.CalculateCategoryCO2(bytes);
        
        // Assert
        Assert.Equal(mainResult, categoryResult);
    }
    
    private CarbonCalculator CreateCalculator(CarbonTrackerOptions? options = null)
    {
        var opts = Options.Create(options ?? _defaultOptions);
        return new CarbonCalculator(opts);
    }
}
