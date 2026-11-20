using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Jellyfin.Plugin.HighlightedCollection.Managers;
using Jellyfin.Plugin.HighlightedCollection.Models;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.HighlightedCollection.Controllers;

/// <summary>
/// Initializes a new instance of the <see cref="CollectionDataController"/> class.
/// </summary>
/// <param name="users">Instance of the <see cref="IUserManager"/> interface.</param>
/// <param name="library">Instance of the <see cref="ILibraryManager"/> interface.</param>
/// <param name="manager">Dates tracker.</param>
[Authorize]
[ApiController]
[Route("HighlightedCollection/CollectionData")]
public class CollectionDataController(
    IUserManager users,
    ILibraryManager library,
    AddedToCollectionManager manager) : Controller
{
    /// <summary>
    /// GET.
    /// </summary>
    /// <returns>Config and dates (or null).</returns>
    public IActionResult Index()
    {
        var config = Plugin.Instance!.Configuration;
        if (!config.Collection.HasValue)
        {
            return Json(null);
        }

        var collectionId = config.Collection.Value;
        var dates = manager.GetCollectionData(collectionId);

        // security trimming
        var user = users.GetUserById(GetCurrentUserId(User)!.Value)!;
        var collection = (BoxSet)library.GetParentItem(collectionId, user.Id);
        var canAccess = collection.GetItems(new InternalItemsQuery(user))
            .Items
            .Select(item => item.Id);
        var remove = dates.Keys.Where(id => !canAccess.Contains(id));
        foreach (var id in remove)
        {
            dates.Remove(id);
        }

        return Json(
            new HighlightedCollectionData
            {
                Config = config,
                AddedDates = dates.ToDictionary(
                    kvp => kvp.Key.ToString("N"),
                    kvp => kvp.Value).AsReadOnly()
            });
    }

    /*
     * https://github.com/n00bcodr/Jellyfin-Enhanced/blob/main/Jellyfin.Plugin.JellyfinEnhanced/Helpers/UserHelper.cs
     */
    private static string? GetClaimValue(ClaimsPrincipal user, string name)
        => user.Claims.FirstOrDefault(claim => claim.Type.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static Guid? GetCurrentUserId(ClaimsPrincipal claimsPrincipal)
    {
        string currentUserString = GetClaimValue(claimsPrincipal, "Jellyfin-UserId") ?? string.Empty;
        if (Guid.TryParse(currentUserString, out Guid userId))
        {
            return userId;
        }

        return null;
    }
}
