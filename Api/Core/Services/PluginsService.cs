using System;
using System.IO;
using System.Reflection;
using Api.Core.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Core.Services;

/// <inheritdoc cref="IPluginsService" />
public class PluginsService : IPluginsService, ITransientService
{
    private readonly IWebHostEnvironment webHostEnvironment;
    private readonly ILogger logger;

    /// <summary>
    /// Creates a new instance of PluginsService.
    /// </summary>
    public PluginsService(IWebHostEnvironment webHostEnvironment, ILogger<PluginsService> logger)
    {
        this.webHostEnvironment = webHostEnvironment;
        this.logger = logger;
    }

    /// <inheritdoc />
    public void LoadPlugins(string pluginsDirectory)
    {
        if (String.IsNullOrWhiteSpace(pluginsDirectory))
        {
            // Empty plugins directory, so no plugins to load.
            return;
        }

        if (!Path.IsPathRooted(pluginsDirectory))
        {
            pluginsDirectory = Path.Combine(webHostEnvironment.ContentRootPath, pluginsDirectory);
        }

        if (!Directory.Exists(pluginsDirectory))
        {
            // Plugins directory doesn't exist, so no plugins to load.
            return;
        }

        var dllFiles = Directory.GetFiles(pluginsDirectory, "*.dll");

        foreach (var dllFile in dllFiles)
        {
            try
            {
                Assembly.LoadFrom(dllFile);
            }
            catch (Exception exception)
            {
                // Handle assembly loading exceptions
                logger.LogError(exception, $"Failed to load plugin {dllFile}");
            }
        }
    }
}