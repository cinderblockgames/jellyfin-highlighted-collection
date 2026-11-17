using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.HighlightedCollection.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

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
    /// <param name="manager">Instance of the <see cref="ICollectionManager"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, ICollectionManager manager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        var tagManager = new TagManager(() => Instance?.Configuration?.Collection, "highlighted");
        manager.ItemsAddedToCollection += tagManager.ItemsAddedToCollection;
        manager.ItemsRemovedFromCollection += tagManager.ItemsRemovedFromCollection;
    }

    /// <inheritdoc />
    public override string Name => "Highlighted Collection";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("14D6403B-1065-4A19-9363-B258740652C6");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
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
