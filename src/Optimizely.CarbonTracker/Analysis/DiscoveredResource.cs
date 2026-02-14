using Optimizely.CarbonTracker.Models;

namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Represents a discovered web resource during page analysis
/// </summary>
public class DiscoveredResource
{
    /// <summary>
    /// URL of the resource
    /// </summary>
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Asset category
    /// </summary>
    public AssetCategory Category { get; set; }
    
    /// <summary>
    /// Transfer size in bytes
    /// </summary>
    public double TransferSizeBytes { get; set; }
    
    /// <summary>
    /// MIME type/content type
    /// </summary>
    public string? ContentType { get; set; }
    
    /// <summary>
    /// Whether the resource loaded successfully
    /// </summary>
    public bool LoadedSuccessfully { get; set; }
    
    /// <summary>
    /// Additional attributes for analyzers (e.g., loading="lazy", async, defer)
    /// </summary>
    public Dictionary<string, string> Attributes { get; set; } = new();
}
