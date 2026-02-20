# Phase 10: Local Testing & Refinement Checklist

## Overview

This document provides a comprehensive testing plan for Phase 10 local testing. Work through each section systematically to verify all plugin functionality before community release.

**Estimated Scope:**
- Environment setup: 30 minutes
- Basic functionality: 1-2 hours
- Multi-user testing: 1 hour
- Edge cases & error handling: 1-2 hours
- Performance testing: 30 minutes
- Total: 4-6 hours active testing

---

## Part 1: Environment Setup

### 1.1 Start Docker Environment

```bash
cd /home/dan/source/jellyfin-plugin-favorite-tags

# Build and start containers
docker-compose up -d

# Verify all containers running
docker-compose ps
# Should show: jellyfin-test, sonarr-test, radarr-test all in "Up" state
```

### 1.2 Initial Service Health Checks

- [ ] **Jellyfin** (http://localhost:8096)
  - [ ] Loads without errors
  - [ ] Can log in (default credentials)
  - [ ] Plugins section visible
  - [ ] Dashboard → Plugins shows empty list initially
  - [ ] Create test library with 5-10 media items

- [ ] **Sonarr** (http://localhost:8989)
  - [ ] Loads without errors
  - [ ] Settings → General → Copy API Key
  - [ ] Add 3-5 test TV series with TVDB IDs:
    - Breaking Bad (TVDB: 81189)
    - Game of Thrones (TVDB: 121361)
    - The Office (TVDB: 6594)

- [ ] **Radarr** (http://localhost:7878)
  - [ ] Loads without errors
  - [ ] Settings → General → Copy API Key
  - [ ] Add 3-5 test movies with IMDB IDs:
    - The Matrix (IMDB: tt0133093)
    - Inception (IMDB: tt1375666)
    - The Shawshank Redemption (IMDB: tt0111161)

### 1.3 Build and Deploy Plugin

```bash
# Build release DLL
dotnet build -c Release

# Copy to Jellyfin plugins directory (docker container)
docker cp src/bin/Release/net8.0/jellyfin-plugin-favorite-tags.dll \
  jellyfin-test:/config/plugins/

# Restart Jellyfin
docker restart jellyfin-test

# Wait 30 seconds and check logs
docker logs jellyfin-test | grep -i "favorite\|plugin" | tail -20
```

### 1.4 Verify Plugin Load

- [ ] Dashboard → Plugins
- [ ] "Favorite Tags" appears in list
- [ ] Version shows v0.1.0
- [ ] Status shows enabled/active
- [ ] No error messages in logs

---

## Part 2: Basic Functionality Testing

### 2.1 Configuration Test

**Objective:** Verify configuration UI and validation works

- [ ] **Access Configuration Page**
  - Click "Favorite Tags" plugin in dashboard
  - Configuration section visible
  - Form fields all rendered correctly

- [ ] **Configure Sonarr**
  - Enter Sonarr URL: `http://sonarr:8989`
  - Enter Sonarr API Key: (paste from above)
  - Click "Test Sonarr Connection"
  - Expected: ✅ "Successfully connected to Sonarr"
  - [ ] Test passes

- [ ] **Configure Radarr**
  - Enter Radarr URL: `http://radarr:7878`
  - Enter Radarr API Key: (paste from above)
  - Click "Test Radarr Connection"
  - Expected: ✅ "Successfully connected to Radarr"
  - [ ] Test passes

- [ ] **Set Sync Options**
  - Tag Name: `jellyfin-favorited` (default)
  - Sync Interval: `1` (1 hour)
  - Enable Plugin: ✓ (checked)

- [ ] **Validate Configuration**
  - Leave Sonarr URL empty
  - Try to save
  - Expected: ❌ "At least one of Sonarr URL or Radarr URL must be configured"
  - [ ] Validation works
  - Restore Sonarr URL

- [ ] **Save Configuration**
  - Click "Save Configuration"
  - Expected: ✅ "Configuration saved successfully!"
  - [ ] Configuration persists (refresh page, settings still there)

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 2.2 Single-User Series Sync Test

**Objective:** Verify sync works for TV series with single user

**Setup:**
- In Jellyfin, add 2-3 series to favorites (click heart icon)
  - Breaking Bad
  - Game of Thrones
  - The Office (only add 2 initially)
- At least one series should already exist in Sonarr

**Test:**
- [ ] Jellyfin shows favorites (heart icons visible)
- [ ] Dashboard → Favorite Tags
- [ ] Click "Manual Sync" button
- [ ] Status shows "Running: Yes"
- [ ] Wait for completion (1-2 minutes)
- [ ] Status shows "Running: No"
- [ ] History shows new sync entry with status "Success"
- [ ] Open Sonarr, browse series
- [ ] Verify "jellyfin-favorited" tag applied to:
  - [ ] Breaking Bad
  - [ ] Game of Thrones
- [ ] The Office does NOT have tag (not in Sonarr)

**Expected Results:**
- 2 items synced successfully
- 1 item not found (expected)
- Tags visible in Sonarr

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 2.3 Single-User Movie Sync Test

**Objective:** Verify sync works for movies with single user

**Setup:**
- In Jellyfin, add 2-3 movies to favorites
  - The Matrix
  - Inception
  - The Shawshank Redemption (only add 2 initially)
- At least one movie should already exist in Radarr

**Test:**
- [ ] Jellyfin shows favorites (heart icons visible)
- [ ] Dashboard → Favorite Tags
- [ ] Click "Manual Sync" button
- [ ] Wait for completion
- [ ] History shows new sync entry
- [ ] Open Radarr, browse movies
- [ ] Verify "jellyfin-favorited" tag applied to:
  - [ ] The Matrix
  - [ ] Inception
- [ ] The Shawshank Redemption does NOT have tag (not in Radarr)

**Expected Results:**
- 2 items synced successfully
- 1 item not found (expected)
- Tags visible in Radarr

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 2.4 Mixed Series & Movies Sync Test

**Objective:** Verify sync works for both series and movies together

**Setup:**
- Keep existing favorites from tests 2.2 and 2.3
- Should have ~4-5 items favorited (2-3 series, 2-3 movies)

**Test:**
- [ ] Dashboard → Favorite Tags
- [ ] Click "Manual Sync" button
- [ ] Wait for completion
- [ ] History shows combined count:
  - [ ] "4 items processed" (or appropriate count)
  - [ ] "4 tags applied" (both services)
- [ ] Sonarr shows tags on series
- [ ] Radarr shows tags on movies

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 2.5 Idempotency Test

**Objective:** Verify sync is idempotent (safe to run multiple times)

**Setup:**
- Keep existing favorites and tags from previous tests

**Test:**
- [ ] Dashboard → Favorite Tags
- [ ] Click "Manual Sync" button
- [ ] Wait for completion
- [ ] Note results (should show same items)
- [ ] Click "Manual Sync" again immediately
- [ ] Wait for completion
- [ ] Results should be identical:
  - [ ] Same items processed
  - [ ] Same tags applied
  - [ ] No duplicate tags
  - [ ] No errors
- [ ] Run 3 times total to verify consistency

**Expected Results:**
- Multiple syncs produce identical results
- No side effects from repeated runs
- Tags don't duplicate

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 2.6 Dry-Run Mode Test

**Objective:** Verify dry-run shows what would happen without making changes

**Setup:**
- Existing favorites and tags from previous tests
- Note current tag state

**Test:**
- [ ] Dashboard → Favorite Tags
- [ ] Click "Dry Run" button
- [ ] Wait for completion
- [ ] Output shows what WOULD happen:
  - [ ] Items that would be tagged
  - [ ] Matching results
  - [ ] No actual tags applied
- [ ] Verify in Sonarr/Radarr:
  - [ ] No new tags appeared
  - [ ] Existing tags unchanged

**Expected Results:**
- Dry-run shows accurate preview
- No actual changes made to Sonarr/Radarr
- Output matches what would actually sync

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

## Part 3: Multi-User Scenario Testing

### 3.1 Multi-User Favorite Aggregation

**Objective:** Verify multi-user favorites are aggregated correctly

**Setup:**
- Create 2-3 additional Jellyfin users (besides admin)
- User A: Favorite Breaking Bad + Game of Thrones
- User B: Favorite Game of Thrones + The Office + The Matrix
- User C: Favorite Inception

**Test:**
- [ ] Clear existing sync history (History → Clear button)
- [ ] Manual Sync
- [ ] Wait for completion
- [ ] History shows combined results:
  - [ ] 5 items processed (BBad, GoT, Office, Matrix, Inception)
- [ ] Verify tags in Sonarr:
  - [ ] Breaking Bad tagged (User A)
  - [ ] Game of Thrones tagged (Users A + B)
  - [ ] The Office tagged (User B)
- [ ] Verify tags in Radarr:
  - [ ] The Matrix tagged (User B)
  - [ ] Inception tagged (User C)

**Expected Results:**
- All user favorites combined
- Each item tagged once (no duplicates)
- Multi-user protection works

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 3.2 Unfavorite Behavior (Multi-User)

**Objective:** Verify tags persist when multi-user has mixed favorite state

**Setup:**
- From previous test: Game of Thrones favorited by Users A and B
- Breaking Bad favorited only by User A

**Test:**
- [ ] User A unfavorites Breaking Bad
- [ ] User A unfavorites Game of Thrones
- [ ] Manual Sync
- [ ] Expected results:
  - [ ] Breaking Bad tag REMOVED (no users have it)
  - [ ] Game of Thrones tag PERSISTS (User B still has it)
- [ ] Verify in Sonarr:
  - [ ] Breaking Bad: No tag
  - [ ] Game of Thrones: Still tagged

**Expected Results:**
- Tags removed only when NO users favor item
- Tags persist if ANY user still has item favorited

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 3.3 Complete Unfavorite

**Objective:** Verify tags removed when all users unfavorite

**Setup:**
- From previous test: Game of Thrones still favorited by User B
- Inception favorited by User C

**Test:**
- [ ] User B unfavorites Game of Thrones (User A already did)
- [ ] User C unfavorites Inception
- [ ] Manual Sync
- [ ] Expected results:
  - [ ] Game of Thrones tag REMOVED
  - [ ] Inception tag REMOVED
- [ ] Verify in Sonarr/Radarr:
  - [ ] No tags on these items

**Expected Results:**
- All tags removed when all users unfavorite
- Plugin cleans up properly

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

## Part 4: Error Handling & Edge Cases

### 4.1 Connection Failure Handling

**Objective:** Verify plugin handles service unavailability gracefully

**Test:**
- [ ] Stop Sonarr container:
  ```bash
  docker stop sonarr-test
  ```
- [ ] Manual Sync in Jellyfin
- [ ] Wait for completion
- [ ] Expected: ⚠️ Warning or error in history
- [ ] Radarr movies still tagged (Sonarr skipped)
- [ ] Check logs for error handling:
  ```bash
  docker logs jellyfin-test | grep -i "sonarr\|error"
  ```
- [ ] Restart Sonarr:
  ```bash
  docker start sonarr-test
  ```

**Expected Results:**
- Sync continues despite one service down
- Partial success with error reporting
- No crash or unhandled exception

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 4.2 Invalid API Key Handling

**Objective:** Verify plugin handles auth failures gracefully

**Test:**
- [ ] Dashboard → Configuration
- [ ] Change Sonarr API Key to: `invalid-key-12345`
- [ ] Click "Test Sonarr Connection"
- [ ] Expected: ❌ Error message (not connection error)
- [ ] Change back to correct key
- [ ] Test connection succeeds

**Expected Results:**
- Clear error message for auth failure
- No crash
- Recoverable with correct key

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 4.3 Item Not Found Handling

**Objective:** Verify plugin handles items not in target services

**Test:**
- [ ] In Jellyfin, favorite an obscure/fake series
  - "FakeSeriesXYZ123" (won't exist in Sonarr)
- [ ] Manual Sync
- [ ] Expected results:
  - [ ] History shows item attempted
  - [ ] Item not found error logged
  - [ ] No crash
  - [ ] Other items still synced
- [ ] Check logs:
  ```bash
  docker logs jellyfin-test | grep -i "not found\|failed to find"
  ```

**Expected Results:**
- Graceful handling of missing items
- Sync continues
- Error logged but not fatal

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 4.4 Title-Based Fallback Matching

**Objective:** Verify plugin falls back to title matching when IDs unavailable

**Setup:**
- Add an item to Jellyfin without external ID (if possible)
- Or manually edit to remove TVDB ID

**Test:**
- [ ] Favorite the item (without ID)
- [ ] Manual Sync
- [ ] Check logs for:
  ```bash
  docker logs jellyfin-test | grep -i "title\|fallback\|matching"
  ```
- [ ] If item exists in Sonarr/Radarr by title:
  - [ ] Tag should be applied
  - [ ] Log should show title-based match

**Expected Results:**
- Title matching works as fallback
- Item successfully tagged
- Appropriate logging

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 4.5 Concurrent Sync Prevention

**Objective:** Verify plugin prevents overlapping syncs

**Test:**
- [ ] Start Manual Sync (see "Running: Yes")
- [ ] Quickly click Manual Sync again before first completes
- [ ] Expected: ⚠️ "Sync already running" or similar message
- [ ] Wait for first sync to complete
- [ ] Second sync does NOT start
- [ ] Confirm only one set of results in history

**Expected Results:**
- Concurrent syncs prevented
- Clear feedback to user
- No data corruption from overlap

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 4.6 Disabled Plugin Behavior

**Objective:** Verify plugin respects enable/disable toggle

**Test:**
- [ ] Dashboard → Configuration
- [ ] Uncheck "Enable Plugin" checkbox
- [ ] Save Configuration
- [ ] Manual Sync button (try to click)
  - [ ] Button disabled or error shown
- [ ] Re-enable plugin
- [ ] Manual Sync works again

**Expected Results:**
- Plugin respects enable toggle
- No operations when disabled
- Recovers properly when re-enabled

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

## Part 5: Performance Testing

### 5.1 Baseline Performance (Normal Load)

**Objective:** Measure sync performance with typical data volume

**Setup:**
- Add 20-30 series to Sonarr
- Add 20-30 movies to Radarr
- Favorite 10-15 items in Jellyfin (mix of series/movies)

**Test:**
- [ ] Note current time
- [ ] Manual Sync
- [ ] Note completion time
- [ ] Calculate sync duration: _____ seconds
- [ ] Check history for results
- [ ] Expected time: 5-15 seconds

**Benchmark:**
- Items favorited: _____
- Sync duration: _____ seconds
- Items/second: _____ (calculate)
- CPU usage during sync: _____ %
- Memory usage: _____ MB

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 5.2 Large Library Performance

**Objective:** Verify performance doesn't degrade significantly with larger library

**Setup:**
- Add 50-100 series to Sonarr
- Add 50-100 movies to Radarr
- Favorite 30-50 items total

**Test:**
- [ ] Note current time
- [ ] Manual Sync
- [ ] Note completion time
- [ ] Calculate sync duration: _____ seconds
- [ ] Expected time: 20-60 seconds (should still be reasonable)

**Benchmark:**
- Items favorited: _____
- Sync duration: _____ seconds
- Items/second: _____ (calculate)
- Compare to baseline: _____ % slower

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 5.3 Memory Leak Detection

**Objective:** Verify no memory leaks over multiple syncs

**Test:**
- [ ] Check initial memory:
  ```bash
  docker stats jellyfin-test --no-stream | grep jellyfin-test
  ```
- [ ] Run 5 syncs (manual, back-to-back)
- [ ] Check final memory:
  ```bash
  docker stats jellyfin-test --no-stream | grep jellyfin-test
  ```
- [ ] Memory should remain relatively stable
- [ ] If memory consistently grows, possible leak

**Memory Check:**
- Initial: _____ MB
- After 5 syncs: _____ MB
- Growth: _____ MB (should be <50 MB)

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

## Part 6: Dashboard & UI Testing

### 6.1 Status Display Updates

**Objective:** Verify dashboard displays real-time status correctly

**Test:**
- [ ] Open Dashboard in two browser windows side-by-side
- [ ] Window A: Configuration tab
- [ ] Window B: Status tab
- [ ] Click Manual Sync in Window A
- [ ] Window B should update in real-time:
  - [ ] "Running: Yes" appears
  - [ ] "Last Sync Time" shows current time
  - [ ] Progress indicator shows activity
- [ ] Wait for completion
- [ ] Window B updates:
  - [ ] "Running: No"
  - [ ] Status shows "Success"

**Expected Results:**
- Real-time status updates
- Dashboard responsive
- Accurate information display

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 6.2 Statistics Display

**Objective:** Verify statistics calculated and displayed correctly

**Test:**
- [ ] Dashboard → Statistics section
- [ ] After sync, should display:
  - [ ] Total Syncs: (count of all historical syncs)
  - [ ] Successful: (count of successful syncs)
  - [ ] Failed: (count of failed syncs)
  - [ ] Tags Applied: (cumulative count)
  - [ ] Tags Removed: (cumulative count)
  - [ ] Items Processed: (cumulative count)
- [ ] Numbers should be reasonable and increase after new syncs

**Expected Results:**
- Accurate statistics calculation
- Real-time updates
- Meaningful metrics displayed

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 6.3 History Table Display

**Objective:** Verify sync history displays correctly

**Test:**
- [ ] Dashboard → History section
- [ ] Should show list of recent syncs:
  - [ ] Timestamp of each sync
  - [ ] Status (Success/Warning/Error)
  - [ ] Items processed
  - [ ] Tags applied/removed counts
- [ ] Rows colored appropriately:
  - [ ] Green for success
  - [ ] Yellow for warning
  - [ ] Red for error
- [ ] Pagination if more than 10 syncs
- [ ] Can sort by clicking headers

**Expected Results:**
- Complete history visible
- Color coding works
- All relevant data displayed

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 6.4 Mobile Responsiveness

**Objective:** Verify dashboard works on mobile/tablet

**Test:**
- [ ] Open Dashboard on mobile device (or use browser developer tools)
- [ ] All sections visible without horizontal scroll
- [ ] Buttons clickable and appropriately sized
- [ ] Forms usable on small screen
- [ ] Status updates work
- [ ] Sync buttons functional

**Test on:**
- [ ] Mobile (portrait): _____ resolution
- [ ] Tablet (landscape): _____ resolution

**Expected Results:**
- Responsive design works
- Mobile-friendly layout
- All functions accessible

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 6.5 Theme & UI Consistency

**Objective:** Verify UI appearance and consistency

**Test:**
- [ ] Dark theme applied throughout
- [ ] Colors consistent:
  - [ ] Status indicators (green, yellow, red)
  - [ ] Form fields
  - [ ] Buttons
  - [ ] Borders and dividers
- [ ] Font sizes readable
- [ ] No UI elements overlapping or broken
- [ ] Modals display correctly
- [ ] Animations smooth (if any)

**Expected Results:**
- Professional appearance
- Consistent throughout
- Accessible and readable

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

## Part 7: Documentation Verification

### 7.1 Installation Guide Accuracy

**Objective:** Verify installation documentation matches reality

**Test:**
- [ ] Follow INSTALLATION.md from scratch (mentally or on test system)
- [ ] All paths and commands accurate
- [ ] Screenshots/examples match current version
- [ ] Docker Compose example works as-is
- [ ] All prerequisites listed and accurate

**Issues Found:**
- _____

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 7.2 Dashboard Guide Accuracy

**Objective:** Verify user guide matches actual dashboard

**Test:**
- [ ] Follow DASHBOARD_GUIDE.md steps
- [ ] All UI elements described actually exist
- [ ] Button names and locations correct
- [ ] Field descriptions accurate
- [ ] All configuration options accessible

**Issues Found:**
- _____

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

### 7.3 Troubleshooting Guide Helpfulness

**Objective:** Verify troubleshooting steps actually resolve issues

**Test:**
- [ ] Try intentional error (invalid API key)
- [ ] Look up in TROUBLESHOOTING.md
- [ ] Follow provided solution
- [ ] Does it work?
- [ ] Repeat with 2-3 different scenarios

**Test Scenarios:**
- [ ] Connection fails → Solution works: ✅ / ❌
- [ ] Plugin not loading → Solution works: ✅ / ❌
- [ ] Sync completes but no tags → Solution works: ✅ / ❌

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

## Part 8: Browser Compatibility

### 8.1 Test Multiple Browsers

**Objective:** Verify dashboard works in common browsers

**Browsers to Test:**
- [ ] Chrome/Chromium (latest)
- [ ] Firefox (latest)
- [ ] Safari (if on Mac)
- [ ] Edge (if on Windows)

**Per Browser:**
- [ ] Dashboard loads
- [ ] All buttons clickable
- [ ] Forms submit correctly
- [ ] Modals display properly
- [ ] Status updates work
- [ ] No console errors (F12)

**Browser Results:**

| Browser | Loads | Functional | No Errors |
|---------|-------|------------|-----------|
| Chrome | ✅ / ❌ | ✅ / ❌ | ✅ / ❌ |
| Firefox | ✅ / ❌ | ✅ / ❌ | ✅ / ❌ |
| Safari | ✅ / ❌ | ✅ / ❌ | ✅ / ❌ |
| Edge | ✅ / ❌ | ✅ / ❌ | ✅ / ❌ |

**Test Result:** ✅ Pass / ❌ Fail
**Notes:**

---

## Part 9: Summary & Issues Log

### 9.1 Overall Test Results

| Phase | Test | Result | Notes |
|-------|------|--------|-------|
| 1 | Environment Setup | ✅ / ❌ | |
| 2.1 | Configuration | ✅ / ❌ | |
| 2.2 | Series Sync | ✅ / ❌ | |
| 2.3 | Movie Sync | ✅ / ❌ | |
| 2.4 | Mixed Sync | ✅ / ❌ | |
| 2.5 | Idempotency | ✅ / ❌ | |
| 2.6 | Dry-Run | ✅ / ❌ | |
| 3.1 | Multi-User Agg. | ✅ / ❌ | |
| 3.2 | Unfavorite (Multi) | ✅ / ❌ | |
| 3.3 | Complete Unfavorite | ✅ / ❌ | |
| 4.1 | Connection Failure | ✅ / ❌ | |
| 4.2 | Invalid API Key | ✅ / ❌ | |
| 4.3 | Item Not Found | ✅ / ❌ | |
| 4.4 | Title Fallback | ✅ / ❌ | |
| 4.5 | Concurrent Prevention | ✅ / ❌ | |
| 4.6 | Disabled Plugin | ✅ / ❌ | |
| 5.1 | Baseline Perf | ✅ / ❌ | |
| 5.2 | Large Library Perf | ✅ / ❌ | |
| 5.3 | Memory Leak | ✅ / ❌ | |
| 6.1 | Status Updates | ✅ / ❌ | |
| 6.2 | Statistics | ✅ / ❌ | |
| 6.3 | History Table | ✅ / ❌ | |
| 6.4 | Mobile Responsive | ✅ / ❌ | |
| 6.5 | UI Consistency | ✅ / ❌ | |
| 7.1 | Install Guide | ✅ / ❌ | |
| 7.2 | Dashboard Guide | ✅ / ❌ | |
| 7.3 | Troubleshooting | ✅ / ❌ | |
| 8.1 | Browser Compat | ✅ / ❌ | |

---

### 9.2 Issues Found

#### Critical Issues (Block Release)
```
[ ] Issue: _______________________________________________
    Description: ___________________________________________
    Reproduction: __________________________________________
    Impact: _________________________________________________
    Fix: ___________________________________________________
```

#### Major Issues (Should Fix)
```
[ ] Issue: _______________________________________________
    Description: ___________________________________________
    Reproduction: __________________________________________
    Impact: _________________________________________________
    Fix: ___________________________________________________
```

#### Minor Issues (Nice to Have)
```
[ ] Issue: _______________________________________________
    Description: ___________________________________________
    Reproduction: __________________________________________
    Impact: _________________________________________________
    Fix: ___________________________________________________
```

---

### 9.3 Test Summary

**Tests Passed:** _____ / 28
**Tests Failed:** _____ / 28
**Critical Issues:** _____
**Major Issues:** _____
**Minor Issues:** _____

**Ready for Release:** ✅ / ❌

**If not ready, block release until:**
1. _____________________________________________________
2. _____________________________________________________
3. _____________________________________________________

---

### 9.4 Next Steps After Testing

- [ ] Fix critical issues
- [ ] Address major issues
- [ ] Document any workarounds needed
- [ ] Update documentation with findings
- [ ] Run full test suite again (regression test)
- [ ] Final security review
- [ ] Prepare release notes
- [ ] Build final release DLL
- [ ] Create GitHub release
- [ ] Announce to community

---

## Appendix: Useful Docker Commands

```bash
# View all container logs
docker-compose logs -f

# View Jellyfin logs only
docker-compose logs -f jellyfin

# Filter for plugin logs
docker logs jellyfin-test | grep -i "favorite"

# Check resource usage
docker stats

# Access container shell
docker exec -it jellyfin-test bash

# Restart all services
docker-compose restart

# Stop all services
docker-compose stop

# Remove all containers and volumes (clean slate)
docker-compose down -v

# Rebuild and restart
docker-compose up -d --build
```

---

## Appendix: Quick Issue Checklist

If a test fails:
1. [ ] Check Docker logs for errors
2. [ ] Verify service connectivity: `docker-compose ps`
3. [ ] Restart affected service: `docker-compose restart [service]`
4. [ ] Check plugin logs: `docker logs jellyfin | grep -i favorite`
5. [ ] Verify configuration still valid
6. [ ] Try manual sync with debug logging enabled
7. [ ] Review TROUBLESHOOTING.md for similar issues
8. [ ] Collect logs and describe issue in detail

---

**Testing Started:** ____________
**Testing Completed:** ____________
**Total Duration:** ____________
**Tester:** ____________
**Sign-off:** ✅ / ❌
