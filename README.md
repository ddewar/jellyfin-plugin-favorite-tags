# Favorite Tags Plugin for Jellyfin

Automatically sync Jellyfin user favorites to Sonarr/Radarr tags.

## Overview

**Favorite Tags** is a Jellyfin plugin that monitors user-favorited items and automatically applies tags in Sonarr and Radarr. This enables seamless integration with deletion protection systems like Jellysweep and other media management workflows.

### What It Does

- 📌 **Monitors Favorites** - Detects when Jellyfin users favorite content
- 🏷️ **Applies Tags** - Automatically tags matched items in Sonarr/Radarr
- 👥 **Multi-user Support** - Aggregates favorites across all users
- 🔄 **Scheduled Sync** - Runs on configurable intervals (default: hourly)
- 🎯 **Smart Matching** - TVDB/IMDB ID matching with title fallback
- 🖥️ **Admin Dashboard** - Web-based UI for configuration and monitoring

### Key Features

- ✅ **Multi-user Aggregation** - Protection if ANY user favorited
- ✅ **Idempotent Operations** - Safe to run repeatedly
- ✅ **Dry-run Mode** - Preview changes before applying
- ✅ **Flexible Matching** - ID-based and title-based
- ✅ **Error Resilient** - Continues on partial failures
- ✅ **Comprehensive Logging** - Debug troubleshooting easy
- ✅ **11 REST Endpoints** - Full API control
- ✅ **Mobile Responsive** - Dashboard works everywhere

## Quick Start

### Installation (Recommended)

**Add via Plugin Repository:**

1. Open Jellyfin Dashboard
2. Go to **Plugins** → **Repositories**
3. Click **+** button to add repository
4. Paste: `https://raw.githubusercontent.com/ddewar/jellyfin-plugin-favorite-tags/main/src/plugin.manifest.json`
5. Click **Add**
6. Go to **Catalog** tab
7. Find "Favorite Tags" and click **Install**
8. Restart Jellyfin
9. Go to Dashboard → Plugins → Favorite Tags
10. Configure Sonarr/Radarr URLs and API keys
11. Test connections and save

**Alternative: Manual Install from Releases**

1. Download DLL from [releases](https://github.com/ddewar/jellyfin-plugin-favorite-tags/releases)
2. Copy to Jellyfin plugins directory
3. Restart Jellyfin
4. Configure plugin

**Detailed:** See [INSTALLATION.md](docs/INSTALLATION.md)

### Configuration

1. **Get API Keys**
   - Sonarr: Settings → General → API Key
   - Radarr: Settings → General → API Key

2. **Configure Plugin**
   - Sonarr URL: `http://sonarr:8989`
   - Sonarr API Key: (paste from above)
   - Radarr URL: `http://radarr:7878` (optional)
   - Radarr API Key: (paste from above)
   - Tag Name: `jellyfin-favorited`
   - Sync Interval: `1` (hour)

3. **Test & Save**
   - Click "Test Connection" for each service
   - Save configuration
   - First sync runs within 1 hour

**Detailed:** See [DASHBOARD_GUIDE.md](docs/DASHBOARD_GUIDE.md)

## Documentation

- **[Installation Guide](docs/INSTALLATION.md)** - Step-by-step setup
- **[User Guide](docs/DASHBOARD_GUIDE.md)** - Dashboard walkthrough
- **[Troubleshooting](docs/TROUBLESHOOTING.md)** - Common issues & solutions
- **[API Reference](docs/API_ENDPOINTS.md)** - REST endpoints
- **[Developer Guide](docs/DEVELOPER_GUIDE.md)** - Architecture & development
- **[Testing Guide](docs/TESTING_GUIDE.md)** - Running tests
- **[Release Notes](docs/RELEASE_NOTES.md)** - What's included
- **[Security Audit](docs/SECURITY_AUDIT.md)** - Security review

## System Requirements

- **Jellyfin:** 10.11.6 or later
- **Sonarr:** v3 API (optional)
- **Radarr:** v3 API (optional)
- **.NET 9.0:** For building from source

*At least one of Sonarr or Radarr must be configured*

## Build & Test

### Build from Source

```bash
# Build release
dotnet build src/jellyfin-plugin-favorite-tags.csproj -c Release

# DLL location: src/bin/Release/net9.0/Jellyfin.Plugin.FavoriteTags.dll
```

### Run Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test
dotnet test --filter "ClassName=SyncServiceTests"
```

## Project Structure

```
src/
├── Services/          - API integration & sync logic
├── Controllers/       - REST endpoints
├── Models/           - Data structures
├── Configuration/    - Settings management
├── ScheduledTasks/   - Background sync
├── Web/              - Admin dashboard UI
├── Exceptions/       - Error handling
└── Constants.cs      - Plugin constants

tests/
├── Services/         - Service tests
├── Integration/      - Workflow tests
├── Controllers/      - API tests
└── TestHelpers/      - Mock data generators

docs/
├── INSTALLATION.md   - Setup guide
├── DASHBOARD_GUIDE.md - User guide
├── TROUBLESHOOTING.md - Problem solving
└── [5 more guides]
```

## API Endpoints

The plugin exposes 11 REST endpoints (all require admin role):

```
GET    /Configuration           - Load settings
POST   /Configuration           - Save settings
GET    /Status                 - Current status
POST   /Sync                   - Manual sync
POST   /Sync/DryRun            - Preview sync
GET    /History                - Sync history
GET    /Statistics             - Overall stats
POST   /History/Clear          - Clear history
POST   /Test/Sonarr            - Test Sonarr connection
POST   /Test/Radarr            - Test Radarr connection
```

**See [API_ENDPOINTS.md](docs/API_ENDPOINTS.md) for full reference**

## Features

### Sync Capabilities

- ✅ Series sync by TVDB ID
- ✅ Movie sync by IMDB ID
- ✅ Title-based fallback matching
- ✅ Multi-user favorite aggregation
- ✅ Idempotent tag operations
- ✅ Partial failure resilience
- ✅ Dry-run preview mode
- ✅ Concurrent sync prevention

### Admin Dashboard

- ✅ Real-time status display
- ✅ Configuration management
- ✅ Manual sync trigger
- ✅ Dry-run testing
- ✅ Sync history table
- ✅ Performance statistics
- ✅ Connection testing
- ✅ Mobile responsive

### Configuration Options

- ✅ Sonarr support (optional)
- ✅ Radarr support (optional)
- ✅ Custom tag names
- ✅ Configurable sync interval
- ✅ Enable/disable toggle
- ✅ Advanced settings
- ✅ Encrypted credentials
- ✅ Input validation

## Usage Scenarios

### Single User
- Tag favorites in Jellyfin
- Tags automatically applied in Sonarr/Radarr
- Use for deletion protection

### Multiple Users
- Each user favorites independently
- Aggregated across all users
- Tag applied if ANY user favorited
- Unfavorite by one doesn't remove tag if others have it

### Use with Jellysweep
- Enable plugin and configure Sonarr/Radarr
- Jellysweep skips items with "jellyfin-favorited" tag
- Favorite system = protection system

## Performance

### Typical Sync Times
- 100 items: ~5-10 seconds
- 500 items: ~20-30 seconds
- 1000+ items: ~1-2 minutes

### Resource Usage
- Memory: ~50-100 MB
- CPU: Minimal when idle, scales with sync
- Network: Depends on Sonarr/Radarr API response time

## Testing

- **50+ Test Cases** covering all major functionality
- **Unit Tests** - Individual components
- **Integration Tests** - Complete workflows
- **Error Scenario Tests** - Edge cases
- **>85% Code Coverage**

**See [TESTING_GUIDE.md](docs/TESTING_GUIDE.md) for details**

## Security

- ✅ API keys encrypted at rest
- ✅ Admin-only endpoints
- ✅ Input validation on all parameters
- ✅ No hardcoded credentials
- ✅ Request timeouts
- ✅ Retry backoff implemented
- ✅ No information disclosure

**See [SECURITY_AUDIT.md](docs/SECURITY_AUDIT.md) for full review**

## Support & Help

- 📖 **Read Docs** - Check [documentation](docs/)
- 🐛 **Report Issues** - GitHub Issues with details
- 💬 **Get Help** - See [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md)

## License

MIT License - See [LICENSE](LICENSE)

## Credits

- Built with Jellyfin Plugin SDK
- Inspired by Jellysweep
- Community feedback appreciated

---

## FAQ

**Q: Do I need both Sonarr and Radarr?**
A: No, either one works independently. Configure at least one.

**Q: How often does it sync?**
A: Default every 1 hour. Adjust in settings.

**Q: Is it safe to run multiple times?**
A: Yes! Idempotent - safe to sync repeatedly.

**Q: Does it modify media files?**
A: No! Only tags in Sonarr/Radarr.

**Q: What if I unfavorite something?**
A: Tag is removed when NO users have it favorited.

**More FAQ:** See [TROUBLESHOOTING.md](docs/TROUBLESHOOTING.md#frequently-asked-questions)

---

**Get Started:** [Installation Guide](docs/INSTALLATION.md)
**Learn More:** [User Guide](docs/DASHBOARD_GUIDE.md)
**Need Help:** [Troubleshooting](docs/TROUBLESHOOTING.md)
