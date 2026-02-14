using HtmlAgilityPack;
using Moq;
using Optimizely.CarbonTracker.Analysis;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;
using Xunit;

namespace Optimizely.CarbonTracker.Tests.Analysis;

public class ScriptAnalyzerTests
{
    [Fact]
    public void Analyze_NoScripts_ReturnsEmptyList()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        var analyzer = new ScriptAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>();
        var htmlDoc = new HtmlDocument();

        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);

        // Assert
        Assert.Empty(suggestions);
    }

    [Fact]
    public void Analyze_RenderBlockingScripts_SuggestsAsyncOrDefer()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        var analyzer = new ScriptAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "https://example.com/app.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 50 * 1024,
                Attributes = new Dictionary<string, string>()
            },
            new()
            {
                Url = "https://example.com/vendor.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 80 * 1024,
                Attributes = new Dictionary<string, string>()
            }
        };
        var htmlDoc = new HtmlDocument();

        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);

        // Assert
        var renderBlockingSuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("async or defer"));
        Assert.NotNull(renderBlockingSuggestion);
        Assert.Equal(SuggestionSeverity.High, renderBlockingSuggestion.Severity);
        Assert.Contains("2 scripts", renderBlockingSuggestion.Description);
    }

    [Fact]
    public void Analyze_ScriptsWithAsyncOrDefer_DoesNotSuggestRenderBlocking()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        var analyzer = new ScriptAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "https://example.com/app.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 50 * 1024,
                Attributes = new Dictionary<string, string> { { "async", "async" } }
            },
            new()
            {
                Url = "https://example.com/vendor.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 80 * 1024,
                Attributes = new Dictionary<string, string> { { "defer", "defer" } }
            }
        };
        var htmlDoc = new HtmlDocument();

        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);

        // Assert
        var renderBlockingSuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("async or defer"));
        Assert.Null(renderBlockingSuggestion);
    }

    [Fact]
    public void Analyze_LargeBundles_SuggestsCodeSplitting()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        mockCalculator.Setup(c => c.CalculateCO2Grams(It.IsAny<double>(), false))
            .Returns((double bytes, bool returning) => bytes * 0.0000004);

        var analyzer = new ScriptAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "https://example.com/bundle.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 200 * 1024, // 200KB > 100KB threshold
                Attributes = new Dictionary<string, string> { { "async", "async" } }
            }
        };
        var htmlDoc = new HtmlDocument();

        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);

        // Assert
        var bundleSuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("large JavaScript bundles"));
        Assert.NotNull(bundleSuggestion);
        Assert.Equal(SuggestionSeverity.High, bundleSuggestion.Severity);
        Assert.True(bundleSuggestion.PotentialSavingsBytes > 0);
        Assert.Contains("bundle.js", bundleSuggestion.AffectedAssetUrl);
    }

    [Fact]
    public void Analyze_ThirdPartyScripts_SuggestsReview()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        mockCalculator.Setup(c => c.CalculateCO2Grams(It.IsAny<double>(), false))
            .Returns((double bytes, bool returning) => bytes * 0.0000004);

        var analyzer = new ScriptAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "https://www.google-analytics.com/analytics.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 40 * 1024,
                Attributes = new Dictionary<string, string> { { "async", "async" } }
            },
            new()
            {
                Url = "https://connect.facebook.net/sdk.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 60 * 1024,
                Attributes = new Dictionary<string, string> { { "async", "async" } }
            }
        };
        var htmlDoc = new HtmlDocument();

        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);

        // Assert
        var thirdPartySuggestion = suggestions.FirstOrDefault(s => s.Title.Contains("third-party"));
        Assert.NotNull(thirdPartySuggestion);
        Assert.Equal(SuggestionSeverity.Medium, thirdPartySuggestion.Severity);
        Assert.Contains("2 third-party scripts", thirdPartySuggestion.Description);
    }

    [Fact]
    public void Analyze_SmallFirstPartyScripts_NoSuggestions()
    {
        // Arrange
        var mockCalculator = new Mock<ICarbonCalculator>();
        var analyzer = new ScriptAnalyzer(mockCalculator.Object);
        var resources = new List<DiscoveredResource>
        {
            new()
            {
                Url = "https://example.com/small.js",
                Category = AssetCategory.JavaScript,
                TransferSizeBytes = 10 * 1024, // 10KB - small, not third-party
                Attributes = new Dictionary<string, string> { { "defer", "defer" } }
            }
        };
        var htmlDoc = new HtmlDocument();

        // Act
        var suggestions = analyzer.Analyze(resources, htmlDoc);

        // Assert
        Assert.Empty(suggestions);
    }
}
