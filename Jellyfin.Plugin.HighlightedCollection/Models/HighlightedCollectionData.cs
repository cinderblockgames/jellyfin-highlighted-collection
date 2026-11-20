using System;
using System.Collections.Generic;
using Jellyfin.Plugin.HighlightedCollection.Configuration;

namespace Jellyfin.Plugin.HighlightedCollection.Models;

/// <summary>
/// Holds collection info.
/// </summary>
public class HighlightedCollectionData
{
    /// <summary>
    /// Gets or sets the collection configuration.
    /// </summary>
    public required PluginConfiguration Config { get; set; }

    /// <summary>
    /// Gets or sets the dates data.
    /// </summary>
    public required IReadOnlyDictionary<string, string?> AddedDates { get; set; }
}
