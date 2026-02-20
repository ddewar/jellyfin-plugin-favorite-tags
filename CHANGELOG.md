# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0.0] - 2026-02-20

### Added
- Initial public release with full functionality
- Sync Jellyfin user favorites to Sonarr/Radarr tags
- Multi-user favorite aggregation across all users
- Manual sync with test buttons for Sonarr/Radarr connections
- Detailed dry-run output showing exact changes
- Auto-scheduling with configurable intervals (0 = manual only)
- Scheduled task visible in Jellyfin dashboard
- Track last sync time to prevent duplicate runs
- Remove tags from unfavorited items automatically
- Idempotent operations (safe to run multiple times)
- Security hardened: authentication on all endpoints, no API key leaks, sanitized logs
- Admin dashboard for configuration and monitoring
- REST API with 11 endpoints for programmatic control

### Security
- All endpoints require authentication
- API keys transmitted via X-Api-Key headers (not URL parameters)
- No sensitive data logged to Jellyfin logs
- Input validation on all configuration fields
- Sanitized error messages (no information disclosure)
