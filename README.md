# Optimizely Carbon Footprint Tracker

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)]() [![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue)]() [![License](https://img.shields.io/badge/license-MIT-green)]()

A complete native Optimizely CMS 12+ add-on that provides real-time carbon footprint analysis for content pages. Help content editors understand and optimize the environmental impact of their web content with actionable insights and Green Score ratings.

![Carbon Tracker Screenshot](https://via.placeholder.com/800x500?text=Carbon+Tracker+UI+Preview)

## ✨ Features

- **🎯 Real-time Carbon Analysis**: Analyze any published page to measure its carbon footprint
- **📊 Green Score Rating**: Industry-standard A-F rating system based on CO₂ emissions per page view
- **📦 Asset Breakdown**: Detailed breakdown by asset type (HTML, CSS, JavaScript, Images, Fonts, Video)
- **💡 Optimization Suggestions**: Actionable recommendations ranked by potential CO₂ savings
- **🌱 SWD v4 Compliant**: Based on the Sustainable Web Design v4 model
- **📈 Visual Dashboard**: Beautiful, responsive UI integrated directly into Optimizely CMS
- **⚡ Zero Configuration**: Works out of the box with sensible defaults

## 🚀 Quick Start

### Installation

```bash
dotnet add package Optimizely.CarbonTracker
```

### Configuration

Add a connection string to your `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "CarbonTrackerDb": "Server=(localdb)\\mssqllocaldb;Database=CarbonTracker;Trusted_Connection=True;"
  }
}
```

Then add to your `Program.cs` or `Startup.cs`:

```csharp
// Minimal configuration
services.AddCarbonTracker();

// Or with custom options and database configuration
services.AddCarbonTracker(
    configure: options =>
    {
        options.GreenHosting = true; // If using green/renewable energy hosting
        options.GridIntensityGramsCO2PerKWh = 300; // Your region's grid intensity
    },
    configureDb: dbOptions =>
    {
        dbOptions.UseSqlServer(
            builder.Configuration.GetConnectionString("CarbonTrackerDb"));
    });
```

That's it! The Carbon Tracker will automatically integrate with your Optimizely CMS.

## 📖 Documentation

Comprehensive documentation is available in the [package README](src/Optimizely.CarbonTracker/README.md), including:

- Detailed configuration options
- API endpoint documentation
- Green Score thresholds
- How the calculations work
- Regional grid intensity values
- Architecture overview

## 🎨 Green Score System

| Score | CO₂ Range | Description |
|-------|-----------|-------------|
| **A** | ≤ 0.20g | Excellent! Cleaner than 95% of web pages |
| **B** | 0.21–0.50g | Good! Better than average |
| **C** | 0.51–1.00g | Average carbon footprint |
| **D** | 1.01–2.00g | Needs improvement |
| **F** | > 2.00g | Critical - urgent optimization needed |

## 🧪 Testing

The project includes comprehensive unit tests:

```bash
dotnet test
```

Current test coverage:
- ✅ 22 unit tests passing
- ✅ Carbon calculation engine (SWD v4 model)
- ✅ GreenScore mapping logic
- ✅ Image analyzer optimization detection
- ✅ Script analyzer optimization detection
- ✅ Video analyzer optimization detection

## 🏗️ Project Structure

```
src/
  Optimizely.CarbonTracker/
    Analysis/          # Page analysis engine & specialized analyzers
    Calculation/       # CO₂ calculation logic (SWD v4 model)
    Models/            # Data models & DTOs
    Controllers/       # API endpoints & view controllers
    Configuration/     # Options & settings
    Views/             # Razor views for UI
    
tests/
  Optimizely.CarbonTracker.Tests/
    Unit tests with xUnit & Moq
```

## 📊 How It Works

### 1. Analysis
The PageAnalyzer fetches your published page, parses the HTML, and discovers all referenced resources (images, scripts, stylesheets, fonts, videos).

### 2. Measurement
Each resource's transfer size is measured via HTTP HEAD requests (or GET if needed), classifying them by asset type.

### 3. Calculation
Using the **Sustainable Web Design v4 model**:
```
CO₂ (g) = (Data Transfer in GB) × (Energy per GB) × (Carbon Intensity)
```

System segments:
- Data Centers: 15% (can be 0% with green hosting)
- Networks: 14%
- User Devices: 52%
- Production: 19%

### 4. Optimization
Specialized analyzers detect common issues:
- Images without lazy loading or modern formats
- Render-blocking scripts
- Autoplay videos
- Large unoptimized assets

Each suggestion includes estimated CO₂ savings.

## 🌍 Standards & Compliance

This add-on implements:
- [Sustainable Web Design v4](https://sustainablewebdesign.org/estimating-digital-emissions/) methodology
- [Green Web Foundation CO2.js](https://github.com/thegreenwebfoundation/co2.js) principles
- [HTTP Archive](https://httparchive.org/) data for percentile comparisons

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues, feature requests, or pull requests.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Sustainable Web Design community
- Green Web Foundation
- Optimizely development community

---

**Making the web greener, one page at a time.** 🌱

For detailed usage instructions and API documentation, see the [package README](src/Optimizely.CarbonTracker/README.md).