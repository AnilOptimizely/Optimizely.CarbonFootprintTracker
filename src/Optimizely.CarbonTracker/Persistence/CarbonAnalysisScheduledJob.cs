using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optimizely.CarbonTracker.Configuration;

namespace Optimizely.CarbonTracker.Persistence;

/// <summary>
/// Scheduled job that runs nightly to analyze all published pages
/// and store reports for trend tracking.
///
/// In an Optimizely CMS environment, this would extend ScheduledJobBase.
/// This implementation provides the core logic that can be invoked
/// by any scheduling mechanism (Optimizely Scheduled Jobs, Hangfire, etc.).
/// </summary>
public class CarbonAnalysisScheduledJob
{
    private readonly ICarbonReportService _reportService;
    private readonly ICarbonReportRepository _repository;
    private readonly CarbonTrackerOptions _options;
    private readonly ILogger<CarbonAnalysisScheduledJob> _logger;

    private bool _stopRequested;

    public CarbonAnalysisScheduledJob(
        ICarbonReportService reportService,
        ICarbonReportRepository repository,
        IOptions<CarbonTrackerOptions> options,
        ILogger<CarbonAnalysisScheduledJob> logger)
    {
        _reportService = reportService;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Request that the job stops at the next safe point
    /// </summary>
    public void Stop()
    {
        _stopRequested = true;
    }

    /// <summary>
    /// Execute the nightly analysis job.
    /// Analyzes all tracked pages and cleans up old reports.
    /// </summary>
    /// <returns>A summary message</returns>
    public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _stopRequested = false;
        _logger.LogInformation("Carbon Analysis Scheduled Job started");

        var analyzed = 0;
        var failed = 0;

        try
        {
            // Clean up old reports first
            await _repository.CleanupOldReportsAsync(_options.HistoryRetentionDays, cancellationToken);
            _logger.LogInformation("Cleaned up reports older than {Days} days", _options.HistoryRetentionDays);

            // Get all tracked content GUIDs
            var contentGuids = await _repository.GetAllTrackedContentGuidsAsync(cancellationToken);
            _logger.LogInformation("Found {Count} pages to analyze", contentGuids.Count);

            foreach (var contentGuid in contentGuids)
            {
                if (_stopRequested || cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("Job stop requested, halting analysis");
                    break;
                }

                try
                {
                    // Get the last known URL for this content
                    var latestReport = await _repository.GetLatestReportAsync(contentGuid, cancellationToken);
                    if (latestReport == null || string.IsNullOrEmpty(latestReport.PageUrl))
                    {
                        continue;
                    }

                    var report = await _reportService.GenerateReportAsync(
                        latestReport.PageUrl, contentGuid, cancellationToken);

                    await _repository.SaveReportAsync(report, cancellationToken);
                    analyzed++;

                    _logger.LogDebug("Analyzed {Url}: {Score} ({CO2}g CO₂)",
                        latestReport.PageUrl, report.Score, report.EstimatedCO2Grams);
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogWarning(ex, "Failed to analyze content {ContentGuid}", contentGuid);
                }
            }

            // Generate site-wide summary
            var summary = await _repository.GetSiteSummaryAsync(cancellationToken);
            _logger.LogInformation(
                "Site Carbon Budget: {TotalPages} pages, avg {AvgCO2:F2}g CO₂/page, total {TotalCO2:F2}g CO₂",
                summary.TotalPagesAnalyzed, summary.AverageCO2PerPage, summary.TotalEstimatedCO2Grams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Carbon Analysis Scheduled Job failed");
            return $"Job failed: {ex.Message}";
        }

        var resultMessage = $"Carbon Analysis complete: {analyzed} pages analyzed, {failed} failed";
        _logger.LogInformation(resultMessage);
        return resultMessage;
    }
}
