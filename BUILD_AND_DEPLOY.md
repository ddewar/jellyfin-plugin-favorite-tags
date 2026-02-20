# Build and Deploy Guide

## Local Development Setup

### Prerequisites
- .NET 9 SDK
- Jellyfin instance running locally at `http://192.168.15.1:8096`
- Sonarr instance at `http://192.168.15.1:8989`
- Radarr instance at `http://192.168.15.1:7878`
- Nix shell (recommended): `nix shell nixpkgs#dotnet-sdk_9`

---

## Building the Plugin

### Full Clean Build
```bash
cd /home/dan/source/jellyfin-plugin-favorite-tags/src
/nix/store/7bsxdrbx302w9b6rz3i4pv1szzz8wqb8-dotnet-sdk-9.0.310/share/dotnet/dotnet clean
/nix/store/7bsxdrbx302w9b6rz3i4pv1szzz8wqb8-dotnet-sdk-9.0.310/share/dotnet/dotnet build -c Release
```

**Output:** `bin/Release/net9.0/Jellyfin.Plugin.FavoriteTags.dll`

### Quick Rebuild (if only doing minor changes)
```bash
cd /home/dan/source/jellyfin-plugin-favorite-tags/src
/nix/store/7bsxdrbx302w9b6rz3i4pv1szzz8wqb8-dotnet-sdk-9.0.310/share/dotnet/dotnet build -c Release
```

---

## Deploying to Local Jellyfin

### Step 1: Copy DLL to Plugin Directory
```bash
sudo cp /home/dan/source/jellyfin-plugin-favorite-tags/src/bin/Release/net9.0/Jellyfin.Plugin.FavoriteTags.dll "/apps/jellyfin/config/plugins/Favorite Tags_0.1.0.0/"
```

### Step 2: Restart Jellyfin
```bash
sudo systemctl restart jellyfin
```

### Step 3: Wait for Restart
```bash
sleep 5
```

### Combined: Build, Deploy, and Restart
```bash
cd /home/dan/source/jellyfin-plugin-favorite-tags/src && \
/nix/store/7bsxdrbx302w9b6rz3i4pv1szzz8wqb8-dotnet-sdk-9.0.310/share/dotnet/dotnet build -c Release 2>&1 | tail -5 && \
sudo cp /home/dan/source/jellyfin-plugin-favorite-tags/src/bin/Release/net9.0/Jellyfin.Plugin.FavoriteTags.dll "/apps/jellyfin/config/plugins/Favorite Tags_0.1.0.0/" && \
sudo systemctl restart jellyfin && \
sleep 5 && \
echo "✅ Plugin deployed and Jellyfin restarted"
```

---

## Checking Logs

### Real-time Log Tail (Last 50 lines)
```bash
sudo tail -50 /apps/jellyfin/config/log/log_20260220.log
```

### Filter for FavoriteTags Plugin Logs
```bash
sudo tail -200 /apps/jellyfin/config/log/log_20260220.log | grep -i "favoritetags\|favorite"
```

### Check for Errors
```bash
sudo tail -200 /apps/jellyfin/config/log/log_20260220.log | grep -i "favoritetags\|error\|exception"
```

### Get Today's Full Log File
```bash
sudo cat /apps/jellyfin/config/log/log_$(date +%Y%m%d).log | tail -100
```

---

## Testing the Plugin

### Access Settings Page
1. Open Jellyfin Web UI: `http://192.168.15.1:8096`
2. Go to Dashboard → Plugins
3. Click settings icon on "Favorite Tags"
4. Configure Sonarr/Radarr URLs and API keys

### Test Connections
- From plugin settings, click "Test Sonarr Connection"
- From plugin settings, click "Test Radarr Connection"
- Check logs for success/failure messages

### Manual Sync
1. Click "Manual Sync" button
2. Check logs: `grep "Sync completed" /apps/jellyfin/config/log/log_*.log | tail -1`

### Dry Run
1. Click "Dry Run" button
2. Should see detailed output showing what WOULD change
3. Check logs for "[DRY-RUN]" messages

### Scheduled Tasks
1. Go to Dashboard → Scheduled Tasks
2. Find "Sync Favorite Tags"
3. Can click to run manually or edit schedule

---

## Troubleshooting

### Plugin Not Loading
- Check logs for load errors: `sudo grep "FavoriteTags" /apps/jellyfin/config/log/log_*.log`
- Verify DLL exists: `sudo ls -lh /apps/jellyfin/config/plugins/Favorite\ Tags_0.1.0.0/`
- Verify Jellyfin restarted: `ps aux | grep jellyfin | grep -v grep`

### Settings Page Blank
- Clear browser cache (Ctrl+Shift+Delete)
- Check for 404 errors in logs: `sudo grep "404\|Failed to get resource" /apps/jellyfin/config/log/log_*.log | tail -5`
- Verify config.html is embedded in DLL

### Settings Not Saving
- Check auth: endpoint requires `[Authorize]` attribute
- Verify you're logged into Jellyfin
- Check logs for error messages
- Try disabling/enabling plugin

### Sync Not Working
- Test connections first (see "Testing" section)
- Check Sonarr/Radarr URLs are correct (http://192.168.15.1:8989)
- Verify API keys are correct
- Check logs for "Failed to connect" messages
- Verify items exist in Sonarr/Radarr before syncing

### API Key Leaks in Logs
- Search logs: `sudo grep -i "apikey\|api_key" /apps/jellyfin/config/log/log_*.log`
- Should NOT appear (we use X-Api-Key header)
- If found, remove log files and rebuild

---

## Code Changes and Rebuilding

### If You Modify...

**Controllers (SyncController.cs):**
1. Rebuild and redeploy
2. Jellyfin may need restart to reload routing
3. Test endpoints via settings page buttons

**Services (SyncService.cs, SonarrService.cs, RadarrService.cs):**
1. Rebuild and redeploy
2. Restart Jellyfin
3. Re-test syncs

**Configuration (PluginConfiguration.cs):**
1. Rebuild
2. Redeploy
3. Restart Jellyfin
4. Configuration persists in XML, should migrate automatically

**UI (config.html):**
1. Rebuild (HTML must be re-embedded)
2. Redeploy
3. Clear browser cache
4. Restart may not be necessary, but try it if UI doesn't update

**Scheduled Task (SyncScheduledTask.cs):**
1. Rebuild and redeploy
2. Restart Jellyfin
3. Task should appear in Scheduled Tasks after restart
4. If not appearing, check logs for instantiation message

---

## Nix Shell Dotnet Location

The .NET SDK used is located at:
```
/nix/store/7bsxdrbx302w9b6rz3i4pv1szzz8wqb8-dotnet-sdk-9.0.310/share/dotnet/dotnet
```

Alternative (simpler, if nix shell is set up):
```bash
nix shell nixpkgs#dotnet-sdk_9 -- dotnet build -c Release
```

---

## Plugin Directory Structure

```
/apps/jellyfin/config/plugins/Favorite Tags_0.1.0.0/
├── Jellyfin.Plugin.FavoriteTags.dll       (The compiled plugin)
├── plugin.manifest.json                   (Version info for plugin repo)
└── meta.json                              (Runtime metadata)
```

Configuration stored at:
```
/apps/jellyfin/config/plugins/configurations/Jellyfin.Plugin.FavoriteTags.xml
```

---

## Verification Checklist After Deploy

- [ ] Build succeeded (no errors)
- [ ] DLL copied to plugin directory
- [ ] Jellyfin restarted
- [ ] Plugin appears in Dashboard → Plugins
- [ ] Settings page loads without 404 errors
- [ ] Configuration saves successfully
- [ ] Test Sonarr connection works
- [ ] Test Radarr connection works
- [ ] Manual sync completes
- [ ] Dry run shows detailed output
- [ ] Scheduled task appears in Dashboard → Scheduled Tasks
- [ ] Logs show no errors or API key leaks

---

## Quick Reference Commands

```bash
# Build only
cd src && /nix/store/7bsxdrbx302w9b6rz3i4pv1szzz8wqb8-dotnet-sdk-9.0.310/share/dotnet/dotnet build -c Release

# Deploy only
sudo cp src/bin/Release/net9.0/Jellyfin.Plugin.FavoriteTags.dll "/apps/jellyfin/config/plugins/Favorite Tags_0.1.0.0/"

# Restart only
sudo systemctl restart jellyfin && sleep 5

# Check plugin loaded
sudo grep "FavoriteTags" /apps/jellyfin/config/log/log_*.log | tail -3

# View recent errors
sudo tail -50 /apps/jellyfin/config/log/log_*.log | grep -i "error\|exception"

# Tail live logs
sudo tail -f /apps/jellyfin/config/log/log_$(date +%Y%m%d).log
```

---

## Known Issues and Workarounds

### Issue: Settings page shows after restart but config lost
**Cause:** Configuration wasn't persisted to XML file
**Fix:** Verify `UpdateConfiguration()` is being called and Jellyfin has write permission to `/apps/jellyfin/config/plugins/configurations/`

### Issue: API endpoints return 401 Unauthorized
**Cause:** Likely a Jellyfin auth token issue in browser
**Fix:**
- Log out and log back in to Jellyfin
- Clear browser cache
- Try private/incognito window

### Issue: Sync runs but no tags appear
**Cause:** Multiple possibilities - check in this order:
1. Dry run works but manual sync doesn't → Check auth logs
2. Sync says "Synced X tags" but no tags in Sonarr/Radarr → Check Sonarr/Radarr API response logs
3. Items not found in Sonarr/Radarr → Check TVDB/IMDB ID matching

---

## Performance Notes

- First sync may take a few seconds (queries all series/movies from Sonarr/Radarr)
- Subsequent syncs are faster (only checks if tag exists)
- Large libraries (1000+ items) may take 10-30 seconds per sync
- Use dry-run first to verify before running actual sync

---

## Version Updates

When bumping version:
1. Update `Directory.Build.props` (when we create it for release)
2. Update `plugin.manifest.json`
3. Rebuild
4. Test locally
5. Create GitHub release with new version tag
