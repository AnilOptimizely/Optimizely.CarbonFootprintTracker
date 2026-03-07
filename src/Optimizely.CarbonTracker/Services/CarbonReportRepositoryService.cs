using System.Collections.Concurrent;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Services;

/// <summary>
/// Repository interface for persisting and retrieving carbon reports
/// </summary>
public interface ICarbonReportRepository
{
    /// <summary>
    /// Save a carbon report after analysis
    /// </summary>
    Task SaveReportAsync(PageCarbonReport report, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the latest report for a content item
    /// </summary>
    Task<PageCarbonReport?> GetLatestReportAsync(Guid contentGuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get historical reports for a content item within a date range
    /// </summary>
    Task<List<PageCarbonReport>> GetHistoryAsync(Guid contentGuid, int days = 30, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get site-wide aggregation summary
    /// </summary>
    Task<SiteCarbonSummary> GetSiteSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete reports older than the specified retention period
    /// </summary>
    Task CleanupOldReportsAsync(int retentionDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all unique content GUIDs that have reports
    /// </summary>
    Task<List<Guid>> GetAllTrackedContentGuidsAsync(CancellationToken cancellationToken = default);
}


/// <summary>
/// In-memory implementation of ICarbonReportRepository.
/// Replace with EF Core / DDS-backed implementation for production use.
/// </summary>
public class CarbonReportRepositoryService(ICarbonCalculatorService calculator) : ICarbonReportRepository
{
    private readonly ConcurrentDictionary<int, PageCarbonReport> _reports = new();
    private readonly ICarbonCalculatorService _calculator = calculator;
    private readonly object _saveLock = new();
    private int _nextId;

    /// <inheritdoc/>
    public Task SaveReportAsync(PageCarbonReport report, CancellationToken cancellationToken = default)
    {
        lock (_saveLock)
        {
            if (report.Id == 0)
            {
                report.Id = Interlocked.Increment(ref _nextId);
            }

            _reports[report.Id] = report;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<PageCarbonReport?> GetLatestReportAsync(Guid contentGuid, CancellationToken cancellationToken = default)
    {
        var report = _reports.Values
            .Where(r => r.ContentGuid == contentGuid)
            .OrderByDescending(r => r.AnalyzedAt)
            .FirstOrDefault();

        return Task.FromResult(report);
    }

    /// <inheritdoc/>
    public Task<List<PageCarbonReport>> GetHistoryAsync(Guid contentGuid, int days = 30, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var reports = _reports.Values
            .Where(r => r.ContentGuid == contentGuid && r.AnalyzedAt >= cutoff)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToList();

        return Task.FromResult(reports);
    }

    /// <inheritdoc/>
    public Task<SiteCarbonSummary> GetSiteSummaryAsync(CancellationToken cancellationToken = default)
    {
        // Get the latest report per content item
        var latestReports = _reports.Values
            .GroupBy(r => r.ContentGuid)
            .Select(g => g.OrderByDescending(r => r.AnalyzedAt).First())
            .ToList();

        var avgCO2 = latestReports.Count > 0 ? latestReports.Average(r => r.EstimatedCO2Grams) : 0;

        var summary = new SiteCarbonSummary
        {
            TotalPagesAnalyzed = latestReports.Count,
            AverageCO2PerPage = avgCO2,
            AverageGreenScore = _calculator.CalculateGreenScore(avgCO2),
            TotalEstimatedCO2Grams = latestReports.Sum(r => r.EstimatedCO2Grams),
            WorstPages = latestReports.OrderByDescending(r => r.EstimatedCO2Grams).Take(5).ToList(),
            BestPages = latestReports.OrderBy(r => r.EstimatedCO2Grams).Take(5).ToList(),
            GeneratedAt = DateTime.UtcNow
        };

        return Task.FromResult(summary);
    }

    /// <inheritdoc/>
    public Task CleanupOldReportsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var toRemove = _reports.Where(kvp => kvp.Value.AnalyzedAt < cutoff).Select(kvp => kvp.Key).ToList();
        foreach (var key in toRemove)
        {
            _reports.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<List<Guid>> GetAllTrackedContentGuidsAsync(CancellationToken cancellationToken = default)
    {
        var guids = _reports.Values
            .Select(r => r.ContentGuid)
            .Where(g => g != Guid.Empty)
            .Distinct()
            .ToList();

        return Task.FromResult(guids);
    }
}
