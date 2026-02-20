# Troubleshooting Guide - Favorite Tags Plugin

## Quick Diagnostics

### Is the Plugin Loaded?

**Check in Jellyfin:**
1. Dashboard → Plugins
2. Look for "Favorite Tags" in the list
3. Status should show as "Active" or enabled

**If not listed:**
- Check DLL is in correct directory (see [INSTALLATION.md](INSTALLATION.md))
- Check Jellyfin logs for errors during startup
- Restart Jellyfin
- Check .NET 8.0 compatibility

### Is Sync Running?

**Check in Dashboard:**
1. Plugins → Favorite Tags
2. Status section → "Running" indicator
3. "Last Sync Time" shows recent timestamp

**If sync not running:**
- Check plugin is enabled (checkbox checked)
- Check at least one service configured
- Wait up to 1 hour for first scheduled sync
- Check logs for errors

---

## Common Issues & Solutions

### Issue: Plugin Not Appearing

#### Symptom
- Dashboard → Plugins shows no "Favorite Tags"
- After restart, still missing

#### Causes
1. DLL not in plugins directory
2. Wrong plugins directory
3. File permission issue
4. .NET version incompatibility

#### Solution

**Step 1: Verify DLL Location**
```bash
# Check file exists
ls -la /path/to/jellyfin/config/plugins/ | grep favorite

# Should show:
# jellyfin-plugin-favorite-tags.dll
```

**Step 2: Check File Permissions**
```bash
# Make readable by jellyfin user
chmod 644 /path/to/jellyfin/config/plugins/jellyfin-plugin-favorite-tags.dll

# Check ownership
ls -l /path/to/jellyfin/config/plugins/jellyfin-plugin-favorite-tags.dll
# Should show jellyfin:jellyfin or appropriate user
```

**Step 3: Check .NET Version**
```bash
dotnet --version
# Should be 8.0.x or later
```

**Step 4: Restart and Verify**
```bash
# Docker
docker restart jellyfin

# Check logs
docker logs jellyfin | grep -i "favorite\|plugin"
# Should see plugin loading messages
```

**If still not appearing:**
- Check Jellyfin logs for startup errors
- Verify DLL is not corrupted (try re-downloading)
- Check with: `docker exec jellyfin ls -la /config/plugins/`

---

### Issue: Configuration Test Fails

#### Symptom
- "Test Connection" button shows error
- Error: "Failed to connect to Sonarr/Radarr"

#### Causes
1. Service not running
2. URL incorrect
3. API key incorrect
4. Network connectivity issue
5. Firewall blocking access

#### Solution

**Step 1: Verify Service Running**
```bash
# Docker
docker ps | grep sonarr
docker ps | grep radarr

# Should show containers running
```

**Step 2: Test URL Connectivity**
```bash
# Test Sonarr
curl http://sonarr:8989/api/v3/system/status

# Test Radarr
curl http://radarr:7878/api/v3/system/status

# Should return JSON response (may show "Unauthorized" but proves URL works)
```

**Step 3: Verify API Key**
```bash
# Get API key from service
# Sonarr: Settings → General → API Key
# Radarr: Settings → General → API Key

# Make sure you copied the FULL key (no truncation)
# Make sure no extra spaces before/after
```

**Step 4: Test with API Key**
```bash
# Test Sonarr with key
curl "http://sonarr:8989/api/v3/system/status?apikey=YOUR_KEY"

# Should return JSON without "Unauthorized" error
```

**Step 5: Check Network**
```bash
# From Jellyfin container (if Docker)
docker exec jellyfin ping sonarr
docker exec jellyfin ping radarr

# Should show responses (not "host unreachable")
```

**Common URL Mistakes:**
- ✅ Correct: `http://sonarr:8989`
- ❌ Incorrect: `sonarr:8989` (missing http://)
- ❌ Incorrect: `http://sonarr` (missing port)
- ❌ Incorrect: `http://sonarr:8989/` (trailing slash is OK but not required)

---

### Issue: Sync Completes but No Tags Applied

#### Symptom
- Manual sync shows success
- History shows "1 processed, 0 tagged"
- No tags appear in Sonarr/Radarr

#### Causes
1. Items not found in Sonarr/Radarr
2. Items not favorited in Jellyfin
3. Incorrect matching
4. Permission issue

#### Solution

**Step 1: Verify Favorites Exist**
```
Jellyfin → Browse Media → Click heart icon on an item
```

**Step 2: Try Dry-Run**
```
Plugins → Favorite Tags → Dry Run button
```

Check output:
- Shows items that would be tagged
- Shows matching results
- Look for error messages

**Step 3: Check Item Names**
```
Jellyfin item name vs Sonarr/Radarr item name

Examples of matching issues:
- "Breaking Bad" (Jellyfin) vs "breaking bad" (Sonarr) - Usually OK
- "Game of Thrones" (Jellyfin) vs "Game Of Thrones" (Sonarr) - Usually OK
- "Show (2020)" (Jellyfin) vs "Show" (Sonarr) - May not match by title
```

**Step 4: Enable Debug Logging**
```
Plugins → Favorite Tags → Configuration
Advanced Settings → Log Level: Debug
Save
```

Then run sync and check logs:
```bash
docker logs jellyfin | grep -A 5 "FavoriteTags"
```

Look for:
- "Attempting to match by ID" - Good sign
- "Attempting to match by title" - Fallback
- "No item found" - Item not in Sonarr/Radarr

**Step 5: Check Sonarr/Radarr Settings**
```
Sonarr Settings → General
Radarr Settings → General

Verify services are working and items are properly indexed
```

---

### Issue: Sync Fails with Errors

#### Symptom
- Manual sync shows "Warning" or "Error"
- History shows error count > 0
- Click error count to see details

#### Causes (by error message)
- "Item not found" - Not in Sonarr/Radarr
- "Connection timeout" - Network issue
- "Invalid API key" - Wrong key
- "Service unavailable" - Service is down

#### Solution

**For "Item not found":**
- Check item exists in Sonarr/Radarr
- Check spelling matches
- Try manually searching in Sonarr/Radarr

**For "Connection timeout":**
- Check network connectivity
- Check service is running
- Increase timeout: Configuration → Advanced → API Timeout
- Try manual connection test

**For "Invalid API key":**
- Verify API key from service
- Re-enter in plugin configuration
- Test connection to confirm

**For "Service unavailable":**
- Check service status: `docker ps`
- Restart service if needed
- Check service logs for errors

---

### Issue: Sync Running Very Slowly

#### Symptom
- Manual sync takes 10+ minutes for 100 items
- History shows correct results but slow

#### Causes
1. Large library size
2. Network latency
3. Sonarr/Radarr performance
4. Default retry/timeout settings

#### Solution

**Step 1: Check Library Size**
```
Jellyfin Dashboard → Libraries
Count total favorited items
```

**Rule of thumb:**
- 100 items: ~5-10 seconds
- 500 items: ~20-30 seconds
- 1000+ items: ~1-2 minutes

**Step 2: Monitor Network**
```bash
# Check network latency
ping sonarr
ping radarr

# Should show < 10ms (same network)
# If 100ms+, network is slow
```

**Step 3: Optimize Settings**
```
Plugins → Favorite Tags → Configuration → Advanced

Lower values = faster but more aggressive:
- Max API Retries: 1 (instead of 3)
- API Timeout: 10 seconds (instead of 30)
```

**Step 4: Check Service Performance**
```
Open Sonarr/Radarr web interface
Check if responsive and not under load
```

**Step 5: Increase Sync Interval**
```
If sync takes 5 minutes, set interval to 6+ hours
to avoid overlapping syncs
```

---

### Issue: Tags Keep Getting Removed

#### Symptom
- Tag applied successfully
- After next sync, tag is gone
- Items not favorited in Jellyfin anymore

#### Causes
1. Item unfavorited in Jellyfin
2. Incorrect unfavorite behavior
3. Configuration issue

#### Solution

**Step 1: Verify Favorite Status**
```
Jellyfin → Check if item still has heart icon
If heart is gone, item was unfavorited
```

**Expected behavior:**
- Tag persists in Sonarr/Radarr even if unfavorited
- Only removed if NO users have it favorited
- To keep protection: re-favorite in Jellyfin

**Step 2: Multi-user Scenario**
```
If multiple users, any can unfavorite without removing tag
Tag only removed when ALL users unfavorite
```

---

### Issue: Configuration Not Saving

#### Symptom
- Click "Save Configuration"
- Error message appears
- Settings not saved

#### Causes
1. Invalid URL format
2. Empty required field
3. Invalid sync interval
4. Server error

#### Solution

**Check Error Message:**

**"At least one of Sonarr URL or Radarr URL must be configured"**
- Enter Sonarr URL OR Radarr URL (at least one)
- Cannot leave both empty

**"Sonarr URL is not a valid URI"**
- Format: `http://sonarr:8989`
- Must include `http://` or `https://`
- Must include port number

**"Sonarr API key is required if Sonarr URL is configured"**
- If Sonarr URL is entered, must also enter API key
- API key cannot be empty

**"Sync interval must be at least 0.25 hours"**
- Minimum value: 0.25 (15 minutes)
- Enter number like: 0.5, 1, 2, 24, etc.

**If error persists:**
1. Refresh page (Ctrl+F5)
2. Try again
3. Check browser console for JavaScript errors
4. Try different browser

---

### Issue: Dashboard Unresponsive

#### Symptom
- Dashboard loads but slow
- Buttons don't respond
- Status doesn't update

#### Causes
1. Sync running (blocking)
2. Network latency
3. Browser issue
4. API issue

#### Solution

**Step 1: Wait for Sync**
```
If "Running: Yes" appears, wait for sync to complete
Syncs typically take 1-2 minutes
```

**Step 2: Refresh Page**
```
Press Ctrl+F5 (hard refresh)
Or use browser's reload button
```

**Step 3: Check Network**
```bash
# From Jellyfin container
docker exec jellyfin curl -I http://localhost:6789/

# Should respond with 200 OK
```

**Step 4: Check Browser**
```
Try different browser
Check browser console for errors (F12)
```

**Step 5: Check Logs**
```bash
docker logs jellyfin | tail -20

Look for errors near sync times
```

---

## Debug Mode

### Enable Debug Logging

1. **In Dashboard:**
   - Plugins → Favorite Tags → Configuration
   - Advanced Settings → Log Level: **Debug**
   - Click "Save Configuration"

2. **Understand Debug Output:**
   - More verbose logging
   - Shows matching attempts
   - Shows API requests (without keys)
   - Helpful for troubleshooting

3. **Disable Debug After Use:**
   - Log Level: **Info** (default)
   - Performance will improve

### Collect Logs for Support

```bash
# Docker
docker logs jellyfin > jellyfin.log 2>&1

# Get last 100 lines
docker logs jellyfin | tail -100

# Filter for plugin logs
docker logs jellyfin | grep -i "favorite"
```

When reporting issues, include:
- Last 50 lines of logs (around error time)
- Configuration summary (without API keys)
- Jellyfin version
- Sonarr/Radarr versions
- Steps to reproduce

---

## Frequently Asked Questions

### Q: Can I use only Sonarr without Radarr?
**A:** Yes! Leave Radarr URL blank. Only series will be synced.

### Q: Can I use only Radarr without Sonarr?
**A:** Yes! Leave Sonarr URL blank. Only movies will be synced.

### Q: How often does sync run?
**A:** By default, every 1 hour. Adjust in Configuration → Sync Interval.

### Q: Can I run sync on demand?
**A:** Yes! Click "Manual Sync" button in Sync Actions section.

### Q: What if I unfavorite an item?
**A:** Tag is removed from Sonarr/Radarr when NO users have it favorited.

### Q: Can multiple Jellyfin users have different favorites?
**A:** Yes! Each user can favorite independently. Tag applied if ANY user favorited.

### Q: Does plugin modify my media files?
**A:** No! Only applies tags in Sonarr/Radarr. Media files unchanged.

### Q: Can I undo/remove tags manually?
**A:** Yes! Go to Sonarr/Radarr and remove tag from item. Plugin won't re-apply if item unfavorited.

### Q: What if Sonarr/Radarr is down during sync?
**A:** Sync fails gracefully. Will retry on next scheduled sync. Check error logs.

### Q: Does plugin use a lot of CPU/memory?
**A:** Minimal impact. Only active during sync (1-2 minutes per hour). Uses ~50-100 MB.

### Q: Can I change the tag name?
**A:** Yes! Configuration → Tag Name. Will apply new tag on next sync.

### Q: Is data encrypted?
**A:** Yes! API keys encrypted at rest using Jellyfin's encryption.

---

## Getting Help

### Before Reporting Issues

1. **Check This Guide** - You're already here!
2. **Enable Debug Logging** - Turn on Debug level
3. **Collect Logs** - Get relevant log lines
4. **Verify Setup** - Confirm URL and API keys work

### Report Issues On GitHub

Include:
- [ ] Jellyfin version
- [ ] Sonarr/Radarr versions
- [ ] Plugin version (v0.1.0)
- [ ] OS/Platform (Docker, Linux, Windows)
- [ ] Error message (exact text)
- [ ] Steps to reproduce
- [ ] Relevant logs (without API keys)
- [ ] Configuration details (without sensitive data)

### Contact & Support

- **Documentation:** Read relevant guide files
- **Issues:** GitHub Issues tracker
- **Discussions:** GitHub Discussions

---

## Related Documentation

- [INSTALLATION.md](INSTALLATION.md) - How to install
- [DASHBOARD_GUIDE.md](DASHBOARD_GUIDE.md) - How to use
- [API_ENDPOINTS.md](API_ENDPOINTS.md) - Technical details
- [DEVELOPER_GUIDE.md](DEVELOPER_GUIDE.md) - For developers

Good luck! 🎬

