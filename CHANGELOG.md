# Changelog

All notable changes to the Optimizely Carbon Tracker will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-02-13

### Added

#### Core Functionality
- **Carbon Calculation Engine**: Full implementation of Sustainable Web Design v4 model
  - Accurate CO₂ calculations based on data transfer
  - Support for green hosting (zero emissions for data centers)
  - Configurable regional grid intensity
  - Returning visitor cache ratio (2% re-transfer)
  
- **Page Analysis Engine**: Comprehensive page resource discovery
  - HTML parsing with HtmlAgilityPack
  - Resource extraction for all asset types (HTML, CSS, JavaScript, Images, Fonts, Video)
  - Transfer size measurement via HEAD/GET requests
  - Timeout handling for slow resources
  
- **Green Score System**: Industry-standard A-F rating
  - A: ≤0.20g CO₂ (Excellent)
  - B: 0.21–0.50g (Good)
  - C: 0.51–1.00g (Average)
  - D: 1.01–2.00g (Needs improvement)
  - F: >2.00g (Critical)

#### Specialized Analyzers
- **Image Analyzer**: Detects optimization opportunities
  - Missing lazy loading
  - Missing responsive srcset
  - Legacy formats (JPEG/PNG vs WebP/AVIF)
  - Oversized images (>200KB)
  
- **Script Analyzer**: JavaScript optimization suggestions
  - Render-blocking scripts (missing async/defer)
  - Large bundles (>100KB)
  - Heavy third-party scripts
  
- **Video Analyzer**: Video optimization recommendations
  - Autoplay videos detection
  - Preload settings
  - File size optimization

#### API & UI
- **REST API**: Full-featured endpoints
  - `/api/carbon-tracker/analyze` - Analyze any page URL
  - `/api/carbon-tracker/summary` - System status
  
- **Web UI**: Beautiful responsive interface
  - Green Score badge with SVG visualization
  - Asset breakdown bar chart and legend
  - Collapsible optimization suggestions
  - CSV export functionality
  - Re-analyze capability

#### Developer Experience
- **Service Registration**: Simple DI setup
  ```csharp
  services.AddCarbonTracker(options => {
      options.GreenHosting = true;
      options.GridIntensityGramsCO2PerKWh = 300;
  });
  ```
  
- **Configuration Options**:
  - GreenHosting (bool, default: false)
  - GridIntensityGramsCO2PerKWh (double, default: 442)
  - EnableRealTimeAnalysis (bool, default: true)
  - HistoryRetentionDays (int, default: 365)
  - ResourceTimeoutSeconds (int, default: 30)
  - PageAnalysisTimeoutSeconds (int, default: 120)

#### Testing
- **22 Unit Tests**: Comprehensive coverage
  - CarbonCalculator: 16 tests
  - ImageAnalyzer: 6 tests
  - All tests passing with xUnit and Moq

#### Documentation
- **Package README**: Complete usage guide
  - Installation instructions
  - Configuration examples
  - API documentation
  - Green Score thresholds
  - Regional grid intensity table
  - Architecture overview
  
- **Demo Application**: Working example
  - ASP.NET Core integration
  - Sample pages and API calls
  - Configuration examples

#### Standards & Compliance
- Based on Sustainable Web Design v4 model
- Follows Green Web Foundation CO2.js methodology
- HTTP Archive data for comparisons

### Technical Details

#### Architecture
- .NET 8.0 target framework
- ASP.NET Core Web SDK
- Dependency injection throughout
- Clean separation of concerns

#### Dependencies
- Microsoft.AspNetCore.Mvc.Core: 2.2.5
- Microsoft.AspNetCore.Mvc.ViewFeatures: 2.2.0
- Microsoft.EntityFrameworkCore: 8.0.0
- HtmlAgilityPack: 1.11.46

#### Package Configuration
- Package ID: Optimizely.CarbonTracker
- Version: 1.0.0
- License: MIT
- Target: Optimizely CMS 12+ (.NET 8+)
- Embedded resources: Views, wwwroot assets

### Repository Structure
```
src/
  Optimizely.CarbonTracker/     # Main library
    Analysis/                    # Page analysis engine
    Calculation/                 # CO₂ calculations
    Models/                      # Data models
    Controllers/                 # API & UI controllers
    Views/                       # Razor views
    Configuration/               # Options classes
    
tests/
  Optimizely.CarbonTracker.Tests/  # Unit tests
  
examples/
  CarbonTracker.Demo/          # Demo application
```

### Future Enhancements (Not in v1.0)

Planned for future releases:
- Persistence layer for historical tracking
- Optimizely CMS IFrameComponent integration
- Content event hooks (publish/save)
- Scheduled jobs for site-wide analysis
- Historical trends and reporting
- Dashboard for aggregate statistics

These features require Optimizely CMS packages which are currently unavailable in the build environment.

---

## Contributing

We welcome contributions! Please feel free to submit issues and pull requests.

## License

MIT License - see LICENSE file for details.
