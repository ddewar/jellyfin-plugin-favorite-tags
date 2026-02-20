# Developer Guide - Favorite Tags Plugin

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│         Jellyfin Task Scheduler                     │
│  (Triggers FavoritesSyncTask at configured interval)│
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
         ┌─────────────────────────┐
         │  FavoritesSyncTask      │
         │  (IScheduledTask impl)  │
         └────────────┬────────────┘
                      │
                      ▼
         ┌─────────────────────────┐
         │    SyncService          │
         │  (Orchestrates sync)    │
         └────────┬────────────────┘
                  │
        ┌─────────┼─────────┐
        ▼         ▼         ▼
    ┌────────┐┌────────┐┌────────┐
    │Jellyfin││Sonarr  ││Radarr  │
    │Service ││Service ││Service │
    └────────┘└────────┘└────────┘
```

## Key Classes

### FavoriteTagsPlugin
Main plugin class, entry point for Jellyfin.

```csharp
public class FavoriteTagsPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    // Configuration management
    // Web page hosting
    // Lifecycle hooks
}
```

### FavoritesSyncTask
Scheduled task implementation.

```csharp
public class FavoritesSyncTask : IScheduledTask
{
    // Implements: Name, Description, Key, Interval, etc.
    // ExecuteAsync() - runs on schedule
}
```

### SyncService
Core sync orchestration logic.

```csharp
public class SyncService
{
    public async Task<SyncResult> SyncFavoritesAsync(bool dryRun = false)
    {
        // 1. Validate configuration
        // 2. Get Sonarr/Radarr tag IDs
        // 3. Get all user favorites from Jellyfin
        // 4. Process each favorite (match, tag)
        // 5. Return results
    }
}
```

### API Services

**JellyfinFavoritesService**
- Query Jellyfin for user favorites
- Extract external IDs (TVDB, IMDB)
- Aggregate across multiple users

**SonarrService** / **RadarrService**
- Query by ID, search by title
- Create/get tags
- Apply/remove tags from items

### BaseApiService
Common HTTP client functionality.

```csharp
public abstract class BaseApiService
{
    // Retry logic with exponential backoff
    // Error handling
    // JSON serialization
    // Timeout handling
}
```

## Service Registration

Services are registered in `ServiceRegistration.cs`:

```csharp
// In Jellyfin plugin startup:
services.AddFavoriteTags(configuration);

// Registers:
// - SyncHistoryTracker (singleton)
// - JellyfinFavoritesService (scoped)
// - SonarrService (scoped)
// - RadarrService (scoped)
// - SyncService (scoped)
// - FavoritesSyncTask (scoped, IScheduledTask)
```

## Configuration Flow

```
Admin Dashboard
      │
      ▼
AdminController (saves config)
      │
      ▼
PluginConfiguration (persisted)
      │
      ▼
FavoriteTagsPlugin.OnConfigurationUpdated()
      │
      ▼
SyncService reads config
      │
      ▼
Next sync uses new settings
```

## Sync Flow

```
FavoritesSyncTask.ExecuteAsync()
    │
    ├─ Check plugin enabled
    │
    ├─ Validate configuration
    │
    ├─ Get/create Sonarr tag
    │
    ├─ Get/create Radarr tag
    │
    ├─ Get all user favorites
    │   │
    │   └─ Per favorite:
    │       ├─ Try ID-based match
    │       ├─ Fallback to title search
    │       ├─ If found, apply tag (idempotent)
    │       └─ Log result
    │
    ├─ Track result in history
    │
    └─ Report completion
```

## Data Flow

### Favorites Query
```
Jellyfin DB
    ↓
IUserManager, ILibraryManager
    ↓
JellyfinFavoritesService.GetAllUserFavoritesAsync()
    ↓
Dictionary<ItemId, FavoriteItem>
    ├─ ItemId: "item-123"
    ├─ Name: "Breaking Bad"
    ├─ Type: Series
    ├─ ExternalIds: { tvdb: "81189" }
    └─ UserIds: ["user1", "user2"]
```

### Matching
```
FavoriteItem
    ├─ Extract TVDB/IMDB ID
    │       ↓
    │   SonarrService/RadarrService.GetBy{Id}Async()
    │       ↓
    │   Found → SonarrSeries/RadarrMovie
    │
    └─ If not found, try title search
            ↓
        SonarrService/RadarrService.GetBy{Title}Async()
            ↓
        Found → SonarrSeries/RadarrMovie
```

### Tagging
```
SonarrSeries/RadarrMovie
    ├─ Check if tag already exists
    │   ├─ Yes → Skip (idempotent)
    │   └─ No → Add tag
    │
    ├─ If Dry-run: Log action only
    ├─ If Normal: API call to Sonarr/Radarr
    │
    └─ Return result (success/failure)
```

## Error Handling Strategy

```
Error Type          Action
─────────────────────────────────────────────────
Network timeout     → Retry with backoff
API rate limit      → Retry with backoff
Server error        → Retry with backoff
Invalid config      → Fail validation, report error
Item not found      → Log warning, continue
API key invalid     → Log error, stop processing
Item already tagged → No action (idempotent)
```

## Testing

### Unit Tests

**SyncServiceTests:**
- Idempotency verification
- Dry-run mode behavior
- Error handling
- Multi-user aggregation

**API Service Tests:**
- Configuration validation
- Connection testing
- Error response handling

### Integration Tests

Structure in place to test:
- Full sync workflow (mocked APIs)
- Retry logic with transient errors
- Configuration persistence

**Run tests:**
```bash
dotnet test
```

## Adding New Features

### Example: Add Lidarr Support

1. Create `LidarrService` (extends `BaseApiService`)
   - Implement artist lookup
   - Tag management similar to Sonarr

2. Update `PluginConfiguration`
   - Add `LidarrUrl`, `LidarrApiKey`

3. Update `SyncService`
   - Add music handling (new MediaType)
   - Process music favorites

4. Update `Models/FavoriteItem`
   - Add music-specific external IDs

5. Register in `ServiceRegistration`
   - Add LidarrService

## Debugging

### Enable Debug Logging

In Jellyfin configuration or logs:
```
Set log level to Debug for FavoriteTags
```

### Check Task Execution

Jellyfin logs show:
```
[2024-01-16 10:30:00.000] Inf [Jellyfin.Plugin.FavoriteTags] Starting Favorite Tags Sync task
[2024-01-16 10:30:05.000] Inf [Jellyfin.Plugin.FavoriteTags] Favorite Tags Sync completed successfully: 15 tagged, 2 removed
```

### Manual Sync via Dashboard

1. Go to Plugins → Favorite Tags
2. Click "Manual Sync" button
3. View results in recent sync table
4. Check logs for details

### Dry-run Testing

1. Click "Dry Run" button
2. See what would happen
3. Verify matches and actions
4. Then execute real sync

## Code Style

- XML documentation on public members
- Async/await for all I/O operations
- Dependency injection via constructor
- Custom exceptions for error context
- Structured logging with context variables

## Performance Considerations

- Sync is I/O bound (network to Sonarr/Radarr)
- Multi-user aggregation is O(n) where n = number of users
- Matching is O(1) per item (single API call per item)
- Total sync time scales with library size + network latency

**Typical Performance:**
- 100 favorites, good network: ~5-10 seconds
- 1000 favorites, slow network: ~30-60 seconds

## Security

- API keys encrypted in Jellyfin storage
- No hardcoded credentials
- Validates URLs before use
- No injection vulnerabilities (parameterized queries)
- Admin-only access to configuration

## Version Compatibility

- Targets Jellyfin 10.8.x+
- .NET 8.0
- Built-in dependencies only (Jellyfin SDK, HttpClient)
