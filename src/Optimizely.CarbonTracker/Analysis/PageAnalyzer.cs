using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Optimizely.CarbonTracker.Configuration;
using Optimizely.CarbonTracker.Models;
using System.Text.RegularExpressions;

namespace Optimizely.CarbonTracker.Analysis;

/// <summary>
/// Analyzes web pages to extract resources and calculate transfer sizes
/// </summary>
public class PageAnalyzer : IPageAnalyzer
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PageAnalyzer> _logger;
    private readonly CarbonTrackerOptions _options;
    private readonly IImageAnalyzer _imageAnalyzer;
    private readonly IScriptAnalyzer _scriptAnalyzer;
    private readonly IVideoAnalyzer _videoAnalyzer;
    
    public PageAnalyzer(
        HttpClient httpClient,
        ILogger<PageAnalyzer> logger,
        IOptions<CarbonTrackerOptions> options,
        IImageAnalyzer imageAnalyzer,
        IScriptAnalyzer scriptAnalyzer,
        IVideoAnalyzer videoAnalyzer)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _imageAnalyzer = imageAnalyzer;
        _scriptAnalyzer = scriptAnalyzer;
        _videoAnalyzer = videoAnalyzer;
        
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.PageAnalysisTimeoutSeconds);
    }
    
    /// <inheritdoc/>
    public async Task<PageAnalysisResult> AnalyzeAsync(string pageUrl, CancellationToken cancellationToken = default)
    {
        var result = new PageAnalysisResult
        {
            PageUrl = pageUrl,
            AnalyzedAt = DateTime.UtcNow
        };
        
        try
        {
            _logger.LogInformation("Starting analysis for page: {PageUrl}", pageUrl);
            
            // Fetch the HTML
            var htmlResponse = await _httpClient.GetAsync(pageUrl, cancellationToken);
            htmlResponse.EnsureSuccessStatusCode();
            
            var htmlContent = await htmlResponse.Content.ReadAsStringAsync(cancellationToken);
            var htmlSize = htmlContent.Length;
            
            // Add HTML as a resource
            result.Resources.Add(new DiscoveredResource
            {
                Url = pageUrl,
                Category = AssetCategory.HTML,
                TransferSizeBytes = htmlSize,
                ContentType = "text/html",
                LoadedSuccessfully = true
            });
            
            // Parse HTML
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);
            
            // Extract resources
            await ExtractImagesAsync(htmlDoc, pageUrl, result, cancellationToken);
            await ExtractStylesheetsAsync(htmlDoc, pageUrl, result, cancellationToken);
            await ExtractScriptsAsync(htmlDoc, pageUrl, result, cancellationToken);
            await ExtractVideosAsync(htmlDoc, pageUrl, result, cancellationToken);
            await ExtractFontsAsync(htmlDoc, pageUrl, result, cancellationToken);
            
            // Calculate total size
            result.TotalTransferSizeBytes = result.Resources.Sum(r => r.TransferSizeBytes);
            
            // Run specialized analyzers for suggestions
            result.Suggestions.AddRange(_imageAnalyzer.Analyze(result.Resources, htmlDoc));
            result.Suggestions.AddRange(_scriptAnalyzer.Analyze(result.Resources, htmlDoc));
            result.Suggestions.AddRange(_videoAnalyzer.Analyze(result.Resources, htmlDoc));
            
            result.Success = true;
            _logger.LogInformation("Analysis completed for {PageUrl}. Total size: {TotalSize} bytes", 
                pageUrl, result.TotalTransferSizeBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing page: {PageUrl}", pageUrl);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }
        
        return result;
    }
    
    private async Task ExtractImagesAsync(HtmlDocument doc, string baseUrl, PageAnalysisResult result, CancellationToken cancellationToken)
    {
        var imageNodes = doc.DocumentNode.SelectNodes("//img | //picture//source | //source[@type]");
        if (imageNodes == null) return;
        
        foreach (var node in imageNodes)
        {
            var src = node.GetAttributeValue("src", null) 
                      ?? node.GetAttributeValue("data-src", null)
                      ?? node.GetAttributeValue("srcset", null)?.Split(',').FirstOrDefault()?.Split(' ').FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(src)) continue;
            
            var absoluteUrl = MakeAbsoluteUrl(src.Trim(), baseUrl);
            if (absoluteUrl == null) continue;
            
            var resource = await FetchResourceAsync(absoluteUrl, AssetCategory.Images, cancellationToken);
            if (resource != null)
            {
                // Capture attributes for analyzer
                resource.Attributes["loading"] = node.GetAttributeValue("loading", "");
                resource.Attributes["srcset"] = node.GetAttributeValue("srcset", "");
                result.Resources.Add(resource);
            }
        }
    }
    
    private async Task ExtractStylesheetsAsync(HtmlDocument doc, string baseUrl, PageAnalysisResult result, CancellationToken cancellationToken)
    {
        var linkNodes = doc.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
        if (linkNodes == null) return;
        
        foreach (var node in linkNodes)
        {
            var href = node.GetAttributeValue("href", null);
            if (string.IsNullOrWhiteSpace(href)) continue;
            
            var absoluteUrl = MakeAbsoluteUrl(href.Trim(), baseUrl);
            if (absoluteUrl == null) continue;
            
            var resource = await FetchResourceAsync(absoluteUrl, AssetCategory.CSS, cancellationToken);
            if (resource != null)
            {
                result.Resources.Add(resource);
            }
        }
        
        // Also count inline styles
        var styleNodes = doc.DocumentNode.SelectNodes("//style");
        if (styleNodes != null)
        {
            var inlineStyleSize = styleNodes.Sum(n => n.InnerText.Length);
            if (inlineStyleSize > 0)
            {
                result.Resources.Add(new DiscoveredResource
                {
                    Url = $"{baseUrl}#inline-styles",
                    Category = AssetCategory.CSS,
                    TransferSizeBytes = inlineStyleSize,
                    ContentType = "text/css",
                    LoadedSuccessfully = true
                });
            }
        }
    }
    
    private async Task ExtractScriptsAsync(HtmlDocument doc, string baseUrl, PageAnalysisResult result, CancellationToken cancellationToken)
    {
        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@src]");
        if (scriptNodes == null) return;
        
        foreach (var node in scriptNodes)
        {
            var src = node.GetAttributeValue("src", null);
            if (string.IsNullOrWhiteSpace(src)) continue;
            
            var absoluteUrl = MakeAbsoluteUrl(src.Trim(), baseUrl);
            if (absoluteUrl == null) continue;
            
            var resource = await FetchResourceAsync(absoluteUrl, AssetCategory.JavaScript, cancellationToken);
            if (resource != null)
            {
                resource.Attributes["async"] = node.GetAttributeValue("async", "");
                resource.Attributes["defer"] = node.GetAttributeValue("defer", "");
                result.Resources.Add(resource);
            }
        }
        
        // Also count inline scripts
        var inlineScriptNodes = doc.DocumentNode.SelectNodes("//script[not(@src)]");
        if (inlineScriptNodes != null)
        {
            var inlineScriptSize = inlineScriptNodes.Sum(n => n.InnerText.Length);
            if (inlineScriptSize > 0)
            {
                result.Resources.Add(new DiscoveredResource
                {
                    Url = $"{baseUrl}#inline-scripts",
                    Category = AssetCategory.JavaScript,
                    TransferSizeBytes = inlineScriptSize,
                    ContentType = "application/javascript",
                    LoadedSuccessfully = true
                });
            }
        }
    }
    
    private async Task ExtractVideosAsync(HtmlDocument doc, string baseUrl, PageAnalysisResult result, CancellationToken cancellationToken)
    {
        // Video elements
        var videoNodes = doc.DocumentNode.SelectNodes("//video//source | //video[@src]");
        if (videoNodes != null)
        {
            foreach (var node in videoNodes)
            {
                var src = node.GetAttributeValue("src", null);
                if (string.IsNullOrWhiteSpace(src)) continue;
                
                var absoluteUrl = MakeAbsoluteUrl(src.Trim(), baseUrl);
                if (absoluteUrl == null) continue;
                
                var resource = await FetchResourceAsync(absoluteUrl, AssetCategory.Video, cancellationToken);
                if (resource != null)
                {
                    var videoParent = node.ParentNode?.Name == "video" ? node.ParentNode : node;
                    resource.Attributes["autoplay"] = videoParent.GetAttributeValue("autoplay", "");
                    resource.Attributes["preload"] = videoParent.GetAttributeValue("preload", "");
                    result.Resources.Add(resource);
                }
            }
        }
        
        // YouTube/Vimeo embeds
        var iframeNodes = doc.DocumentNode.SelectNodes("//iframe[contains(@src, 'youtube.com') or contains(@src, 'vimeo.com')]");
        if (iframeNodes != null)
        {
            foreach (var node in iframeNodes)
            {
                var src = node.GetAttributeValue("src", "");
                // Estimate ~800KB for embedded video players
                result.Resources.Add(new DiscoveredResource
                {
                    Url = src,
                    Category = AssetCategory.Video,
                    TransferSizeBytes = 800 * 1024, // 800KB estimate
                    ContentType = "video/embed",
                    LoadedSuccessfully = true
                });
            }
        }
    }
    
    private async Task ExtractFontsAsync(HtmlDocument doc, string baseUrl, PageAnalysisResult result, CancellationToken cancellationToken)
    {
        // Font preload links
        var fontPreloadNodes = doc.DocumentNode.SelectNodes("//link[@rel='preload' and @as='font']");
        if (fontPreloadNodes != null)
        {
            foreach (var node in fontPreloadNodes)
            {
                var href = node.GetAttributeValue("href", null);
                if (string.IsNullOrWhiteSpace(href)) continue;
                
                var absoluteUrl = MakeAbsoluteUrl(href.Trim(), baseUrl);
                if (absoluteUrl == null) continue;
                
                var resource = await FetchResourceAsync(absoluteUrl, AssetCategory.Fonts, cancellationToken);
                if (resource != null)
                {
                    result.Resources.Add(resource);
                }
            }
        }
        
        // Parse CSS for @font-face URLs (simplified - would need full CSS parser for production)
        var styleNodes = doc.DocumentNode.SelectNodes("//style");
        if (styleNodes != null)
        {
            var fontUrlRegex = new Regex(@"@font-face[^}]*url\(['""]?([^'"")]+)['""]?\)", RegexOptions.IgnoreCase);
            foreach (var styleNode in styleNodes)
            {
                var matches = fontUrlRegex.Matches(styleNode.InnerText);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var fontUrl = match.Groups[1].Value;
                        var absoluteUrl = MakeAbsoluteUrl(fontUrl, baseUrl);
                        if (absoluteUrl != null)
                        {
                            var resource = await FetchResourceAsync(absoluteUrl, AssetCategory.Fonts, cancellationToken);
                            if (resource != null)
                            {
                                result.Resources.Add(resource);
                            }
                        }
                    }
                }
            }
        }
    }
    
    private async Task<DiscoveredResource?> FetchResourceAsync(string url, AssetCategory category, CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ResourceTimeoutSeconds));
            
            // Try HEAD first to get Content-Length
            var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            var headResponse = await _httpClient.SendAsync(headRequest, cts.Token);
            
            if (headResponse.IsSuccessStatusCode && headResponse.Content.Headers.ContentLength.HasValue)
            {
                return new DiscoveredResource
                {
                    Url = url,
                    Category = category,
                    TransferSizeBytes = headResponse.Content.Headers.ContentLength.Value,
                    ContentType = headResponse.Content.Headers.ContentType?.MediaType,
                    LoadedSuccessfully = true
                };
            }
            
            // Fall back to GET if HEAD doesn't work
            var getResponse = await _httpClient.GetAsync(url, cts.Token);
            if (getResponse.IsSuccessStatusCode)
            {
                var content = await getResponse.Content.ReadAsByteArrayAsync(cts.Token);
                return new DiscoveredResource
                {
                    Url = url,
                    Category = category,
                    TransferSizeBytes = content.Length,
                    ContentType = getResponse.Content.Headers.ContentType?.MediaType,
                    LoadedSuccessfully = true
                };
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Timeout fetching resource: {Url}", url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching resource: {Url}", url);
        }
        
        return null;
    }
    
    private string? MakeAbsoluteUrl(string url, string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        
        // Skip data URIs
        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;
        
        // Already absolute
        if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            return url;
        
        // Make relative URL absolute
        if (Uri.TryCreate(new Uri(baseUrl), url, out var absoluteUri))
            return absoluteUri.ToString();
        
        return null;
    }
}
