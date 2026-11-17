using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.HighlightedCollection.Configuration;

/// <summary>
/// The order in which to display items from the highlighted collection.
/// </summary>
public enum SortOrder
{
    /// <summary>
    /// Default collection sorting.
    /// </summary>
    JellyfinDefault,

    /// <summary>
    /// Date added to collection.
    /// </summary>
    DateAddedToCollection,

    /// <summary>
    /// Alphabetical order (using SortName).
    /// </summary>
    Name,
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // set default options here
        SortOrder = SortOrder.DateAddedToCollection;
    }

    /// <summary>
    /// Gets or sets the highlighted collection.
    /// </summary>
    public Guid? Collection { get; set; }

    /// <summary>
    /// Gets or sets the sort order.
    /// </summary>
    public SortOrder SortOrder { get; set; }
}
