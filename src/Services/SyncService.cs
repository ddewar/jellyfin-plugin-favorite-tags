using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Plugin.FavoriteTags.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FavoriteTags.Services;

/// <summary>
/// Service for syncing Jellyfin favorites to Sonarr/Radarr tags.
/// </summary>
public class SyncService
{
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private bool _isSyncing;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncService"/> class.
    /// </summary>
    /// <param name="userManager">User manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public SyncService(IUserManager userManager, ILibraryManager libraryManager, ILoggerFactory loggerFactory)
    {
        _userManager = userManager;
        _libraryManager = libraryManager;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger("FavoriteTags.SyncService");
        _isSyncing = false;
    }

    /// <summary>
    /// Gets a value indicating whether a sync is currently running.
    /// </summary>
    public bool IsSyncing => _isSyncing;

    /// <summary>
    /// Sync Jellyfin favorites to Sonarr/Radarr.
    /// </summary>
    /// <param name="config">Plugin configuration.</param>
    /// <param name="dryRun">If true, preview changes without applying.</param>
    /// <returns>Sync result summary.</returns>
    public async Task<SyncResult> SyncFavoritesAsync(PluginConfiguration config, bool dryRun = false)
    {
        if (_isSyncing)
        {
            _logger.LogWarning("Sync already in progress");
            return new SyncResult { Success = false, Message = "Sync already in progress" };
        }

        _isSyncing = true;
        try
        {
            var result = new SyncResult
            {
                StartTime = DateTime.UtcNow,
                DryRun = dryRun
            };

            // Validate configuration
            if (string.IsNullOrEmpty(config.SonarrUrl) && string.IsNullOrEmpty(config.RadarrUrl))
            {
                result.Success = false;
                result.Message = "No services configured";
                return result;
            }

            // Get all user favorites
            var favorites = GetAllUserFavorites();
            result.ItemsProcessed = favorites.Count;

            if (favorites.Count == 0)
            {
                result.Success = true;
                result.Message = "No favorites to sync";
                return result;
            }

            _logger.LogInformation("Found {Count} favorite items to sync", favorites.Count);

            // Sync to Sonarr
            if (!string.IsNullOrEmpty(config.SonarrUrl))
            {
                await SyncToSonarrAsync(favorites, config, result, dryRun);
            }

            // Sync to Radarr
            if (!string.IsNullOrEmpty(config.RadarrUrl))
            {
                await SyncToRadarrAsync(favorites, config, result, dryRun);
            }

            result.EndTime = DateTime.UtcNow;
            result.Success = true;

            if (dryRun && result.Changes.Count > 0)
            {
                result.Message = $"Dry Run: Would apply {result.TagsApplied} tags, remove {result.TagsRemoved} tags ({result.ItemsProcessed} items)";
            }
            else
            {
                result.Message = $"Synced {result.TagsApplied} tags (applied), {result.TagsRemoved} tags (removed) ({result.ItemsProcessed} items)";
                // Update last sync time only for actual syncs, not dry-runs
                config.LastSyncTime = DateTime.UtcNow;
            }

            _logger.LogInformation("Sync completed: {Message}", result.Message);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync");
            return new SyncResult
            {
                Success = false,
                Message = ex.Message,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private Dictionary<string, FavoriteItem> GetAllUserFavorites()
    {
        var favorites = new Dictionary<string, FavoriteItem>();

        try
        {
            // Get all users from the library manager
            var allUsers = _userManager.Users;

            foreach (var user in allUsers)
            {
                if (user == null)
                {
                    continue;
                }

                _logger.LogDebug("Getting favorites for user {UserName}", user.Username);

                try
                {
                    // Get favorite items for this user
                    var query = new InternalItemsQuery(user)
                    {
                        IsFavorite = true,
                        Recursive = true,
                        IsVirtualItem = false
                    };

                    var userFavorites = _libraryManager.GetItemList(query);

                    foreach (var item in userFavorites)
                    {
                        if (item?.GetType() == null)
                        {
                            continue;
                        }

                        var key = $"{item.GetType().Name}:{item.Id}";

                        if (!favorites.ContainsKey(key))
                        {
                            favorites[key] = new FavoriteItem
                            {
                                ItemId = item.Id,
                                Name = item.Name,
                                ItemType = item.GetType().Name,
                                Users = new List<string>()
                            };
                        }

                        if (!favorites[key].Users.Contains(user.Username ?? "Unknown"))
                        {
                            favorites[key].Users.Add(user.Username ?? "Unknown");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting favorites for user {UserName}", user.Username);
                }
            }

            _logger.LogInformation("Found {Count} unique favorite items across all users", favorites.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user favorites");
        }

        return favorites;
    }

    private async Task SyncToSonarrAsync(Dictionary<string, FavoriteItem> favorites, PluginConfiguration config, SyncResult result, bool dryRun)
    {
        try
        {
            var sonarrLogger = _loggerFactory.CreateLogger("FavoriteTags.SonarrService");
            var sonarrService = new SonarrService(config.SonarrUrl, config.SonarrApiKey, new System.Net.Http.HttpClient(), sonarrLogger);

            // Test connection
            if (!await sonarrService.TestConnectionAsync())
            {
                _logger.LogError("Failed to connect to Sonarr");
                result.Errors.Add("Failed to connect to Sonarr");
                return;
            }

            // Get or create tag (skip in dry run)
            if (dryRun)
            {
                _logger.LogDebug("[DRY-RUN] Skipping tag creation in Sonarr");
            }

            var tagId = dryRun ? 0 : await sonarrService.GetOrCreateTagAsync(config.TagName);
            if (!dryRun && tagId < 0)
            {
                _logger.LogError("Failed to get or create tag in Sonarr");
                result.Errors.Add("Failed to create tag in Sonarr");
                return;
            }

            // Process series favorites (add tags)
            var seriesFavorites = favorites.Values.Where(f => f.ItemType == "Series").ToList();
            var favoritedSeriesIds = new HashSet<int>();

            foreach (var favorite in seriesFavorites)
            {
                try
                {
                    var item = _libraryManager.GetItemById(favorite.ItemId);
                    if (item == null)
                    {
                        _logger.LogWarning("Series item not found: {ItemId}", favorite.ItemId);
                        continue;
                    }

                    // Try to find series in Sonarr
                    var tvdbId = item.GetProviderId("Tvdb");
                    _logger.LogInformation("Processing series: {SeriesName}, TVDB ID: {TvdbId}", item.Name, tvdbId ?? "NONE");

                    if (int.TryParse(tvdbId, out var tvdbIdNum))
                    {
                        var series = await sonarrService.GetSeriesByTvdbIdAsync(tvdbIdNum);
                        if (series != null)
                        {
                            favoritedSeriesIds.Add(series.Id);
                            if (!dryRun)
                            {
                                if (await sonarrService.AddTagToSeriesAsync(series.Id, tagId))
                                {
                                    result.TagsApplied++;
                                    _logger.LogInformation("Tagged series {SeriesName} in Sonarr", series.Title);
                                }
                            }
                            else
                            {
                                result.TagsApplied++;
                                var hasTag = series.Tags?.Contains(tagId) ?? false;
                                if (hasTag)
                                {
                                    result.Changes.Add($"[Sonarr] {series.Title} - tag already present");
                                }
                                else
                                {
                                    result.Changes.Add($"[Sonarr] Would add tag to: {series.Title}");
                                }
                                _logger.LogInformation("[DRY-RUN] Would tag series {SeriesName} in Sonarr", series.Title);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Series not found in Sonarr with TVDB ID: {TvdbId}", tvdbIdNum);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No TVDB ID found for series: {SeriesName}", item.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing series favorite");
                    result.Errors.Add(ex.Message);
                }
            }

            // Process removal of tags from unfavorited series
            var taggedSeries = await sonarrService.GetSeriesWithTagAsync(tagId);
            foreach (var series in taggedSeries)
            {
                if (!favoritedSeriesIds.Contains(series.Id))
                {
                    if (dryRun)
                    {
                        result.Changes.Add($"[Sonarr] Would remove tag from: {series.Title}");
                        _logger.LogInformation("[DRY-RUN] Would remove tag from series {SeriesName}", series.Title);
                    }
                    else
                    {
                        if (await sonarrService.RemoveTagFromSeriesAsync(series.Id, tagId))
                        {
                            result.TagsRemoved++;
                            _logger.LogInformation("Removed tag from unfavorited series {SeriesName}", series.Title);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to Sonarr");
            result.Errors.Add($"Sonarr sync error: {ex.Message}");
        }
    }

    private async Task SyncToRadarrAsync(Dictionary<string, FavoriteItem> favorites, PluginConfiguration config, SyncResult result, bool dryRun)
    {
        try
        {
            var radarrLogger = _loggerFactory.CreateLogger("FavoriteTags.RadarrService");
            var radarrService = new RadarrService(config.RadarrUrl, config.RadarrApiKey, new System.Net.Http.HttpClient(), radarrLogger);

            // Test connection
            if (!await radarrService.TestConnectionAsync())
            {
                _logger.LogError("Failed to connect to Radarr");
                result.Errors.Add("Failed to connect to Radarr");
                return;
            }

            // Get or create tag (skip in dry run)
            if (dryRun)
            {
                _logger.LogDebug("[DRY-RUN] Skipping tag creation in Radarr");
            }

            var tagId = dryRun ? 0 : await radarrService.GetOrCreateTagAsync(config.TagName);
            if (!dryRun && tagId < 0)
            {
                _logger.LogError("Failed to get or create tag in Radarr");
                result.Errors.Add("Failed to create tag in Radarr");
                return;
            }

            // Process movie favorites (add tags)
            var movieFavorites = favorites.Values.Where(f => f.ItemType == "Movie").ToList();
            var favoritedMovieIds = new HashSet<int>();

            foreach (var favorite in movieFavorites)
            {
                try
                {
                    var item = _libraryManager.GetItemById(favorite.ItemId);
                    if (item == null)
                    {
                        _logger.LogWarning("Movie item not found: {ItemId}", favorite.ItemId);
                        continue;
                    }

                    // Try to find movie in Radarr
                    var imdbId = item.GetProviderId("Imdb");
                    _logger.LogInformation("Processing movie: {MovieName}, IMDB ID: {ImdbId}", item.Name, imdbId ?? "NONE");

                    if (!string.IsNullOrEmpty(imdbId))
                    {
                        var movie = await radarrService.GetMovieByImdbIdAsync(imdbId);
                        if (movie != null)
                        {
                            favoritedMovieIds.Add(movie.Id);
                            if (!dryRun)
                            {
                                if (await radarrService.AddTagToMovieAsync(movie.Id, tagId))
                                {
                                    result.TagsApplied++;
                                    _logger.LogInformation("Tagged movie {MovieName} in Radarr", movie.Title);
                                }
                            }
                            else
                            {
                                result.TagsApplied++;
                                var hasTag = movie.Tags?.Contains(tagId) ?? false;
                                if (hasTag)
                                {
                                    result.Changes.Add($"[Radarr] {movie.Title} - tag already present");
                                }
                                else
                                {
                                    result.Changes.Add($"[Radarr] Would add tag to: {movie.Title}");
                                }
                                _logger.LogInformation("[DRY-RUN] Would tag movie {MovieName} in Radarr", movie.Title);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Movie not found in Radarr with IMDB ID: {ImdbId}", imdbId);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("No IMDB ID found for movie: {MovieName}", item.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing movie favorite");
                    result.Errors.Add(ex.Message);
                }
            }

            // Process removal of tags from unfavorited movies
            var taggedMovies = await radarrService.GetMoviesWithTagAsync(tagId);
            foreach (var movie in taggedMovies)
            {
                if (!favoritedMovieIds.Contains(movie.Id))
                {
                    if (dryRun)
                    {
                        result.Changes.Add($"[Radarr] Would remove tag from: {movie.Title}");
                        _logger.LogInformation("[DRY-RUN] Would remove tag from movie {MovieName}", movie.Title);
                    }
                    else
                    {
                        if (await radarrService.RemoveTagFromMovieAsync(movie.Id, tagId))
                        {
                            result.TagsRemoved++;
                            _logger.LogInformation("Removed tag from unfavorited movie {MovieName}", movie.Title);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing to Radarr");
            result.Errors.Add($"Radarr sync error: {ex.Message}");
        }
    }
}

/// <summary>
/// Favorite item model.
/// </summary>
public class FavoriteItem
{
    /// <summary>
    /// Gets or sets the Jellyfin item ID.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the item name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the item type (Series, Movie, etc).
    /// </summary>
    public string? ItemType { get; set; }

    /// <summary>
    /// Gets or sets the users who favorited this item.
    /// </summary>
    public List<string> Users { get; set; } = new();
}

/// <summary>
/// Sync result model.
/// </summary>
public class SyncResult
{
    /// <summary>
    /// Gets or sets a value indicating whether sync was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the sync message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start time.
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the end time.
    /// </summary>
    public DateTime EndTime { get; set; }

    /// <summary>
    /// Gets or sets the number of items processed.
    /// </summary>
    public int ItemsProcessed { get; set; }

    /// <summary>
    /// Gets or sets the number of tags applied.
    /// </summary>
    public int TagsApplied { get; set; }

    /// <summary>
    /// Gets or sets the number of tags removed.
    /// </summary>
    public int TagsRemoved { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this was a dry run.
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    /// Gets or sets the list of errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets detailed changes for dry-run reporting.
    /// </summary>
    public List<string> Changes { get; set; } = new();
}
