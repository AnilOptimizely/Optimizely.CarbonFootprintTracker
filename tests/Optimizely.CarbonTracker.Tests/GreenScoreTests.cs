using Microsoft.Extensions.Options;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Configuration;
using Optimizely.CarbonTracker.Models;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Calculation;

public class GreenScoreTests
{
    private readonly CarbonCalculator _calculator;

    public GreenScoreTests()
    {
        var options = Options.Create(new CarbonTrackerOptions());
        _calculator = new CarbonCalculator(options);
    }

    [Theory]
    [InlineData(0.0, GreenScore.A)]
    [InlineData(0.10, GreenScore.A)]
    [InlineData(0.20, GreenScore.A)]
    public void GreenScore_A_AtOrBelow020g(double co2Grams, GreenScore expected)
    {
        Assert.Equal(expected, _calculator.CalculateGreenScore(co2Grams));
    }

    [Theory]
    [InlineData(0.21, GreenScore.B)]
    [InlineData(0.35, GreenScore.B)]
    [InlineData(0.50, GreenScore.B)]
    public void GreenScore_B_Between021And050g(double co2Grams, GreenScore expected)
    {
        Assert.Equal(expected, _calculator.CalculateGreenScore(co2Grams));
    }

    [Theory]
    [InlineData(0.51, GreenScore.C)]
    [InlineData(0.75, GreenScore.C)]
    [InlineData(1.00, GreenScore.C)]
    public void GreenScore_C_Between051And100g(double co2Grams, GreenScore expected)
    {
        Assert.Equal(expected, _calculator.CalculateGreenScore(co2Grams));
    }

    [Theory]
    [InlineData(1.01, GreenScore.D)]
    [InlineData(1.50, GreenScore.D)]
    [InlineData(2.00, GreenScore.D)]
    public void GreenScore_D_Between101And200g(double co2Grams, GreenScore expected)
    {
        Assert.Equal(expected, _calculator.CalculateGreenScore(co2Grams));
    }

    [Theory]
    [InlineData(2.01, GreenScore.F)]
    [InlineData(5.00, GreenScore.F)]
    [InlineData(10.00, GreenScore.F)]
    public void GreenScore_F_Above200g(double co2Grams, GreenScore expected)
    {
        Assert.Equal(expected, _calculator.CalculateGreenScore(co2Grams));
    }

    [Fact]
    public void GreenScore_BoundaryAt020_IsA()
    {
        Assert.Equal(GreenScore.A, _calculator.CalculateGreenScore(0.20));
    }

    [Fact]
    public void GreenScore_BoundaryAt021_IsB()
    {
        // 0.20 is the last A; anything above 0.20 should be B
        Assert.Equal(GreenScore.B, _calculator.CalculateGreenScore(0.201));
    }

    [Fact]
    public void GreenScore_BoundaryAt050_IsB()
    {
        Assert.Equal(GreenScore.B, _calculator.CalculateGreenScore(0.50));
    }

    [Fact]
    public void GreenScore_BoundaryAt051_IsC()
    {
        Assert.Equal(GreenScore.C, _calculator.CalculateGreenScore(0.501));
    }

    [Fact]
    public void GreenScore_BoundaryAt200_IsD()
    {
        Assert.Equal(GreenScore.D, _calculator.CalculateGreenScore(2.00));
    }

    [Fact]
    public void GreenScore_BoundaryAt201_IsF()
    {
        Assert.Equal(GreenScore.F, _calculator.CalculateGreenScore(2.001));
    }
}
