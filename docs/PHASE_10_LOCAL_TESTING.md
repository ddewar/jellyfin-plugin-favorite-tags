# Phase 10: Local Testing Against Real Instances

Testing against your existing Jellyfin, Sonarr, and Radarr instances.

## Quick Start

### Service Details (from your nixos config)

```
Jellyfin:  http://10.89.0.1:8096
Sonarr:    http://192.168.15.1:8989
Radarr:    http://192.168.15.1:7878
```

Get API keys from:
- **Sonarr:** Settings → General → API Key
- **Radarr:** Settings → General → API Key

---

## Step 1: Build and Deploy Plugin

```bash
cd /home/dan/source/jellyfin-plugin-favorite-tags

# Build release DLL
dotnet build -c Release

# Locate Jellyfin plugins directory on your system
# Typically: /var/lib/jellyfin/plugins or in your nixos config

# Copy the DLL
cp src/bin/Release/net8.0/jellyfin-plugin-favorite-tags.dll \
   /path/to/jellyfin/plugins/

# Restart Jellyfin (depends on your setup)
# If using systemd: sudo systemctl restart jellyfin
# If using docker: docker restart jellyfin
# If using nixos: sudo systemctl restart jellyfin
```

### Verify Plugin Loads

1. Go to Dashboard → Plugins
2. Look for "Favorite Tags" in list
3. Check version (v0.1.0)
4. Should show enabled/active

---

## Step 2: Configure Plugin

1. **Access Configuration**
   - Click "Favorite Tags" plugin

2. **Enter Sonarr Details**
   - URL: `http://192.168.15.1:8989`
   - API Key: (paste from Sonarr settings)
   - Click "Test Connection"

3. **Enter Radarr Details**
   - URL: `http://192.168.15.1:7878`
   - API Key: (paste from Radarr settings)
   - Click "Test Connection"

4. **Set Sync Options**
   - Tag Name: `jellyfin-favorited`
   - Sync Interval: `1` (hourly)
   - Enable Plugin: ✓ (checked)

5. **Save Configuration**
   - Click "Save Configuration"
   - Should show: ✅ "Configuration saved successfully!"

---

## Step 3: Test Basic Sync

### Test 1: Add Favorites and Sync

1. **In Jellyfin:**
   - Browse your existing media libraries
   - Click heart icon on 3-5 items (mix of shows and movies)
   - Note which items you favorited

2. **Trigger Sync:**
   - Go to Plugins → Favorite Tags
   - Click "Manual Sync" button
   - Wait for completion (usually 5-30 seconds with real data)

3. **Verify Results:**
   - Check Sonarr: Browse Series, look for `jellyfin-favorited` tag on favorited shows
   - Check Radarr: Browse Movies, look for `jellyfin-favorited` tag on favorited movies
   - Dashboard → History should show sync results

**Test Result:** ✅ Pass / ❌ Fail

---

### Test 2: Dry-Run Mode

1. **Go to Dashboard**
2. Click "Dry Run" button
3. Check output:
   - Should show items that WOULD be tagged
   - Should NOT actually apply tags to Sonarr/Radarr

4. **Verify in Sonarr/Radarr:**
   - No new tags actually appeared
   - Only the tags from Test 1 should exist

**Test Result:** ✅ Pass / ❌ Fail

---

### Test 3: Idempotency

1. **Run Manual Sync again**
2. Check results:
   - Should match previous sync
   - Same number of items
   - No duplicates
   - No new errors

3. **Run 2-3 more times:**
   - Results should be identical each time
   - No side effects from repeated runs

**Test Result:** ✅ Pass / ❌ Fail

---

## Step 4: Test Multi-User Scenarios (if applicable)

If you have multiple Jellyfin users:

1. **Create different favorites for each user:**
   - User A: favorite shows A, B, C
   - User B: favorite shows B, C, D
   - User C: favorite movies X, Y, Z

2. **Run Manual Sync**

3. **Verify aggregation:**
   - All shows A, B, C, D should be tagged
   - All movies X, Y, Z should be tagged
   - Each item tagged once (no duplicates)

4. **Test unfavorite:**
   - User A unfavorites show B
   - User B still has B and C
   - Manual Sync
   - Show B should still be tagged (User B has it)

5. **Complete unfavorite:**
   - User B unfavorites B and C
   - Manual Sync
   - Shows B and C should lose tags (no users have them)

**Test Result:** ✅ Pass / ❌ Fail

---

## Step 5: Test Error Handling

### Test 5.1: Invalid API Key

1. **Go to Configuration**
2. Change Sonarr API Key to: `invalid-key-12345`
3. Click "Test Sonarr Connection"
4. Expected: ❌ Error (not connection error)
5. Restore correct API key
6. Test connection passes ✅

**Test Result:** ✅ Pass / ❌ Fail

---

### Test 5.2: Service Unavailable

If you can temporarily stop Sonarr:

1. Stop Sonarr
2. Try Manual Sync
3. Expected: Sync continues for Radarr, fails for Sonarr
4. Restart Sonarr
5. Sync works again

(If you can't easily stop it, skip this test)

**Test Result:** ✅ Pass / ❌ Fail / ⊘ Skipped

---

### Test 5.3: Item Not Found

1. **In Jellyfin, favorite an obscure/rare item** (something unlikely in Sonarr/Radarr)
2. **Manual Sync**
3. **Check history:** Should show item attempted but not found
4. **Check logs:** Should show graceful error handling
5. **Other items still synced successfully**

**Test Result:** ✅ Pass / ❌ Fail

---

## Step 6: Integration with Jellysweep

This is the main reason you built this plugin!

### Verify Jellysweep Integration

1. **Check Jellysweep config:**
   - Confirm it excludes `jellyfin-favorited` tag (it should already)
   - Lines in jellysweep.nix: 111 & 121

2. **Create test scenario:**
   - Favorite an item you DON'T want cleaned up
   - Run plugin sync
   - Verify tag applied in Sonarr/Radarr

3. **Run Jellysweep cleanup:**
   - Trigger cleanup or wait for scheduled time
   - Verify favorited items are NOT deleted
   - Non-favorited items are cleaned normally

4. **Test unfavorite:**
   - Unfavorite an item
   - Run plugin sync (tag removed)
   - Run Jellysweep again
   - Previously-favorited item can now be cleaned

**Test Result:** ✅ Pass / ❌ Fail

---

## Step 7: Performance Check

1. **Count your actual items:**
   - How many shows in Sonarr? _____
   - How many movies in Radarr? _____
   - How many Jellyfin users? _____
   - How many items do you typically favorite? _____

2. **Note sync time:**
   - Click Manual Sync, note start and end time
   - Sync duration: _____ seconds
   - Expected: Should be quick (under 30 seconds for typical library)

3. **Repeat a few times:**
   - Note if performance is consistent
   - Memory/CPU usage reasonable? ✅ / ❌

**Test Result:** ✅ Pass / ❌ Fail

---

## Step 8: Dashboard UI Check

1. **Configuration section:**
   - [ ] All fields visible and editable
   - [ ] Test buttons work
   - [ ] Settings save properly

2. **Status section:**
   - [ ] Shows "Running: Yes/No"
   - [ ] Shows last sync time
   - [ ] Shows next scheduled sync

3. **History section:**
   - [ ] Shows list of syncs
   - [ ] Results visible (items processed, tags applied/removed)
   - [ ] Color coding (green=success, etc.)

4. **Statistics section:**
   - [ ] Shows reasonable numbers
   - [ ] Updates after syncs
   - [ ] Makes sense given your data

**Test Result:** ✅ Pass / ❌ Fail

---

## Summary

| Test | Result | Notes |
|------|--------|-------|
| Build & Deploy | ✅ / ❌ | |
| Configuration | ✅ / ❌ | |
| Basic Sync | ✅ / ❌ | |
| Dry-Run | ✅ / ❌ | |
| Idempotency | ✅ / ❌ | |
| Multi-User | ✅ / ❌ | ⊘ N/A |
| Error Handling | ✅ / ❌ | |
| Jellysweep Integration | ✅ / ❌ | |
| Performance | ✅ / ❌ | |
| Dashboard UI | ✅ / ❌ | |

---

## Issues Found

### Critical Issues (Block Release)

_None found so far_

### Major Issues

_None found so far_

### Minor Issues

_None found so far_

---

## Notes & Observations

```
[Add notes here as you test]
```

---

## Next Steps After Testing

- [ ] Fix any issues found
- [ ] Run automated test suite to verify no regressions
- [ ] Final documentation review
- [ ] Prepare for GitHub release
- [ ] Create release notes
- [ ] Announce to community

---

## Useful Commands

```bash
# Check Jellyfin logs (if systemd)
journalctl -u jellyfin -f | grep -i favorite

# Check Jellyfin logs (if docker)
docker logs jellyfin | grep -i favorite

# Restart Jellyfin
sudo systemctl restart jellyfin

# View plugin logs
journalctl -u jellyfin -f
```

---

**Testing Date:** ____________
**Results:** ✅ All Pass / ⚠️ Issues Found / ❌ Blocking Issues
