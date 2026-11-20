using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Plugin.HighlightedCollection.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Collections;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.EntityFrameworkCore;
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
    /// <param name="database">Instance of the <see cref="IJellyfinDatabaseProvider"/> interface.</param>
    /// <param name="users">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public Plugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ICollectionManager collections,
        IJellyfinDatabaseProvider database,
        IUserManager users,
        ILogger<Plugin> logger)
        : base(applicationPaths, xmlSerializer)
    {
        var adminUserId = database.DbContextFactory!.CreateDbContext()
            .Users
            .Include(user => user.Permissions)
            .First(user => user.Permissions.Any(perm => perm.Kind == PermissionKind.IsAdministrator && perm.Value))
            .Id;
        var adminUser = users.GetUserById(adminUserId)!;

        Instance = this;
        Logger = logger;
        AddedToCollectionManager = new AddedToCollectionManager(applicationPaths, collections, adminUser, logger);
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
    /// <remarks>Does this trigger when disabling a plugin as well?  Needs testing.</remarks>
    public override void OnUninstalling()
    {
        base.OnUninstalling();
        try
        {
            ScriptManager.Unregister();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error encountered while unregistering scripts; skipping.");
        }
    }
}
