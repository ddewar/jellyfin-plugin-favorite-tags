# Favorite Tags Task Lifecycle

## Overview

The Favorite Tags plugin uses Jellyfin's built-in task scheduler to automatically sync user favorites to Sonarr/Radarr tags on a configurable schedule.

## Task Components

### FavoritesSyncTask
Implements `IScheduledTask` and is the entry point for scheduled syncs.

**Properties:**
- **Name:** "Favorite Tags Sync"
- **Category:** Library
- **Key:** "FavoriteTagsSync"
- **Enabled:** True by default
- **Logged:** True (sends output to Jellyfin logs)
- **Interval:** Configurable (default: 1 hour)

**Execution Flow:**
1. Check if plugin is enabled (from configuration)
2. Execute `SyncService.SyncFavoritesAsync(dryRun: false)`
3. Track result in `SyncHistoryTracker`
4. Log results and any errors
5. Report progress (0% → 100%)

## Configuration

### Sync Interval

The sync interval is read dynamically from the plugin configuration:

```csharp
// In FavoritesSyncTask.Interval property:
var config = plugin.Configuration;
var interval = TimeSpan.FromHours(config.SyncIntervalHours);
```

**Default:** 1 hour
**Minimum:** 0.25 hours (15 minutes)
**Maximum:** No limit (user configurable)

### Changing Interval

When the sync interval is changed in settings:

1. Admin updates settings in dashboard
2. `PluginConfiguration` is updated
3. `FavoriteTagsPlugin.OnConfigurationUpdated()` is called
4. Plugin logs the change
5. Next task interval uses the new value

**Note:** The interval takes effect for the *next* scheduled run. The currently running task is not affected.

## Task Lifecycle

### Startup
1. Jellyfin starts
2. Plugin is loaded
3. `FavoriteTagsPlugin` instance created
4. Services registered via `ServiceRegistration.AddFavoriteTags()`
5. `FavoritesSyncTask` registered with task scheduler
6. First scheduled run set based on interval

### Scheduled Execution
1. Task scheduler triggers at configured interval
2. `ExecuteAsync()` called with progress callback
3. Plugin enabled check performed
4. `SyncService.SyncFavoritesAsync()` executes
5. Results tracked in history
6. Task completes, next run scheduled

### Configuration Update
1. Admin changes settings (e.g., Sonarr URL, sync interval)
2. `OnConfigurationUpdated()` triggered
3. Configuration logged
4. Next task run uses new configuration

### Shutdown
1. Jellyfin begins shutdown
2. Any running sync is allowed to complete
3. Task scheduler stops scheduling new runs
4. Plugin disposes resources

## Error Handling

### During Execution

**Transient Errors (retried by SyncService):**
- Network timeout
- API rate limit (429)
- Service unavailable (503)
- Handled with exponential backoff

**Non-transient Errors (logged, sync continues):**
- Item not found in Sonarr/Radarr
- Invalid API key
- Configuration invalid
- Logged as warnings, sync continues with other items

**Fatal Errors:**
- Jellyfin unavailable
- Can't create tags
- Logged as errors, task reports failure

### Task-level Error Handling

If `ExecuteAsync()` throws an exception:
1. Exception logged
2. Progress reported as 100% (completed)
3. Exception re-thrown to task scheduler
4. Task marked as failed
5. Next scheduled run will attempt again

## History Tracking

The `SyncHistoryTracker` keeps recent sync results:

**Features:**
- Stores last 50 sync results by default
- Accessible for dashboard display
- Provides statistics (total syncs, successes, failures, etc.)
- Used for admin UI to show last sync time and status

**Access:**
```csharp
var tracker = serviceProvider.GetRequiredService<SyncHistoryTracker>();
var recent = tracker.GetRecent(10);  // Last 10 syncs
var stats = tracker.GetStatistics(); // Overall stats
```

## Service Registration

Services are registered in `ServiceRegistration.cs`:

```csharp
services.AddFavoriteTags(configuration);
```

**Registered Services:**
- `SyncHistoryTracker` - Singleton (shared history)
- `JellyfinFavoritesService` - Scoped
- `SonarrService` - Scoped
- `RadarrService` - Scoped
- `SyncService` - Scoped
- `FavoritesSyncTask` - Scoped (IScheduledTask)

**Scoped Services:** Fresh instance per task execution
**Singleton:** Shared across all task runs

## Logging

Task logs go to standard Jellyfin logs:

**Info Level:**
- Task starting/completion
- Configuration changes
- Sync results (items tagged, removed)

**Warning Level:**
- Title-based matches (less reliable than ID matching)
- Items not found in Sonarr/Radarr
- Configuration issues

**Error Level:**
- API failures
- Fatal errors during sync
- Service unavailability

**Debug Level:**
- Item matching attempts
- Tag existence checks
- Detailed operation steps

## Metrics

Available from `SyncHistoryTracker.GetStatistics()`:

```json
{
  "total_syncs": 24,
  "successful_syncs": 23,
  "failed_syncs": 1,
  "total_items_processed": 487,
  "total_tags_applied": 245,
  "total_tags_removed": 15,
  "total_errors": 3,
  "first_sync": "2024-01-15T10:30:00Z",
  "last_sync": "2024-01-16T16:45:00Z"
}
```

## Troubleshooting

### Task Not Running

**Check:**
1. Plugin enabled in settings
2. Jellyfin task scheduler running
3. Check Jellyfin logs for errors
4. Verify configuration is valid

### Task Running But Not Syncing

**Check:**
1. Sonarr/Radarr URLs and API keys configured
2. At least one URL configured (Sonarr or Radarr)
3. Network connectivity to services
4. API keys are valid

### Errors in Log

**Common Issues:**
- `"At least one of Sonarr URL or Radarr URL must be configured"` - Configure URL in settings
- `"Sonarr API key is required"` - Add API key if URL is set
- `"Failed to connect to Sonarr"` - Check URL, API key, network connectivity

### Performance

**Sync Slow?**
- Check number of favorites (scales with library size)
- Check network latency to Sonarr/Radarr
- Review API timeout settings (default: 30 seconds)

**Too Frequent?**
- Increase sync interval in settings
- Reduce max API retries if desired

## Monitoring

### Dashboard
Admin dashboard shows:
- Last sync time
- Next scheduled sync
- Sync status (Success/Warning/Error)
- Recent sync history
- Statistics

### Jellyfin Logs
Task activity logged with timestamps and details.

### Manual Sync
Dashboard allows manual sync trigger for testing:
- Normal sync: Makes actual changes
- Dry-run sync: Shows what would happen without changes
