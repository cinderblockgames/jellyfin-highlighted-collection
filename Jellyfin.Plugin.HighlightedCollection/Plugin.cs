using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.HighlightedCollection.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HighlightedCollection;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="addedToCollectionManager">Date tracker.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        AddedToCollectionManager addedToCollectionManager,
        ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        Logger = logger;
        AddedToCollectionManager = addedToCollectionManager;
        ScriptManager = new ScriptManager(Instance, logger);
        Instance.ConfigurationChanged += (_, _) => ScriptManager.Register("highlighted-collection-plugin", "Scripts/highlighted-collection-plugin.js");
    }

    /// <inheritdoc />
    public override string Name => "Highlighted Collection";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("14D6403B-1065-4A19-9363-B258740652C6");

    /// <summary>
    /// Gets or sets the current plugin instance.
    /// </summary>
    private Plugin Instance { get; set; }

    private AddedToCollectionManager AddedToCollectionManager { get; }

    private ScriptManager ScriptManager { get; }

    private ILogger<Plugin> Logger { get; }

    private static object Deadbolt { get; } = new object();

    private static bool Initialized { get; set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        // This is a misuse of this method, but -- it runs after all plugins are loaded, so...
        if (!Initialized)
        {
            lock (Deadbolt)
            {
                if (!Initialized)
                {
                    if (Instance.Configuration.Collection.HasValue)
                    {
                        ScriptManager.Register("highlighted-collection-plugin", "Scripts/highlighted-collection-plugin.js");
                    }

                    Initialized = true;
                }
            }
        }

        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }

    /// <summary>
    /// Clean up scripts.
    /// </summary>
    public override void OnUninstalling()
    {
        base.OnUninstalling();
        ScriptManager.Unregister();
    }
}
