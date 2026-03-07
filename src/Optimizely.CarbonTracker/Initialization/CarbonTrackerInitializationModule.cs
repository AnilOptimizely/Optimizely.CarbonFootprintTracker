using EPiServer;
using EPiServer.Core;
using EPiServer.Core.Internal;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using EPiServer.Shell;
using EPiServer.Shell.Web.Mvc.Html;
using Microsoft.AspNetCore.Hosting.Server;
using Optimizely.CarbonTracker.Models;
using System.Threading;

namespace Optimizely.CarbonTracker.Initialization; 
    /// <summary>
    /// Initialization module for the Reading Time add-on.
    /// </summary>
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    [ModuleDependency(typeof(ShellInitialization))]
public class CarbonTrackerInitializationModule : IConfigurableModule
{
    
    public void ConfigureContainer(ServiceConfigurationContext context)
    {
    }

    public void Initialize(InitializationEngine context)
    {
    }

    public void Uninitialize(InitializationEngine context)
    {
        // Cleanup logic
    }
}
