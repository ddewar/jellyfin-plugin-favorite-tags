using System.Threading.Tasks;
using Jellyfin.Plugin.FavoriteTags.Configuration;
using Jellyfin.Plugin.FavoriteTags.Services;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.FavoriteTags.Controllers;

/// <summary>
/// Controller for sync operations.
/// </summary>
[ApiController]
[Route("Plugins/FavoriteTags")]
public class SyncController : ControllerBase
{
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IUserManager _userManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<SyncController> _logger;
    private SyncService? _syncService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SyncController"/> class.
    /// </summary>
    /// <param name="configurationManager">Configuration manager.</param>
    /// <param name="userManager">User manager.</param>
    /// <param name="libraryManager">Library manager.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    /// <param name="logger">Logger.</param>
    public SyncController(
        IServerConfigurationManager configurationManager,
        IUserManager userManager,
        ILibraryManager libraryManager,
        ILoggerFactory loggerFactory,
        ILogger<SyncController> logger)
    {
        _configurationManager = configurationManager;
        _userManager = userManager;
        _libraryManager = libraryManager;
        _loggerFactory = loggerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Get plugin configuration.
    /// </summary>
    /// <returns>Current configuration.</returns>
    [HttpGet("Configuration")]
    [Authorize]
    public ActionResult<PluginConfiguration> GetConfiguration()
    {
        var plugin = FavoriteTagsPlugin.Instance;
        if (plugin == null)
        {
            return BadRequest("Plugin not loaded");
        }

        return plugin.Configuration;
    }

    /// <summary>
    /// Save plugin configuration.
    /// </summary>
    /// <param name="config">Configuration to save.</param>
    /// <returns>Success response.</returns>
    [HttpPost("Configuration")]
    [Authorize]
    public ActionResult SaveConfiguration([FromBody] PluginConfiguration config)
    {
        var plugin = FavoriteTagsPlugin.Instance;
        if (plugin == null)
        {
            return BadRequest("Plugin not loaded");
        }

        try
        {
            plugin.UpdateConfiguration(config);
            return Ok(new { message = "Configuration saved" });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger manual sync.
    /// </summary>
    /// <returns>Sync result.</returns>
    [HttpPost("Sync")]
    [Authorize]
    public async Task<ActionResult> TriggerSync()
    {
        var plugin = FavoriteTagsPlugin.Instance;
        if (plugin == null)
        {
            return BadRequest("Plugin not loaded");
        }

        try
        {
            _syncService ??= new SyncService(_userManager, _libraryManager, _loggerFactory);

            if (_syncService.IsSyncing)
            {
                return BadRequest(new { error = "Sync already in progress" });
            }

            var result = await _syncService.SyncFavoritesAsync(plugin.Configuration, dryRun: false);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error during sync");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Trigger dry-run sync (preview only).
    /// </summary>
    /// <returns>Dry-run result.</returns>
    [HttpPost("Sync/DryRun")]
    [Authorize]
    public async Task<ActionResult> TriggerDryRun()
    {
        var plugin = FavoriteTagsPlugin.Instance;
        if (plugin == null)
        {
            return BadRequest("Plugin not loaded");
        }

        try
        {
            _syncService ??= new SyncService(_userManager, _libraryManager, _loggerFactory);

            if (_syncService.IsSyncing)
            {
                return BadRequest(new { error = "Sync already in progress" });
            }

            var result = await _syncService.SyncFavoritesAsync(plugin.Configuration, dryRun: true);
            return Ok(result);
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error during dry-run");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test Sonarr connection.
    /// </summary>
    /// <param name="request">Test request with URL and API key.</param>
    /// <returns>Connection test result.</returns>
    [HttpPost("Test/Sonarr")]
    [Authorize]
    public async Task<ActionResult> TestSonarrConnection([FromBody] TestConnectionRequest? request)
    {
        try
        {
            string url = request?.Url ?? FavoriteTagsPlugin.Instance?.Configuration?.SonarrUrl ?? string.Empty;
            string apiKey = request?.ApiKey ?? FavoriteTagsPlugin.Instance?.Configuration?.SonarrApiKey ?? string.Empty;

            if (string.IsNullOrEmpty(url))
            {
                return BadRequest(new { error = "Sonarr URL not configured" });
            }

            _logger.LogInformation("Testing Sonarr connection to: {SonarrUrl}", url);

            var sonarrService = new SonarrService(url, apiKey, new System.Net.Http.HttpClient(), _loggerFactory.CreateLogger("Test.Sonarr"));
            var success = await sonarrService.TestConnectionAsync();

            _logger.LogInformation("Sonarr test result: {Success}", success);

            return Ok(new { success, message = success ? "Connected to Sonarr" : "Failed to connect to Sonarr" });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error testing Sonarr connection");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test Radarr connection.
    /// </summary>
    /// <param name="request">Test request with URL and API key.</param>
    /// <returns>Connection test result.</returns>
    [HttpPost("Test/Radarr")]
    [Authorize]
    public async Task<ActionResult> TestRadarrConnection([FromBody] TestConnectionRequest? request)
    {
        try
        {
            string url = request?.Url ?? FavoriteTagsPlugin.Instance?.Configuration?.RadarrUrl ?? string.Empty;
            string apiKey = request?.ApiKey ?? FavoriteTagsPlugin.Instance?.Configuration?.RadarrApiKey ?? string.Empty;

            if (string.IsNullOrEmpty(url))
            {
                return BadRequest(new { error = "Radarr URL not configured" });
            }

            _logger.LogInformation("Testing Radarr connection to: {RadarrUrl}", url);

            var radarrService = new RadarrService(url, apiKey, new System.Net.Http.HttpClient(), _loggerFactory.CreateLogger("Test.Radarr"));
            var success = await radarrService.TestConnectionAsync();

            _logger.LogInformation("Radarr test result: {Success}", success);

            return Ok(new { success, message = success ? "Connected to Radarr" : "Failed to connect to Radarr" });
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, "Error testing Radarr connection");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get plugin status.
    /// </summary>
    /// <returns>Status information.</returns>
    [HttpGet("Status")]
    [Authorize]
    public ActionResult GetStatus()
    {
        var plugin = FavoriteTagsPlugin.Instance;
        if (plugin == null)
        {
            return BadRequest("Plugin not loaded");
        }

        return Ok(new
        {
            enabled = plugin.Configuration.Enabled,
            running = _syncService?.IsSyncing ?? false,
            lastSync = "Never",
            nextSync = "N/A"
        });
    }

    /// <summary>
    /// Health check endpoint.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet("Health")]
    [AllowAnonymous]
    public ActionResult GetHealth()
    {
        return Ok(new { status = "ok", message = "FavoriteTags plugin is running" });
    }
}

/// <summary>
/// Test connection request.
/// </summary>
public class TestConnectionRequest
{
    /// <summary>
    /// Gets or sets the service URL.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the API key.
    /// </summary>
    public string? ApiKey { get; set; }
}
