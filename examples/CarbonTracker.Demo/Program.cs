using Optimizely.CarbonTracker;

var builder = WebApplication.CreateBuilder(args);

// Add Carbon Tracker services
builder.Services.AddCarbonTracker(options =>
{
    options.GreenHosting = false;
    options.GridIntensityGramsCO2PerKWh = 442; // Global average
});

// Add controllers and views
builder.Services.AddControllersWithViews();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapControllers();

// Sample home page
app.MapGet("/", () => Results.Content(@"
<!DOCTYPE html>
<html>
<head>
    <title>Carbon Tracker Demo</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif;
            max-width: 800px;
            margin: 50px auto;
            padding: 20px;
            line-height: 1.6;
        }
        h1 { color: #22c55e; }
        .card {
            background: #f9fafb;
            border: 1px solid #e5e7eb;
            border-radius: 8px;
            padding: 20px;
            margin-bottom: 20px;
        }
        .button {
            display: inline-block;
            padding: 12px 24px;
            background: #3b82f6;
            color: white;
            text-decoration: none;
            border-radius: 6px;
            font-weight: 500;
        }
        .button:hover {
            background: #2563eb;
        }
        pre {
            background: #1e293b;
            color: #e2e8f0;
            padding: 16px;
            border-radius: 6px;
            overflow-x: auto;
        }
    </style>
</head>
<body>
    <h1>🌱 Carbon Tracker Demo</h1>
    
    <div class=""card"">
        <h2>Welcome!</h2>
        <p>This demo shows how to integrate the Optimizely Carbon Tracker into your ASP.NET Core application.</p>
    </div>
    
    <div class=""card"">
        <h3>Try It Out</h3>
        <p>Analyze the carbon footprint of any web page:</p>
        <a href=""/api/carbon-tracker/analyze?pageUrl=https://example.com"" class=""button"">Analyze example.com</a>
    </div>
    
    <div class=""card"">
        <h3>API Endpoints</h3>
        <ul>
            <li><code>GET /api/carbon-tracker/analyze?pageUrl={url}</code> - Analyze a page</li>
            <li><code>GET /api/carbon-tracker/summary</code> - Get system summary</li>
            <li><code>GET /carbon-tracker/ui</code> - View the UI panel</li>
        </ul>
    </div>
    
    <div class=""card"">
        <h3>Setup Code</h3>
        <pre>// Program.cs
builder.Services.AddCarbonTracker(options =>
{
    options.GreenHosting = false;
    options.GridIntensityGramsCO2PerKWh = 442;
});</pre>
    </div>
    
    <div class=""card"">
        <h3>Example Analysis</h3>
        <p>Click the button above to see a live analysis, or try these URLs:</p>
        <ul>
            <li><a href=""/api/carbon-tracker/analyze?pageUrl=https://www.google.com"">Google.com</a></li>
            <li><a href=""/api/carbon-tracker/analyze?pageUrl=https://github.com"">GitHub.com</a></li>
        </ul>
    </div>
</body>
</html>
", "text/html"));

app.Run();

