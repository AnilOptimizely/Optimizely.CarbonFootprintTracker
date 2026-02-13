# Carbon Tracker Demo Application

This is a simple ASP.NET Core web application demonstrating the Optimizely Carbon Tracker add-on.

## Running the Demo

```bash
cd examples/CarbonTracker.Demo
dotnet run
```

Then open your browser to `https://localhost:5001` (or the URL shown in the console).

## What's Included

- **Home Page**: Overview and quick links
- **API Endpoints**: Full REST API for carbon analysis
- **UI Panel**: View the Carbon Tracker UI at `/carbon-tracker/ui`

## Try It Out

### Analyze a Page

Visit any of these endpoints:
- `/api/carbon-tracker/analyze?pageUrl=https://example.com`
- `/api/carbon-tracker/analyze?pageUrl=https://www.google.com`

### View the Summary

- `/api/carbon-tracker/summary`

### See the UI

- `/carbon-tracker/ui?pageUrl=https://example.com`

## Code Example

```csharp
// In Program.cs
builder.Services.AddCarbonTracker(options =>
{
    options.GreenHosting = false; // Set to true if using green hosting
    options.GridIntensityGramsCO2PerKWh = 442; // Global average
});
```

That's all you need! The Carbon Tracker automatically:
- Registers API controllers
- Sets up the UI view
- Configures all necessary services

## Understanding the Results

When you analyze a page, you'll get:

```json
{
  "score": "B",
  "estimatedCO2Grams": 0.45,
  "totalTransferSizeBytes": 1048576,
  "assets": [...],
  "suggestions": [...]
}
```

### Green Score Scale
- **A**: ≤0.20g CO₂ (Excellent!)
- **B**: 0.21–0.50g (Good)
- **C**: 0.51–1.00g (Average)
- **D**: 1.01–2.00g (Needs work)
- **F**: >2.00g (Critical)

## Customization

### Regional Grid Intensity

Adjust for your region:

```csharp
options.GridIntensityGramsCO2PerKWh = 60; // France (mostly nuclear)
options.GridIntensityGramsCO2PerKWh = 350; // Germany
options.GridIntensityGramsCO2PerKWh = 700; // Poland (high coal)
```

### Green Hosting

If your hosting provider uses renewable energy:

```csharp
options.GreenHosting = true; // Reduces data center emissions to zero
```

## Learn More

See the main [package README](../../src/Optimizely.CarbonTracker/README.md) for complete documentation.
