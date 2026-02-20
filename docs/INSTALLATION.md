# Installation Guide - Favorite Tags Plugin

## Quick Start (5 minutes)

### Prerequisites
- Jellyfin 10.8.x or later
- Sonarr v3 API and/or Radarr v3 API
- Admin access to Jellyfin

### Installation Steps

1. **Download**
   - Get latest DLL from GitHub releases
   - Or build from source (see below)

2. **Install**
   - Copy DLL to: `/path/to/jellyfin/config/plugins/`
   - Restart Jellyfin

3. **Configure**
   - Go to Dashboard → Plugins → Favorite Tags
   - Enter Sonarr/Radarr URLs and API keys
   - Test connections
   - Save and enable

4. **Verify**
   - Wait for first sync (within 1 hour)
   - Check sync history table
   - Verify tags in Sonarr/Radarr

---

## Detailed Installation

### Method 1: Pre-built DLL (Recommended)

#### Step 1: Download DLL

```
GitHub Releases → Favorite Tags → v0.1.0
→ jellyfin-plugin-favorite-tags.dll
```

#### Step 2: Locate Jellyfin Plugins Directory

**Docker:**
```bash
# Jellyfin container plugins directory
/config/plugins/
```

**Bare Metal (Linux):**
```bash
# Default location
/var/lib/jellyfin/plugins/

# Or check Jellyfin logs for exact location
```

**Bare Metal (Windows):**
```bash
# Typical location
C:\ProgramData\Jellyfin\Jellyfin\plugins\

# Or check settings in Jellyfin dashboard
```

**Docker Compose:**
```yaml
volumes:
  - ./plugins:/config/plugins
```

#### Step 3: Copy DLL

```bash
# Linux/Mac
cp jellyfin-plugin-favorite-tags.dll /path/to/jellyfin/config/plugins/

# Windows (PowerShell)
Copy-Item "jellyfin-plugin-favorite-tags.dll" -Destination "C:\ProgramData\Jellyfin\Jellyfin\plugins\"
```

#### Step 4: Restart Jellyfin

```bash
# Docker
docker restart jellyfin

# Systemd
sudo systemctl restart jellyfin

# Manual
# Stop Jellyfin, then start it again
```

#### Step 5: Verify Installation

1. Go to Dashboard → Plugins
2. Look for "Favorite Tags" in plugin list
3. Check version number (v0.1.0)
4. Click to configure

---

### Method 2: Build From Source

#### Prerequisites
- .NET 8.0 SDK installed
- Git installed
- Text editor or IDE

#### Step 1: Clone Repository

```bash
git clone https://github.com/your-org/jellyfin-plugin-favorite-tags.git
cd jellyfin-plugin-favorite-tags
```

#### Step 2: Build Release

```bash
dotnet build -c Release
```

Compiled DLL located at:
```
src/bin/Release/net8.0/jellyfin-plugin-favorite-tags.dll
```

#### Step 3: Copy to Jellyfin

```bash
cp src/bin/Release/net8.0/jellyfin-plugin-favorite-tags.dll \
  /path/to/jellyfin/config/plugins/
```

#### Step 4: Restart Jellyfin

```bash
# Docker
docker restart jellyfin

# Systemd
sudo systemctl restart jellyfin
```

---

## Configuration

### Prerequisites: Get API Keys

**Sonarr:**
1. Open Sonarr (http://sonarr:8989)
2. Settings → General
3. Look for "API Key" section
4. Copy the key (long alphanumeric string)

**Radarr:**
1. Open Radarr (http://radarr:7878)
2. Settings → General
3. Look for "API Key" section
4. Copy the key (long alphanumeric string)

### Configure in Jellyfin

1. **Access Dashboard**
   - Go to Dashboard → Plugins
   - Click "Favorite Tags"

2. **Enter Sonarr Details**
   - Sonarr URL: `http://sonarr:8989`
   - Sonarr API Key: (paste from above)
   - Click "Test Connection"
   - Should show: ✅ "Successfully connected to Sonarr"

3. **Enter Radarr Details** (Optional)
   - Radarr URL: `http://radarr:7878`
   - Radarr API Key: (paste from above)
   - Click "Test Connection"
   - Should show: ✅ "Successfully connected to Radarr"

4. **Set Sync Options**
   - Tag Name: `jellyfin-favorited` (default, change if desired)
   - Sync Interval: `1` (hourly, adjust as needed)
   - Enable Plugin: ✓ (checked)

5. **Save Configuration**
   - Click "Save Configuration"
   - Should show: ✅ "Configuration saved successfully!"

### Advanced Configuration

In "Advanced Settings" section:

- **Dry Run Mode**: Check to test without applying changes
- **Log Level**: Set to "Info" (or "Debug" for troubleshooting)
- **Max API Retries**: `3` (default, increase for unreliable networks)
- **API Timeout**: `30` seconds (default, increase for slow networks)

---

## Post-Installation Verification

### Verify Plugin Loaded

1. Dashboard → Plugins
2. Look for "Favorite Tags" in list
3. Status should show as enabled/active

### Check Logs

1. Dashboard → Logs
2. Filter for "FavoriteTags"
3. Should see initialization messages

### Run Test Sync

1. Go to Plugins → Favorite Tags
2. Status section → "Manual Sync" button
3. Wait for completion (1-2 minutes for typical library)
4. Check "Recent Sync History" table
5. Verify status shows "Success"

### Verify Tags Applied

1. Open Sonarr
2. Browse series
3. Look for "jellyfin-favorited" tag on favorited items
4. (Optional) Open Radarr and verify movies

---

## Docker Installation

### Docker Compose Example

```yaml
version: '3'
services:
  jellyfin:
    image: jellyfin/jellyfin:latest
    container_name: jellyfin
    ports:
      - "8096:8096"
    volumes:
      - ./jellyfin/config:/config
      - ./jellyfin/cache:/cache
      - ./plugins:/config/plugins  # Add this line
    environment:
      - JELLYFIN_FFmpeg=/usr/lib/jellyfin-ffmpeg/ffmpeg
    restart: unless-stopped

  sonarr:
    image: linuxserver/sonarr:latest
    container_name: sonarr
    ports:
      - "8989:8989"
    volumes:
      - ./sonarr/config:/config
    restart: unless-stopped

  radarr:
    image: linuxserver/radarr:latest
    container_name: radarr
    ports:
      - "7878:7878"
    volumes:
      - ./radarr/config:/config
    restart: unless-stopped
```

### Docker Installation Steps

1. **Create plugins directory:**
   ```bash
   mkdir -p ./plugins
   chmod 777 ./plugins
   ```

2. **Copy DLL:**
   ```bash
   cp jellyfin-plugin-favorite-tags.dll ./plugins/
   ```

3. **Start containers:**
   ```bash
   docker-compose up -d
   ```

4. **Wait for startup:**
   ```bash
   docker-compose logs -f jellyfin
   # Watch for "Plugin: Favorite Tags loaded"
   ```

5. **Configure via Jellyfin web UI** (port 8096)

---

## Troubleshooting Installation

### Plugin Not Appearing

**Check:**
1. DLL copied to correct directory
2. Directory permissions allow read access
3. Jellyfin restarted after copying
4. Check Jellyfin logs for errors

**Solution:**
```bash
# Check file exists
ls -la /path/to/jellyfin/config/plugins/ | grep favorite

# Check permissions (should be readable by jellyfin user)
chmod 644 /path/to/jellyfin/config/plugins/jellyfin-plugin-favorite-tags.dll

# Restart Jellyfin
docker restart jellyfin  # or appropriate restart command
```

### Connection Test Fails

**Issue:** "Failed to connect to Sonarr"

**Check:**
1. URL is correct (host and port)
2. Service is running (ping or curl test)
3. API key is correct
4. Firewall not blocking connection
5. Network connectivity between services

**Solution:**
```bash
# Test connectivity
curl http://sonarr:8989/api/v3/system/status?apikey=YOUR_KEY

# If fails, check:
# - Service running: docker ps | grep sonarr
# - Port exposed: docker port sonarr
# - URL correct: Check dashboard settings
```

### Configuration Not Saving

**Issue:** "Error saving configuration"

**Check:**
1. URL format valid (include http://)
2. API key not empty
3. Tag name not empty
4. Sync interval >= 0.25

**Solution:**
```bash
# Check logs for details
docker logs jellyfin | grep "FavoriteTags"

# Validate settings manually
# Sync interval: must be >= 0.25
# URLs: must be valid HTTP(S) URLs
# API keys: must be non-empty
```

### First Sync Not Running

**Issue:** Sync doesn't run within 1 hour

**Check:**
1. Plugin enabled (checkbox checked)
2. At least one service configured (Sonarr OR Radarr)
3. Configuration saved
4. Check logs for errors

**Solution:**
```bash
# Manually trigger sync
# Dashboard → Plugins → Favorite Tags → Sync Actions → Manual Sync

# Check logs
docker logs jellyfin | grep "FavoriteTags.*Sync"

# If no logs, plugin may not be loading
```

---

## Upgrade Installation

### From v0.x to v0.y

1. **Backup Configuration** (optional)
   - Jellyfin automatically saves settings

2. **Download New DLL**
   - Get new version from releases

3. **Stop Jellyfin** (recommended)
   ```bash
   docker stop jellyfin
   ```

4. **Replace DLL**
   ```bash
   cp jellyfin-plugin-favorite-tags-new.dll \
     /path/to/jellyfin/config/plugins/jellyfin-plugin-favorite-tags.dll
   ```

5. **Restart Jellyfin**
   ```bash
   docker start jellyfin
   ```

6. **Verify**
   - Dashboard → Plugins → Check version
   - Run manual sync to test

No configuration migration needed - settings preserved automatically.

---

## Uninstallation

### Remove Plugin

1. **Stop Jellyfin**
   ```bash
   docker stop jellyfin
   ```

2. **Remove DLL**
   ```bash
   rm /path/to/jellyfin/config/plugins/jellyfin-plugin-favorite-tags.dll
   ```

3. **Restart Jellyfin**
   ```bash
   docker start jellyfin
   ```

4. **Verify Removed**
   - Dashboard → Plugins
   - "Favorite Tags" should no longer appear

### Optional: Remove Tags from Sonarr/Radarr

1. Open Sonarr/Radarr
2. Settings → Tags
3. Find "jellyfin-favorited" tag
4. Delete (or leave for historical reference)

---

## Support

### Getting Help

1. **Check docs:**
   - [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
   - [DASHBOARD_GUIDE.md](DASHBOARD_GUIDE.md)
   - [FAQ](TROUBLESHOOTING.md#frequently-asked-questions)

2. **Check logs:**
   - Dashboard → Logs → Filter "FavoriteTags"
   - Save logs for reference

3. **Report issues:**
   - GitHub Issues with:
     - Jellyfin version
     - Sonarr/Radarr versions
     - Relevant error logs
     - Steps to reproduce

---

## Next Steps

After successful installation:

1. **Initial Configuration** - See [DASHBOARD_GUIDE.md](DASHBOARD_GUIDE.md)
2. **Common Issues** - See [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
3. **Advanced Settings** - See [API_ENDPOINTS.md](API_ENDPOINTS.md)

Happy syncing! 🎬

