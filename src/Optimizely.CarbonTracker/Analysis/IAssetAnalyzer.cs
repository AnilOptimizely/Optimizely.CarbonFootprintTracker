using HtmlAgilityPack;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Interface for analyzing specific asset types
/// </summary>
public interface IAssetAnalyzer
{
    /// <summary>
    /// Analyze resources and generate optimization suggestions
    /// </summary>
    List<OptimizationSuggestion> Analyze(List<DiscoveredResource> resources, HtmlDocument htmlDoc);
}

/// <summary>
/// Interface for image-specific analysis
/// </summary>
public interface IImageAnalyzer : IAssetAnalyzer { }

/// <summary>
/// Interface for script-specific analysis
/// </summary>
public interface IScriptAnalyzer : IAssetAnalyzer { }

/// <summary>
/// Interface for video-specific analysis
/// </summary>
public interface IVideoAnalyzer : IAssetAnalyzer { }
