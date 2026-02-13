using HtmlAgilityPack;
using Moq;
using Optimizely.CarbonTracker.Analysis;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Analysis;

public class ImageAnalyzerTests
{
    [Fact]
    public void Analyze_NoImages_ReturnsEmptyList()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        var analyzer = new ImageAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>();
        var htmlDoc = new HtmlDocument();
        
        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);
        
        // Assert
        Assert.Empty(suggestions);
    }
    
    [Fact]
    public void Analyze_ImagesWithoutLazyLoading_SuggestsLazyLoading()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        mockCalculator.Setup(c => c.CalculateCO2Grams(It.IsAny<double>(), false))
            .Returns((double bytes, bool returning) => bytes * 0.0000004);
        
        var analyzer = new ImageAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "image1.jpg",
                Category = AssetCategory.Images,
                TransferSizeBytes = 100 * 1024,
                Attributes = new Dictionary<string, string>()
            },
            new()
            {
                Url = "image2.jpg",
                Category = AssetCategory.Images,
                TransferSizeBytes = 200 * 1024,
                Attributes = new Dictionary<string, string>()
            }
        };
        var htmlDoc = new HtmlDocument();
        
        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);
        
        // Assert
        var lazySuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("lazy loading"));
        Assert.NotNull(lazySuggestion);
        Assert.Equal(SuggestionSeverity.Medium, lazySuggestion.Severity);
    }
    
    [Fact]
    public void Analyze_ImagesWithoutSrcset_SuggestsResponsiveImages()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        mockCalculator.Setup(c => c.CalculateCO2Grams(It.IsAny<double>(), false))
            .Returns((double bytes, bool returning) => bytes * 0.0000004);
        
        var analyzer = new ImageAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "image1.jpg",
                Category = AssetCategory.Images,
                TransferSizeBytes = 300 * 1024,
                Attributes = new Dictionary<string, string> { { "loading", "lazy" } }
            }
        };
        var htmlDoc = new HtmlDocument();
        
        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);
        
        // Assert
        var srcsetSuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("srcset"));
        Assert.NotNull(srcsetSuggestion);
        Assert.Equal(SuggestionSeverity.Medium, srcsetSuggestion.Severity);
    }
    
    [Fact]
    public void Analyze_LegacyFormats_SuggestsModernFormats()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        mockCalculator.Setup(c => c.CalculateCO2Grams(It.IsAny<double>(), false))
            .Returns((double bytes, bool returning) => bytes * 0.0000004);
        
        var analyzer = new ImageAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "image1.jpg",
                Category = AssetCategory.Images,
                TransferSizeBytes = 500 * 1024,
                Attributes = new Dictionary<string, string>()
            },
            new()
            {
                Url = "image2.png",
                Category = AssetCategory.Images,
                TransferSizeBytes = 600 * 1024,
                Attributes = new Dictionary<string, string>()
            }
        };
        var htmlDoc = new HtmlDocument();
        
        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);
        
        // Assert
        var modernFormatSuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("modern formats"));
        Assert.NotNull(modernFormatSuggestion);
        Assert.Equal(SuggestionSeverity.High, modernFormatSuggestion.Severity);
        Assert.True(modernFormatSuggestion.PotentialSavingsBytes > 0);
    }
    
    [Fact]
    public void Analyze_LargeImages_SuggestsOptimization()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        mockCalculator.Setup(c => c.CalculateCO2Grams(It.IsAny<double>(), false))
            .Returns((double bytes, bool returning) => bytes * 0.0000004);
        
        var analyzer = new ImageAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "large-image.jpg",
                Category = AssetCategory.Images,
                TransferSizeBytes = 500 * 1024, // 500KB
                Attributes = new Dictionary<string, string>()
            }
        };
        var htmlDoc = new HtmlDocument();
        
        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);
        
        // Assert
        var optimizeSuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("large images"));
        Assert.NotNull(optimizeSuggestion);
        Assert.Equal(SuggestionSeverity.High, optimizeSuggestion.Severity);
        Assert.Contains("large-image.jpg", optimizeSuggestion.AffectedAssetUrl);
    }
}
