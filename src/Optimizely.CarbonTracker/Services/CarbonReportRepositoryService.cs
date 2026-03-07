using Microsoft.EntityFrameworkCore;
using Optimizely.CarbonTracker.Data;
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
/// EF Core implementation of ICarbonReportRepository for production use.
/// </summary>
public class CarbonReportRepositoryService(
    CarbonTrackerDbContext dbContext,
    ICarbonCalculatorService calculator) : ICarbonReportRepository
{
    private readonly CarbonTrackerDbContext _dbContext = dbContext;
    private readonly ICarbonCalculatorService _calculator = calculator;

    /// <inheritdoc/>
    public async Task SaveReportAsync(PageCarbonReport report, CancellationToken cancellationToken = default)
    {
        if (report.Id == 0)
        {
            _dbContext.PageCarbonReports.Add(report);
        }
        else
        {
            _dbContext.PageCarbonReports.Update(report);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<PageCarbonReport?> GetLatestReportAsync(Guid contentGuid, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PageCarbonReports
            .Include(r => r.Assets)
            .Include(r => r.Suggestions)
            .Where(r => r.ContentGuid == contentGuid)
            .OrderByDescending(r => r.AnalyzedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<PageCarbonReport>> GetHistoryAsync(Guid contentGuid, int days = 30, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);
        return await _dbContext.PageCarbonReports
            .Include(r => r.Assets)
            .Include(r => r.Suggestions)
            .Where(r => r.ContentGuid == contentGuid && r.AnalyzedAt >= cutoff)
            .OrderByDescending(r => r.AnalyzedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<SiteCarbonSummary> GetSiteSummaryAsync(CancellationToken cancellationToken = default)
    {
        // Get the latest report per content item
        var latestReportIds = await _dbContext.PageCarbonReports
            .GroupBy(r => r.ContentGuid)
            .Select(g => g.OrderByDescending(r => r.AnalyzedAt).First().Id)
            .ToListAsync(cancellationToken);

        var latestReports = await _dbContext.PageCarbonReports
            .Include(r => r.Assets)
            .Include(r => r.Suggestions)
            .Where(r => latestReportIds.Contains(r.Id))
            .ToListAsync(cancellationToken);

        var avgCO2 = latestReports.Count > 0 ? latestReports.Average(r => r.EstimatedCO2Grams) : 0;

        return new SiteCarbonSummary
        {
            TotalPagesAnalyzed = latestReports.Count,
            AverageCO2PerPage = avgCO2,
            AverageGreenScore = _calculator.CalculateGreenScore(avgCO2),
            TotalEstimatedCO2Grams = latestReports.Sum(r => r.EstimatedCO2Grams),
            WorstPages = latestReports.OrderByDescending(r => r.EstimatedCO2Grams).Take(5).ToList(),
            BestPages = latestReports.OrderBy(r => r.EstimatedCO2Grams).Take(5).ToList(),
            GeneratedAt = DateTime.UtcNow
        };
    }

    /// <inheritdoc/>
    public async Task CleanupOldReportsAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var oldReports = await _dbContext.PageCarbonReports
            .Where(r => r.AnalyzedAt < cutoff)
            .ToListAsync(cancellationToken);

        _dbContext.PageCarbonReports.RemoveRange(oldReports);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<Guid>> GetAllTrackedContentGuidsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.PageCarbonReports
            .Where(r => r.ContentGuid != Guid.Empty)
            .Select(r => r.ContentGuid)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
