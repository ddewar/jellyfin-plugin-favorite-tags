using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.FavoriteTags.Services;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FavoriteTags.ScheduledTasks;

/// <summary>
/// Scheduled task for syncing Jellyfin favorites to Sonarr/Radarr.
/// </summary>
public class SyncScheduledTask : IScheduledTask
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SyncScheduledTask> _logger;
    private SyncService? _syncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncScheduledTask"/> class.
    /// </summary>
    /// <param name="userManager">User manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="logger">Logger.</param>
    public SyncScheduledTask(
        IUserManager userManager,
        ILibraryManager libraryManager,
        ILoggerFactory loggerFactory,
        ILogger<SyncScheduledTask> logger)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _logger.LogInformation("SyncScheduledTask instantiated - scheduled task is loaded");
    }

    /// <inheritdoc />
    public string Name => "Sync Favorite Tags";

    /// <inheritdoc />
    public string Description => "Sync Jellyfin user favorites to Sonarr/Radarr tags";

    /// <inheritdoc />
    public string Category => "Jellyfin";

    /// <inheritdoc />
    public string Key => "FavoriteTagsSync";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        // Return a default hourly trigger that users can modify in the dashboard
        return new[]
        {
            new TaskTriggerInfo
            {
                Type = TaskTriggerInfoType.IntervalTrigger,
                IntervalTicks = TimeSpan.FromHours(1).Ticks  // Default: every hour
            }
        };
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting Favorite Tags sync task");
            progress?.Report(0);

            var plugin = FavoriteTagsPlugin.Instance;
            if (plugin == null)
            {
                _logger.LogError("Plugin instance not found");
                progress?.Report(100);
                return;
            }

            if (!plugin.Configuration.Enabled)
            {
                _logger.LogInformation("Plugin is disabled, skipping sync");
                progress?.Report(100);
                return;
            }

            // Check if auto-scheduling is enabled (SyncIntervalHours > 0)
            if (plugin.Configuration.SyncIntervalHours <= 0)
            {
                _logger.LogInformation("Auto-scheduling disabled (SyncIntervalHours = 0), skipping automatic sync");
                progress?.Report(100);
                return;
            }

            // Check if enough time has passed since last sync
            if (plugin.Configuration.LastSyncTime.HasValue)
            {
                var timeSinceLastSync = DateTime.UtcNow - plugin.Configuration.LastSyncTime.Value;
                var intervalTimespan = TimeSpan.FromHours(plugin.Configuration.SyncIntervalHours);

                if (timeSinceLastSync < intervalTimespan)
                {
                    _logger.LogInformation(
                        "Not enough time has passed since last sync. Last sync: {LastSync}, Next sync: {NextSync}",
                        plugin.Configuration.LastSyncTime,
                        plugin.Configuration.LastSyncTime.Value.Add(intervalTimespan));
                    progress?.Report(100);
                    return;
                }
            }

            _syncService ??= new SyncService(_userManager, _libraryManager, _loggerFactory);

            if (_syncService.IsSyncing)
            {
                _logger.LogWarning("Sync already in progress, skipping");
                progress?.Report(100);
                return;
            }

            progress?.Report(25);
            var result = await _syncService.SyncFavoritesAsync(plugin.Configuration, dryRun: false);
            progress?.Report(100);

            if (result.Success)
            {
                _logger.LogInformation(
                    "Sync completed successfully. Processed: {ItemsProcessed}, Tagged: {TagsApplied}, Errors: {ErrorCount}",
                    result.ItemsProcessed,
                    result.TagsApplied,
                    result.Errors.Count);
            }
            else
            {
                _logger.LogError("Sync failed: {Message}", result.Message);
                if (result.Errors.Count > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        _logger.LogError("Error: {Error}", error);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled sync task");
            progress?.Report(100);
        }
    }

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;
}
