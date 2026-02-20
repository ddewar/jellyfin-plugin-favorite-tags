# Admin Dashboard Guide

## Overview

The Favorite Tags admin dashboard provides a web-based interface for managing the plugin configuration, monitoring sync status, and viewing history.

**Access:** Dashboard → Plugins → Favorite Tags

## Dashboard Sections

### Status Section

Displays real-time information about the plugin:

- **Last Sync:** Timestamp of the most recent sync (manual or scheduled)
- **Next Sync:** Estimated time of the next scheduled sync
- **Status:** Overall sync status (Success, Warning, Error, Unknown)
- **Running:** Whether a sync is currently in progress
- **Message:** Details about the last sync result

**Refresh Button:** Manually refresh status without page reload

### Configuration Section

Manage all plugin settings. Changes take effect immediately after saving.

#### Sonarr Settings
- **Sonarr URL:** Base URL of your Sonarr instance (e.g., `http://sonarr:8989`)
- **Sonarr API Key:** API key for authentication
- **Test Connection:** Verify credentials and connectivity

#### Radarr Settings
- **Radarr URL:** Base URL of your Radarr instance (e.g., `http://radarr:7878`)
- **Radarr API Key:** API key for authentication
- **Test Connection:** Verify credentials and connectivity

#### Sync Settings
- **Tag Name:** Name of the tag to apply to favorites (default: `jellyfin-favorited`)
- **Sync Interval:** How often to run automatic syncs in hours (0.25 - any value)
  - 0.25 = every 15 minutes
  - 0.5 = every 30 minutes
  - 1 = every hour (default)
  - 24 = once per day
- **Enable Plugin:** Toggle to enable/disable the plugin

#### Advanced Settings
- **Dry Run Mode:** When enabled, syncs show what would happen without making changes
- **Log Level:** Set verbosity of logs (Debug, Info, Warning, Error)
- **Max API Retries:** Number of times to retry failed API calls (default: 3)
- **API Timeout:** Seconds to wait for API responses (default: 30)

### Sync Actions Section

Manual operations for testing and maintenance:

- **Manual Sync:** Trigger a sync immediately
  - Useful for testing after configuration changes
  - Confirmation required before execution
  - Blocks other syncs while running

- **Dry Run:** Preview sync without making changes
  - See what items would be tagged
  - Verify matching works correctly
  - No changes applied to Sonarr/Radarr

- **Clear History:** Delete all sync records
  - **Warning:** This cannot be undone
  - Confirmation required

### Statistics Section

Overall sync metrics:

- **Total Syncs:** Number of sync operations (manual + scheduled)
- **Successful:** Syncs that completed without errors
- **Failed:** Syncs that encountered errors
- **Items Processed:** Total items evaluated across all syncs
- **Tags Applied:** Total tags created across all syncs
- **Tags Removed:** Total tags deleted across all syncs

### Recent Sync History

Table showing the last 20 sync operations:

| Column | Description |
|--------|-------------|
| Time | When the sync ran |
| Status | Success/Warning/Error |
| Processed | Number of items evaluated |
| Applied | Tags added |
| Removed | Tags deleted |
| Errors | Number of errors (clickable to view details) |

**Click error count** to see specific errors from that sync.

## Configuration Tips

### For Sonarr Only
- Leave Radarr URL and API Key blank
- Only series will be synced
- Movie favorites will be ignored

### For Radarr Only
- Leave Sonarr URL and API Key blank
- Only movies will be synced
- Series favorites will be ignored

### For Both
- Configure both URLs and API keys
- Series and movies will be synced separately
- Tags created in each system independently

### Finding API Keys

**Sonarr:**
1. Settings → General
2. Copy "API Key" value

**Radarr:**
1. Settings → General
2. Copy "API Key" value

### Testing Before Enabling

1. Configure URLs and API keys
2. Click "Test Connection" for each service
3. Verify green success message
4. Click "Dry Run" to preview what would happen
5. Review results
6. Enable and save configuration
7. Click "Manual Sync" to run immediately

### Troubleshooting Configuration

**"Cannot connect to Sonarr/Radarr"**
- Verify URL is correct (including port)
- Check service is running
- Verify network connectivity
- Check firewall rules

**"Invalid API key"**
- Copy API key from settings page directly
- Verify no extra spaces
- Check you're using correct key (not admin password)

**"At least one URL must be configured"**
- Configure at least Sonarr OR Radarr
- Both are optional but need at least one

**"Tag name cannot be empty"**
- Enter a valid tag name (alphanumeric)
- Common choice: "jellyfin-favorited"

## Auto-sync Schedule

By default, syncs run hourly at the configured interval.

**How interval works:**
- If set to 1 hour, syncs run at :00, 1:00, 2:00, etc.
- If set to 0.5 hours, syncs run every 30 minutes
- If set to 6 hours, syncs run every 6 hours

**Important:** Interval changes take effect on the next scheduled run

## Monitoring

### Real-time Monitoring
- Status section auto-refreshes every 30 seconds
- Statistics refresh every 60 seconds
- Manual refresh buttons available

### Log Files
For detailed troubleshooting, check Jellyfin logs:
- Jellyfin logs directory (varies by installation)
- Look for lines starting with `[Jellyfin.Plugin.FavoriteTags]`
- Set Log Level to "Debug" for verbose output

### Success Indicators
- Status shows "Success"
- Last Sync time is recent (within last sync interval)
- History shows recent successful syncs
- Statistics show growing totals

### Warning Signs
- Status shows "Warning" but not Error
- Some items not tagged (check error list)
- Sync taking unusually long
- API timeouts in logs

### Error Indicators
- Status shows "Error"
- Manual sync fails
- Connection test fails
- History shows mostly failed syncs

## Performance

### Typical Sync Times
- 100 favorites, good network: ~5-10 seconds
- 500 favorites, good network: ~20-30 seconds
- 1000+ favorites, good network: ~1-2 minutes

### Optimization Tips
1. Increase API timeout if getting timeouts
2. Reduce max retries if network is stable
3. Run during off-peak hours
4. Use more frequent syncs with smaller batches

### Large Library Handling
- Plugin can handle thousands of favorites
- Performance scales with network speed to Sonarr/Radarr
- Consider running syncs during night hours
- Dry run shows performance before enabling

## Common Workflows

### Initial Setup
1. Install plugin
2. Go to Plugins → Favorite Tags
3. Enter Sonarr and/or Radarr URLs and API keys
4. Click "Test Connection" for each
5. Review advanced settings
6. Save configuration
7. Wait for first scheduled sync OR click "Manual Sync"
8. Check history to verify

### Testing Configuration Changes
1. Update URL, API key, or tag name
2. Click "Dry Run"
3. Review results (what would be tagged)
4. If correct, save configuration
5. Run "Manual Sync" to apply changes

### Regular Maintenance
1. Check status weekly
2. Review statistics monthly
3. No manual intervention usually needed
4. Adjust sync interval if needed
5. Clear history if it gets too long (optional)

### Troubleshooting Issues
1. Click "Refresh" in status section
2. Run "Dry Run" to see matching
3. Check sync history for specific errors
4. Set Log Level to "Debug"
5. Check Jellyfin logs for detailed messages
6. Run "Manual Sync" to test immediately
7. Test connections individually

## Keyboard Shortcuts

| Action | Key |
|--------|-----|
| Submit form | Ctrl+Enter |
| Reset form | Escape (in focused field) |
| Search in history | Ctrl+F (browser search) |

## Accessibility

The dashboard is designed with accessibility in mind:

- Keyboard navigation supported (Tab to navigate)
- Color-blind friendly indicators (badges have text labels)
- Screen reader compatible (proper labels and semantic HTML)
- Mobile responsive (works on tablets and phones)

## Mobile Access

The dashboard is fully responsive:

- Works on tablets
- Simplified layout on phones
- Touch-friendly buttons and controls
- All features accessible on mobile

## Browser Support

- Chrome/Edge 90+
- Firefox 88+
- Safari 14+
- Opera 76+

## Data Privacy

All settings are:
- Stored locally in Jellyfin
- Encrypted at rest (API keys)
- Never sent to external services except Sonarr/Radarr
- Admin-only access (requires authorization)

## Limitations

- Cannot edit Sonarr/Radarr data (read-only sync)
- Cannot manually select which items to tag
- Cannot create custom matching rules
- History limited to most recent 50 syncs (in memory)
