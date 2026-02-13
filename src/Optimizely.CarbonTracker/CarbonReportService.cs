using Optimizely.CarbonTracker.Analysis;
using Optimizely.CarbonTracker.Calculation;
using Optimizely.CarbonTracker.Models;
using Microsoft.Extensions.Logging;

namespace Optimizely.CarbonTracker;

/// <summary>
/// Service for generating complete carbon footprint reports
/// </summary>
public interface ICarbonReportService
{
    /// <summary>
    /// Generate a complete carbon report for a page URL
    /// </summary>
    Task<PageCarbonReport> GenerateReportAsync(string pageUrl, Guid contentGuid, CancellationToken cancellationToken = default);
}

public class CarbonReportService : ICarbonReportService
{
    private readonly IPageAnalyzer _pageAnalyzer;
    private readonly ICarbonCalculator _carbonCalculator;
    private readonly ILogger<CarbonReportService> _logger;
    
    public CarbonReportService(
        IPageAnalyzer pageAnalyzer,
        ICarbonCalculator carbonCalculator,
        ILogger<CarbonReportService> logger)
    {
        _pageAnalyzer = pageAnalyzer;
        _carbonCalculator = carbonCalculator;
        _logger = logger;
    }
    
    public async Task<PageCarbonReport> GenerateReportAsync(string pageUrl, Guid contentGuid, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating carbon report for {PageUrl}", pageUrl);
        
        // Analyze the page
        var analysisResult = await _pageAnalyzer.AnalyzeAsync(pageUrl, cancellationToken);
        
        if (!analysisResult.Success)
        {
            throw new InvalidOperationException($"Failed to analyze page: {analysisResult.ErrorMessage}");
        }
        
        // Calculate CO₂ emissions
        var co2Grams = _carbonCalculator.CalculateCO2Grams(analysisResult.TotalTransferSizeBytes);
        var greenScore = _carbonCalculator.CalculateGreenScore(co2Grams);
        
        // Group resources by category
        var assetBreakdowns = analysisResult.Resources
            .GroupBy(r => r.Category)
            .Select(g => new AssetBreakdown
            {
                Category = g.Key,
                TransferSizeBytes = g.Sum(r => r.TransferSizeBytes),
                Percentage = analysisResult.TotalTransferSizeBytes > 0 
                    ? (g.Sum(r => r.TransferSizeBytes) / analysisResult.TotalTransferSizeBytes) * 100 
                    : 0,
                EstimatedCO2Grams = _carbonCalculator.CalculateCategoryCO2(g.Sum(r => r.TransferSizeBytes)),
                ResourceCount = g.Count()
            })
            .OrderByDescending(a => a.TransferSizeBytes)
            .ToList();
        
        // Create the report
        var report = new PageCarbonReport
        {
            ContentGuid = contentGuid,
            PageUrl = pageUrl,
            AnalyzedAt = analysisResult.AnalyzedAt,
            TotalTransferSizeBytes = analysisResult.TotalTransferSizeBytes,
            EstimatedCO2Grams = co2Grams,
            Score = greenScore,
            Assets = assetBreakdowns,
            Suggestions = analysisResult.Suggestions.OrderByDescending(s => s.PotentialCO2SavingsGrams).ToList()
        };
        
        _logger.LogInformation("Report generated: {Score} ({CO2}g CO₂)", greenScore, co2Grams);
        
        return report;
    }
}
