using Jellyfin.Plugin.HighlightedCollection.Managers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.HighlightedCollection.Infrastructure;

/// <summary>
/// Registers types with dependency injection engine.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <summary>
    /// Registers types with dependency injection engine.
    /// </summary>
    /// <param name="serviceCollection">Instance of the <see cref="IServiceCollection"/> interface.</param>
    /// <param name="applicationHost">Instance of the <see cref="IServerApplicationHost"/> interface.</param>
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<AdminUserRetriever>();
        serviceCollection.AddSingleton<AddedToCollectionManager>();
    }
}
