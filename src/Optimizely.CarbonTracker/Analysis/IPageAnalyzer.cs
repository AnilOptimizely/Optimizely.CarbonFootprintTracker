namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Interface for analyzing web pages to determine carbon footprint
/// </summary>
public interface IPageAnalyzer
{
    /// <summary>
    /// Analyze a page by its URL
    /// </summary>
    /// <param name="pageUrl">URL of the page to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Analysis result with discovered resources</returns>
    Task<PageAnalysisResult> AnalyzeAsync(string pageUrl, CancellationToken cancellationToken = default);
}
