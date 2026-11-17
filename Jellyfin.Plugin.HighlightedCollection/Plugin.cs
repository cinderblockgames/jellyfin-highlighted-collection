using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Jellyfin.Plugin.HighlightedCollection.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Collections;
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
    /// <param name="collections">Instance of the <see cref="ICollectionManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ICollectionManager collections, ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        var tagManager = new TagManager(() => Instance?.Configuration?.Collection, "highlighted");
        collections.ItemsAddedToCollection += tagManager.ItemsAddedToCollection;
        collections.ItemsRemovedFromCollection += tagManager.ItemsRemovedFromCollection;
        ScriptManager = new ScriptManager(Instance, logger);
    }

    /// <inheritdoc />
    public override string Name => "Highlighted Collection";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("14D6403B-1065-4A19-9363-B258740652C6");

    /// <summary>
    /// Gets or sets the current plugin instance.
    /// </summary>
    private static Plugin? Instance { get; set; }

    private ScriptManager? ScriptManager { get; set;  }

    private static object Deadbolt { get; } = new object();

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        // This is a misuse of this method, but -- it runs after all plugins are loaded, so...
        if (ScriptManager != null)
        {
            lock (Deadbolt)
            {
                if (ScriptManager != null)
                {
                    ScriptManager.Register("highlighted-collection-plugin", "Scripts/highlighted-collection-plugin.js");
                    ScriptManager = null;
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
}
