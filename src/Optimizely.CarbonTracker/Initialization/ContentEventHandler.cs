using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optimizely.CarbonTracker.Configuration;
using Optimizely.CarbonTracker.Models;
using Optimizely.CarbonTracker.Persistence;

namespace Optimizely.CarbonTracker.Initialization;

/// <summary>
/// Handles CMS content events (publish, save, workflow) to trigger carbon analysis.
///
/// In an Optimizely CMS environment, this integrates with IContentEvents.
/// This implementation provides the core event-handling logic that can be wired
/// to Optimizely's IContentEvents or any other content lifecycle system.
/// </summary>
public interface IContentEventHandler
{
    /// <summary>
    /// Handle content published event — queue full carbon analysis
    /// </summary>
    Task OnContentPublishedAsync(Guid contentGuid, string pageUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle content saved (draft) event — provide lightweight estimate
    /// </summary>
    Task OnContentSavedAsync(Guid contentGuid, long estimatedContentSizeBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if content meets carbon threshold for approval workflow
    /// </summary>
    Task<CarbonApprovalResult> CheckCarbonThresholdAsync(Guid contentGuid, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of carbon threshold check for workflow approval
/// </summary>
public class CarbonApprovalResult
{
    public bool Approved { get; set; }
    public GreenScore Score { get; set; }
    public GreenScore? PreviousScore { get; set; }
    public double EstimatedCO2Grams { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool ScoreWorsened { get; set; }
}

/// <inheritdoc />
public class ContentEventHandler : IContentEventHandler
{
    private readonly ICarbonReportService _reportService;
    private readonly ICarbonReportRepository _repository;
    private readonly CarbonTrackerOptions _options;
    private readonly ILogger<ContentEventHandler> _logger;

    public ContentEventHandler(
        ICarbonReportService reportService,
        ICarbonReportRepository repository,
        IOptions<CarbonTrackerOptions> options,
        ILogger<ContentEventHandler> logger)
    {
        _reportService = reportService;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task OnContentPublishedAsync(Guid contentGuid, string pageUrl, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableRealTimeAnalysis)
        {
            _logger.LogDebug("Real-time analysis disabled, skipping publish analysis for {ContentGuid}", contentGuid);
            return;
        }

        try
        {
            _logger.LogInformation("Content published: analyzing {PageUrl} ({ContentGuid})", pageUrl, contentGuid);

            // Get previous report for comparison
            var previousReport = await _repository.GetLatestReportAsync(contentGuid, cancellationToken);

            // Generate new report
            var report = await _reportService.GenerateReportAsync(pageUrl, contentGuid, cancellationToken);
            await _repository.SaveReportAsync(report, cancellationToken);

            // Log comparison if previous report exists
            if (previousReport != null)
            {
                var changeDirection = report.EstimatedCO2Grams > previousReport.EstimatedCO2Grams ? "worsened" : "improved";
                _logger.LogInformation(
                    "Carbon score {ChangeDirection} for {PageUrl}: {OldScore} ({OldCO2:F2}g) → {NewScore} ({NewCO2:F2}g)",
                    changeDirection, pageUrl,
                    previousReport.Score, previousReport.EstimatedCO2Grams,
                    report.Score, report.EstimatedCO2Grams);

                // Warn if score worsened to D or F
                if (report.Score >= GreenScore.D && report.Score > previousReport.Score)
                {
                    _logger.LogWarning(
                        "⚠️ Carbon score DEGRADED to {Score} for {PageUrl}. Consider reviewing recent changes.",
                        report.Score, pageUrl);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze published content {ContentGuid}", contentGuid);
        }
    }

    /// <inheritdoc />
    public Task OnContentSavedAsync(Guid contentGuid, long estimatedContentSizeBytes, CancellationToken cancellationToken = default)
    {
        if (!_options.EnableRealTimeAnalysis)
        {
            return Task.CompletedTask;
        }

        _logger.LogDebug("Content saved (draft): {ContentGuid}, estimated size: {Size} bytes",
            contentGuid, estimatedContentSizeBytes);

        // Lightweight estimate - just log for now; full analysis happens on publish
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<CarbonApprovalResult> CheckCarbonThresholdAsync(Guid contentGuid, CancellationToken cancellationToken = default)
    {
        var latestReport = await _repository.GetLatestReportAsync(contentGuid, cancellationToken);

        if (latestReport == null)
        {
            return new CarbonApprovalResult
            {
                Approved = true,
                Message = "No carbon report available. Approval granted by default."
            };
        }

        // Get the report before the latest to compare
        var history = await _repository.GetHistoryAsync(contentGuid, days: 90, cancellationToken);
        var previousReport = history.Count > 1 ? history[1] : null;

        var result = new CarbonApprovalResult
        {
            Score = latestReport.Score,
            PreviousScore = previousReport?.Score,
            EstimatedCO2Grams = latestReport.EstimatedCO2Grams,
            ScoreWorsened = previousReport != null && latestReport.Score > previousReport.Score
        };

        // Flag pages with D or F scores for review
        if (latestReport.Score >= GreenScore.D)
        {
            result.Approved = false;
            result.Message = $"Page has a Green Score of {latestReport.Score} ({latestReport.EstimatedCO2Grams:F2}g CO₂). " +
                             "Pages with D or F scores require review before publishing.";
        }
        else
        {
            result.Approved = true;
            result.Message = $"Carbon footprint approved: {latestReport.Score} ({latestReport.EstimatedCO2Grams:F2}g CO₂)";
        }

        return result;
    }
}
