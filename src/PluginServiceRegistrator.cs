using Jellyfin.Plugin.FavoriteTags.Controllers;
using Jellyfin.Plugin.FavoriteTags.ScheduledTasks;
using Jellyfin.Plugin.FavoriteTags.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.FavoriteTags;

/// <summary>
/// Register plugin services.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register the sync service
        serviceCollection.AddScoped<SyncService>();

        // Register the controller - this makes it discoverable by Jellyfin
        serviceCollection.AddScoped<SyncController>();

        // Register the scheduled task
        serviceCollection.AddScoped<IScheduledTask, SyncScheduledTask>();
    }
}
