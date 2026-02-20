using System;
using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.FavoriteTags.Configuration;

/// <summary>
/// Plugin configuration settings.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        SonarrUrl = string.Empty;
        SonarrApiKey = string.Empty;
        RadarrUrl = string.Empty;
        RadarrApiKey = string.Empty;
        TagName = "jellyfin-favorited";
        LogLevel = "Info";
    }

    /// <summary>
    /// Gets or sets the Sonarr base URL.
    /// </summary>
    public string SonarrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; }

    /// <summary>
    /// Gets or sets the Radarr base URL.
    /// </summary>
    public string RadarrUrl { get; set; }

    /// <summary>
    /// Gets or sets the Radarr API key.
    /// </summary>
    public string RadarrApiKey { get; set; }

    /// <summary>
    /// Gets or sets the tag name to apply to favorites.
    /// </summary>
    public string TagName { get; set; }

    /// <summary>
    /// Gets or sets the sync interval in hours (0 = manual only, no auto-scheduling).
    /// </summary>
    public double SyncIntervalHours { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the timestamp of the last sync.
    /// </summary>
    public DateTime? LastSyncTime { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the plugin is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether dry-run mode is enabled.
    /// </summary>
    public bool DryRunMode { get; set; }

    /// <summary>
    /// Gets or sets the log level.
    /// </summary>
    public string LogLevel { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of API retries.
    /// </summary>
    public int MaxApiRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the API timeout in seconds.
    /// </summary>
    public int ApiTimeoutSeconds { get; set; } = 30;
}
