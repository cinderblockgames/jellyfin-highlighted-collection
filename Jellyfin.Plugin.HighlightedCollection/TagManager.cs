using System;
using System.Linq;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.HighlightedCollection;

/// <summary>
/// Initializes a new instance of the <see cref="TagManager"/> class.
/// </summary>
/// <param name="getCollectionId">A function that returns the collection ID.</param>
/// <param name="tagPrefix">The tag prefix to manage.</param>
public class TagManager(Func<Guid?> getCollectionId, string tagPrefix)
{
    /// <summary>
    /// Removes existing tag(s) and adds tag with current date.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="args">Event args.</param>
    public void ItemsAddedToCollection(object? sender, CollectionModifiedEventArgs args)
    {
        if (args.Collection.Id == getCollectionId())
        {
            foreach (var item in args.ItemsChanged)
            {
                AddTag(item);
            }
        }
    }

    /// <summary>
    /// Removes existing tag(s).
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="args">Event args.</param>
    public void ItemsRemovedFromCollection(object? sender, CollectionModifiedEventArgs args)
    {
        if (args.Collection.Id == getCollectionId())
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
                    tag => tag.StartsWith(tagPrefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    private void AddTag(BaseItem item)
    {
        RemoveTag(item);
        item.AddTag($"{tagPrefix}:{DateTime.Today:yyyy-MM-dd}");
    }
}
