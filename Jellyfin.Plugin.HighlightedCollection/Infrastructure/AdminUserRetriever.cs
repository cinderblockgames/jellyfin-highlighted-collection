using System.Linq;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Library;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Plugin.HighlightedCollection.Infrastructure;

/// <summary>
/// Stores an admin user for collection visibility.
/// </summary>
public class AdminUserRetriever
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AdminUserRetriever"/> class.
    /// </summary>
    /// <param name="database">Instance of the <see cref="IJellyfinDatabaseProvider"/> interface.</param>
    /// <param name="users">Instance of the <see cref="IUserManager"/> interface.</param>
    public AdminUserRetriever(IJellyfinDatabaseProvider database, IUserManager users)
    {
        var adminUserId = database.DbContextFactory!.CreateDbContext()
            .Users
            .Include(user => user.Permissions)
            .First(user => user.Permissions.Any(perm => perm.Kind == PermissionKind.IsAdministrator && perm.Value))
            .Id;
        AdminUser = users.GetUserById(adminUserId)!;
    }

    /// <summary>
    /// Gets an admin user.
    /// </summary>
    public User AdminUser { get; }
}
