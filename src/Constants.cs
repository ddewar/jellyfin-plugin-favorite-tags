namespace Jellyfin.Plugin.FavoriteTags;

/// <summary>
/// Plugin constants.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Default tag name for favorited items.
    /// </summary>
    public const string DefaultTagName = "jellyfin-favorited";

    /// <summary>
    /// Default sync interval in hours.
    /// </summary>
    public const double DefaultSyncIntervalHours = 1.0;

    /// <summary>
    /// Minimum sync interval in hours.
    /// </summary>
    public const double MinimumSyncIntervalHours = 0.25;

    /// <summary>
    /// Default maximum API retries.
    /// </summary>
    public const int DefaultMaxApiRetries = 3;

    /// <summary>
    /// Default API timeout in seconds.
    /// </summary>
    public const int DefaultApiTimeoutSeconds = 30;

    /// <summary>
    /// Sonarr API version.
    /// </summary>
    public const string SonarrApiVersion = "v3";

    /// <summary>
    /// Radarr API version.
    /// </summary>
    public const string RadarrApiVersion = "v3";

    /// <summary>
    /// Sonarr API base path.
    /// </summary>
    public const string SonarrApiBasePath = "/api/v3";

    /// <summary>
    /// Radarr API base path.
    /// </summary>
    public const string RadarrApiBasePath = "/api/v3";

    /// <summary>
    /// Log category name.
    /// </summary>
    public const string LogCategoryName = "Jellyfin.Plugin.FavoriteTags";

    /// <summary>
    /// Default log level.
    /// </summary>
    public const string DefaultLogLevel = "Info";
}
