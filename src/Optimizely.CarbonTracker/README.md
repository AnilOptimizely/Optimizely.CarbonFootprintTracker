# Optimizely Carbon Tracker

A native Optimizely CMS 12+ add-on that provides real-time carbon footprint analysis for content pages. Track and optimize the environmental impact of your web content with actionable insights and Green Score ratings.

## Features

- **Real-time Analysis**: Analyze pages on-demand to measure their carbon footprint
- **Green Score Rating**: A-F rating system based on CO₂ emissions per page view
- **Asset Breakdown**: Detailed breakdown by asset type (HTML, CSS, JavaScript, Images, Fonts, Video)
- **Optimization Suggestions**: Actionable recommendations to reduce carbon emissions
- **SWD v4 Compliant**: Based on the Sustainable Web Design v4 model
- **Web Standards**: Follows Green Web Foundation's CO2.js methodology

## Green Score Thresholds

- **A**: ≤0.20g CO₂ per page view (Excellent!)
- **B**: 0.21–0.50g CO₂ per page view (Good)
- **C**: 0.51–1.00g CO₂ per page view (Average)
- **D**: 1.01–2.00g CO₂ per page view (Needs improvement)
- **F**: >2.00g CO₂ per page view (Critical)

## Installation

### NuGet Package

```bash
dotnet add package Optimizely.CarbonTracker
```

### Configuration

Add Carbon Tracker to your `Startup.cs` or `Program.cs`:

```csharp
// In ConfigureServices or builder.Services
services.AddCarbonTracker(options =>
{
    options.GreenHosting = true; // Set to true if using green hosting
    options.GridIntensityGramsCO2PerKWh = 300; // e.g., Sweden
    options.EnableRealTimeAnalysis = true;
    options.HistoryRetentionDays = 365;
});
```

Then register the middleware in the application pipeline:

```csharp
// In Configure or app pipeline
app.UseCarbonTracker(); // Registers static file serving for embedded assets
```

That's it! The add-on will automatically register with Optimizely CMS. The IFrame component
auto-registers via the module initializer so editors see the Carbon Footprint panel
immediately after installing the NuGet package.

## Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `GreenHosting` | bool | `false` | Whether your hosting uses renewable energy (reduces data center emissions) |
| `GridIntensityGramsCO2PerKWh` | double | `442` | Regional grid carbon intensity (g CO₂/kWh). Examples: France=~60, USA=~400, Poland=~700 |
| `EnableRealTimeAnalysis` | bool | `true` | Enable automatic analysis on content publish |
| `HistoryRetentionDays` | int | `365` | Number of days to retain historical reports |
| `ResourceTimeoutSeconds` | int | `30` | Timeout for analyzing a single resource |
| `PageAnalysisTimeoutSeconds` | int | `120` | Total timeout for analyzing a complete page |

## API Endpoints

### Analyze a Page

```http
GET /api/carbon-tracker/analyze?pageUrl={url}&contentGuid={guid}
```

Returns a complete carbon footprint report with:
- Green Score (A-F)
- Total CO₂ emissions in grams
- Asset breakdown by category
- Optimization suggestions with potential savings

**Example Response:**

```json
{
  "score": "B",
  "estimatedCO2Grams": 0.45,
  "totalTransferSizeBytes": 1048576,
  "assets": [
    {
      "category": "Images",
      "transferSizeBytes": 524288,
      "percentage": 50.0,
      "estimatedCO2Grams": 0.225,
      "resourceCount": 5
    }
  ],
  "suggestions": [
    {
      "severity": "High",
      "title": "Convert images to modern formats (WebP/AVIF)",
      "description": "5 images are using legacy formats...",
      "potentialSavingsBytes": 209715,
      "potentialCO2SavingsGrams": 0.09
    }
  ]
}
```

### Get Summary

```http
GET /api/carbon-tracker/summary
```

Returns basic system information and status.

## How It Works

### 1. Sustainable Web Design v4 Model

The add-on uses the industry-standard SWD v4 model to calculate CO₂ emissions:

```
CO₂ (g) = (Data Transfer in GB) × (Energy per GB in kWh) × (Carbon Intensity in g CO₂/kWh)
```

**System Segments:**
- Data Centers: 15% of energy usage
- Networks: 14% of energy usage
- User Devices: 52% of energy usage
- Production: 19% of energy usage

**Constants:**
- Energy per GB: 0.81 kWh/GB
- Global average grid intensity: 442 g CO₂/kWh
- Green hosting: Data center segment uses 0 g CO₂/kWh
- Returning visits: 2% of data re-transferred (98% cache hit rate)

### 2. Page Analysis

The analyzer:
1. Fetches the published page HTML
2. Parses and extracts all referenced resources
3. Measures transfer size for each resource
4. Classifies resources by type (Images, CSS, JavaScript, Fonts, Video)
5. Runs specialized analyzers to detect optimization opportunities

### 3. Optimization Detection

**Image Analyzer** detects:
- Images without lazy loading
- Images without responsive `srcset`
- Legacy formats (JPEG/PNG instead of WebP/AVIF)
- Oversized images (>200KB)

**Script Analyzer** detects:
- Render-blocking scripts (missing async/defer)
- Large bundles (>100KB)
- Heavy third-party scripts

**Video Analyzer** detects:
- Autoplay videos
- Videos with `preload="auto"`
- Excessive video file sizes

## Regional Grid Intensity Values

Customize `GridIntensityGramsCO2PerKWh` based on your region:

| Region | g CO₂/kWh | Notes |
|--------|-----------|-------|
| France | 60 | High nuclear/renewable energy |
| Norway | 20 | Mostly hydroelectric |
| UK | 250 | Mixed energy sources |
| Germany | 350 | Transitioning to renewables |
| USA (average) | 400 | Varies by state |
| China | 550 | High coal dependency |
| Poland | 700 | High coal dependency |
| Australia | 650 | High coal dependency |

Source: [Ember Climate](https://ember-climate.org/data/data-explorer/)

## Architecture

```
┌─────────────────────────────────────┐
│     Optimizely CMS UI (IFrame)      │  ← Visual panel in edit mode
├─────────────────────────────────────┤
│    API Controllers (REST)           │  ← Endpoints for analysis
├─────────────────────────────────────┤
│    Carbon Report Service            │  ← Orchestration layer
├─────────────────────────────────────┤
│  ┌──────────────┐  ┌──────────────┐ │
│  │ Page Analyzer│  │  Calculators │ │  ← Core engines
│  ├──────────────┤  └──────────────┘ │
│  │ • Image      │                    │
│  │ • Script     │                    │
│  │ • Video      │                    │
│  └──────────────┘                    │
├─────────────────────────────────────┤
│         Data Models & DTOs          │
└─────────────────────────────────────┘
```

## Standards & References

- [Sustainable Web Design v4](https://sustainablewebdesign.org/estimating-digital-emissions/)
- [CO2.js by Green Web Foundation](https://github.com/thegreenwebfoundation/co2.js)
- [Website Carbon Calculator](https://www.websitecarbon.com/)
- [HTTP Archive](https://httparchive.org/) - For percentile comparisons

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

## License

MIT License - see LICENSE file for details.

## Support

For issues and questions, please visit the [GitHub repository](https://github.com/AnilOptimizely/Optimizely.CarbonFootprintTracker).

---

**Making the web greener, one page at a time.** 🌱
