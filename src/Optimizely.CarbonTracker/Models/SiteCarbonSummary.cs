namespace Optimizely.CarbonTracker.Models;

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
