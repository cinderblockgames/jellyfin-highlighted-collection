using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.HighlightedCollection;

/// <summary>
/// Initializes a new instance of the <see cref="ScriptManager"/> class.
/// </summary>
/// <param name="plugin">The plugin for which this script is being managed.</param>
/// <param name="logger">Logger for reporting success/failure.</param>
public class ScriptManager(IPlugin plugin, ILogger<Plugin> logger)
{
    /// <summary>
    /// Registers the specified script.
    /// </summary>
    /// <param name="name">The name under which to register the script.</param>
    /// <param name="path">The JavaScript file to register.</param>
    public void Register(string name, string path)
    {
        try
        {
            // Find the JavaScript Injector assembly
            Assembly? jsInjectorAssembly = AssemblyLoadContext.All
                .SelectMany(x => x.Assemblies)
                .FirstOrDefault(x => x.FullName?.Contains("Jellyfin.Plugin.JavaScriptInjector", StringComparison.Ordinal) ?? false);

            if (jsInjectorAssembly != null)
            {
                // Get the PluginInterface type
                Type? pluginInterfaceType = jsInjectorAssembly.GetType("Jellyfin.Plugin.JavaScriptInjector.PluginInterface");

                if (pluginInterfaceType != null)
                {
                    path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, path);
                    logger.LogInformation(@"Registering script located at {Path}.", path);

                    // Create the registration payload
                    var scriptRegistration = new JObject
                    {
                        { "id", $"{plugin.Id}-{name}" }, // Unique ID for your script
                        { "name", name },
                        { "script", File.ReadAllText(path) },
                        { "enabled", true },
                        { "requiresAuthentication", true }, // Set to true if script should only run for logged-in users
                        { "pluginId", plugin.Id.ToString() },
                        { "pluginName", plugin.Name },
                        { "pluginVersion", plugin.Version.ToString() }
                    };

                    // Register the script
                    var registerResult = pluginInterfaceType.GetMethod("RegisterScript")?.Invoke(null, [scriptRegistration]);

                    if (registerResult is bool and true)
                    {
                        logger.LogInformation("Successfully registered JavaScript with JavaScript Injector plugin.");
                    }
                    else
                    {
                        logger.LogWarning("Failed to register JavaScript with JavaScript Injector plugin. RegisterScript returned false.");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to register JavaScript with JavaScript Injector plugin.");
        }
    }
}
