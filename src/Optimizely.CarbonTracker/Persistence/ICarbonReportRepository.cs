using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Persistence;

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
/// Site-wide carbon summary
/// </summary>
public class SiteCarbonSummary
{
    public int TotalPagesAnalyzed { get; set; }
    public double AverageScore { get; set; }
    public GreenScore AverageGreenScore { get; set; }
    public double TotalEstimatedCO2Grams { get; set; }
    public double AverageCO2PerPage { get; set; }
    public List<PageCarbonReport> WorstPages { get; set; } = new();
    public List<PageCarbonReport> BestPages { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
