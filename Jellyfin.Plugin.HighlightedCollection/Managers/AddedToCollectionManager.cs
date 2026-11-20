using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using J2N.Text;
using Jellyfin.Plugin.HighlightedCollection.Extensions;
using Jellyfin.Plugin.HighlightedCollection.Infrastructure;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.HighlightedCollection.Managers;

/// <summary>
/// Tracks when items are added to a collection.
/// </summary>
public class AddedToCollectionManager
{
    private readonly string _directory;
    private readonly AdminUserRetriever _accessor;
    private readonly ILogger<Plugin> _logger;
    private static readonly ReaderWriterLockSlim _rwls = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AddedToCollectionManager"/> class.
    /// </summary>
    /// <param name="paths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="collections">Instance of the <see cref="ICollectionManager"/> interface.</param>
    /// <param name="accessor">Admin user for viewing collection contents.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public AddedToCollectionManager(IApplicationPaths paths, ICollectionManager collections, AdminUserRetriever accessor, ILogger<Plugin> logger)
    {
        paths = paths ?? throw new ArgumentNullException(nameof(paths));
        _directory = Path.Combine(paths.PluginsPath, "configurations", "Jellyfin.Plugin.HighlightedCollection");
        if (!Directory.Exists(_directory))
        {
            Directory.CreateDirectory(_directory);
        }

        collections = collections ?? throw new ArgumentNullException(nameof(collections));
        collections.ItemsAddedToCollection += ItemsAddedToCollection;
        collections.ItemsRemovedFromCollection += ItemsRemovedFromCollection;

        _accessor = accessor;

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Reads tracked data for the specified collection.
    /// </summary>
    /// <param name="collectionId">Collection for which to get tracked data.</param>
    /// <returns>Tracked data for the specified collection.</returns>
    /// <remarks>
    /// Does not check against current collection information, so do not assume that all items exist or that this is all items.
    /// Use just as a lookup/reference.
    /// </remarks>
    public Dictionary<Guid, string?> GetCollectionData(Guid collectionId)
    {
        try
        {
            return _rwls.SafeRead(() => ReadFile(collectionId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered while reading collection data for collection.Id {CollectionId}.  Returning empty value.", collectionId);
            return new Dictionary<Guid, string?>();
        }
    }

    private void ItemsAddedToCollection(object? sender, CollectionModifiedEventArgs args)
    {
        try
        {
            ModifyCollectionData(args.Collection, data =>
            {
                foreach (var item in args.ItemsChanged)
                {
                    data[item.Id] = DateTime.Today.ToString("yyyy-MM-dd", StringFormatter.InvariantCulture);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered while adding items to collection data. Skipping.");
        }
    }

    private void ItemsRemovedFromCollection(object? sender, CollectionModifiedEventArgs args)
    {
        try
        {
            // No need to remove the items from the current list, since we're getting a new list, and they'll already be gone.
            ModifyCollectionData(args.Collection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception encountered while removing items from collection data. Skipping.");
        }
    }

    private void ModifyCollectionData(BoxSet collection, Action<Dictionary<Guid, string?>>? modify = null)
    {
        _rwls.SafeWrite(() =>
        {
            var data = ReadFile(collection.Id);
            modify?.Invoke(data);

            // Save only the items that currently exist in the collection; prevent orphaned state.
            var newData = collection.GetItems(new InternalItemsQuery(_accessor.AdminUser))
                .Items
                .ToDictionary(
                    item => item.Id,
                    item => data.GetValueOrDefault(item.Id));
            SaveFile(collection.Id, newData);
        });
    }

    private Dictionary<Guid, string?> ReadFile(Guid collectionId)
    {
        var file = FilePath(collectionId);
        if (File.Exists(file))
        {
            var serialized = File.ReadAllText(FilePath(collectionId));
            if (serialized.Length > 0)
            {
                return JsonConvert.DeserializeObject<Dictionary<Guid, string?>>(serialized) ?? new();
            }
        }

        return new Dictionary<Guid, string?>();
    }

    private void SaveFile(Guid collectionId, Dictionary<Guid, string?> data)
    {
        var serialized = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(FilePath(collectionId), serialized);
    }

    private string FilePath(Guid collectionId) => Path.Combine(_directory, $"{collectionId.ToString("D").ToUpperInvariant()}.json");
}
