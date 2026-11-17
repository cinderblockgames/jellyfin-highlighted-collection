using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Plugin.HighlightedCollection.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
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
        manager.ItemsAddedToCollection += ItemsAddedToCollection;
        manager.ItemsRemovedFromCollection += ItemsRemovedFromCollection;
    }

    private string TagPrefix => "highlighted";

    /// <inheritdoc />
    public override string Name => "Highlighted Collection";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("14D6403B-1065-4A19-9363-B258740652C6");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    private static PluginConfiguration? Config => Instance?.Configuration;

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

    private void ItemsAddedToCollection(object? sender, CollectionModifiedEventArgs args)
    {
        if (args.Collection.Id == Config?.Collection)
        {
            foreach (var item in args.ItemsChanged)
            {
                AddTag(item);
            }
        }
    }

    private void ItemsRemovedFromCollection(object? sender, CollectionModifiedEventArgs args)
    {
        if (args.Collection.Id == Config?.Collection)
        {
            foreach (var item in args.ItemsChanged)
            {
                RemoveTag(item);
            }
        }
    }

    private void RemoveTag(BaseItem item)
    {
        item.Tags = item.Tags.Except(
            item.Tags.Where(
                t => t.StartsWith(TagPrefix, StringComparison.OrdinalIgnoreCase)))
                    .ToArray();
    }

    private void AddTag(BaseItem item)
    {
        RemoveTag(item);
        item.AddTag($"{TagPrefix}:{DateTime.Today:yyyy-MM-dd}");
    }
}
