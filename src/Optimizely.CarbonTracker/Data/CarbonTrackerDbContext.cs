using Microsoft.EntityFrameworkCore;
using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Data;

/// <summary>
/// EF Core DbContext for persisting carbon footprint reports
/// </summary>
public class CarbonTrackerDbContext(DbContextOptions<CarbonTrackerDbContext> options) : DbContext(options)
{
    public DbSet<PageCarbonReport> PageCarbonReports => Set<PageCarbonReport>();
    public DbSet<AssetBreakdown> AssetBreakdowns => Set<AssetBreakdown>();
    public DbSet<OptimizationSuggestion> OptimizationSuggestions => Set<OptimizationSuggestion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PageCarbonReport>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ContentGuid);
            entity.HasIndex(e => e.AnalyzedAt);

            entity.HasMany(e => e.Assets)
                .WithOne()
                .HasForeignKey(a => a.PageCarbonReportId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Suggestions)
                .WithOne()
                .HasForeignKey(s => s.PageCarbonReportId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AssetBreakdown>(entity =>
        {
            entity.HasKey(e => e.Id);
        });

        modelBuilder.Entity<OptimizationSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
        });
    }
}
