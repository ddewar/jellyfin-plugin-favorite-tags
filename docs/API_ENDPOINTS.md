# Favorite Tags API Endpoints

## Overview

All endpoints require admin authentication and are prefixed with `/Plugins/FavoriteTags/Admin`.

**Base URL:** `http://jellyfin-instance/Plugins/FavoriteTags/Admin`

## Authentication

All endpoints require the `RequireAdministratorRole` authorization policy.

Include the Jellyfin authentication token in the request:
```
Authorization: Bearer {token}
X-MediaBrowser-Token: {token}
```

## Endpoints

### Configuration Management

#### GET /Configuration
Retrieve the current plugin configuration.

**Response (200 OK):**
```json
{
  "sonarrUrl": "http://sonarr:8989",
  "sonarrApiKey": "your-api-key",
  "radarrUrl": "http://radarr:7878",
  "radarrApiKey": "your-api-key",
  "tagName": "jellyfin-favorited",
  "syncIntervalHours": 1.0,
  "enabled": true,
  "dryRunMode": false,
  "logLevel": "Info",
  "maxApiRetries": 3,
  "apiTimeoutSeconds": 30
}
```

#### POST /Configuration
Save the plugin configuration.

**Request Body:**
```json
{
  "sonarrUrl": "http://sonarr:8989",
  "sonarrApiKey": "your-api-key",
  "radarrUrl": "http://radarr:7878",
  "radarrApiKey": "your-api-key",
  "tagName": "jellyfin-favorited",
  "syncIntervalHours": 1.0,
  "enabled": true,
  "dryRunMode": false,
  "logLevel": "Info",
  "maxApiRetries": 3,
  "apiTimeoutSeconds": 30
}
```

**Response (200 OK):**
```json
{
  "message": "Configuration saved successfully"
}
```

**Response (400 Bad Request):**
```json
{
  "errors": [
    "At least one of Sonarr URL or Radarr URL must be configured",
    "Sonarr URL is not a valid URI"
  ]
}
```

### Status & Sync Control

#### GET /Status
Get the current sync status.

**Response (200 OK):**
```json
{
  "lastSyncTime": "2024-01-16T10:30:00Z",
  "nextSyncTime": "2024-01-16T11:30:00Z",
  "isRunning": false,
  "status": "Success",
  "message": "Sync completed: 15 tagged, 2 removed, 0 errors"
}
```

#### POST /Sync
Trigger a manual sync immediately.

**Response (200 OK):**
```json
{
  "syncTime": "2024-01-16T10:30:00Z",
  "itemsProcessed": 150,
  "tagsApplied": 15,
  "tagsRemoved": 2,
  "errorCount": 0,
  "errors": [],
  "success": true,
  "message": "Sync completed: 15 tagged, 2 removed, 0 errors"
}
```

**Response (409 Conflict):**
```json
{
  "error": "Sync already in progress"
}
```

#### POST /Sync/DryRun
Run a sync without making any changes (preview mode).

**Response (200 OK):**
```json
{
  "syncTime": "2024-01-16T10:30:00Z",
  "itemsProcessed": 150,
  "tagsApplied": 15,
  "tagsRemoved": 2,
  "errorCount": 0,
  "errors": [],
  "success": true,
  "message": "[DRY-RUN] Sync completed: 15 would be tagged, 2 would be removed"
}
```

### History & Monitoring

#### GET /History
Get recent sync history.

**Query Parameters:**
- `limit` (optional): Number of results (1-50, default: 10)

**Response (200 OK):**
```json
[
  {
    "syncTime": "2024-01-16T10:30:00Z",
    "status": "Success",
    "itemsProcessed": 150,
    "tagsApplied": 15,
    "tagsRemoved": 2,
    "errorCount": 0,
    "errors": []
  },
  {
    "syncTime": "2024-01-16T09:30:00Z",
    "status": "Warning",
    "itemsProcessed": 150,
    "tagsApplied": 14,
    "tagsRemoved": 2,
    "errorCount": 1,
    "errors": ["Could not find item 'Breaking Bad' in Sonarr"]
  }
]
```

#### GET /Statistics
Get overall sync statistics.

**Response (200 OK):**
```json
{
  "total_syncs": 24,
  "successful_syncs": 23,
  "failed_syncs": 1,
  "total_items_processed": 3600,
  "total_tags_applied": 360,
  "total_tags_removed": 45,
  "total_errors": 8,
  "first_sync": "2024-01-01T10:00:00Z",
  "last_sync": "2024-01-16T10:30:00Z"
}
```

#### POST /History/Clear
Clear all sync history.

**Response (200 OK):**
```json
{
  "message": "History cleared"
}
```

### Connection Testing

#### POST /Test/Sonarr
Test connection to Sonarr.

**Request Body:**
```json
{
  "url": "http://sonarr:8989",
  "apiKey": "your-api-key"
}
```

**Response (200 OK - Success):**
```json
{
  "success": true,
  "details": "Successfully connected to Sonarr"
}
```

**Response (200 OK - Failure):**
```json
{
  "success": false,
  "errorMessage": "Failed to connect to Sonarr",
  "details": null
}
```

**Response (400 Bad Request):**
```json
{
  "error": "URL and API key required"
}
```

#### POST /Test/Radarr
Test connection to Radarr.

**Request Body:**
```json
{
  "url": "http://radarr:7878",
  "apiKey": "your-api-key"
}
```

**Response (200 OK - Success):**
```json
{
  "success": true,
  "details": "Successfully connected to Radarr"
}
```

**Response (200 OK - Failure):**
```json
{
  "success": false,
  "errorMessage": "Failed to connect to Radarr",
  "details": null
}
```

## Error Responses

### 400 Bad Request
- Invalid request format
- Missing required fields
- Invalid configuration values

### 401 Unauthorized
- Missing authentication token
- Invalid token

### 403 Forbidden
- User does not have admin role

### 409 Conflict
- Sync already in progress

### 500 Internal Server Error
- Server-side error during processing

## Usage Examples

### JavaScript/Fetch
```javascript
// Get configuration
const response = await fetch(
  'http://jellyfin-instance/Plugins/FavoriteTags/Admin/Configuration',
  {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  }
);
const config = await response.json();

// Trigger manual sync
const syncResponse = await fetch(
  'http://jellyfin-instance/Plugins/FavoriteTags/Admin/Sync',
  {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  }
);
const result = await syncResponse.json();

// Get recent history
const historyResponse = await fetch(
  'http://jellyfin-instance/Plugins/FavoriteTags/Admin/History?limit=10',
  {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  }
);
const history = await historyResponse.json();
```

### cURL
```bash
# Get configuration
curl -H "Authorization: Bearer YOUR_TOKEN" \
  http://jellyfin-instance/Plugins/FavoriteTags/Admin/Configuration

# Save configuration
curl -X POST \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sonarrUrl": "http://sonarr:8989",
    "sonarrApiKey": "key",
    "tagName": "jellyfin-favorited",
    "syncIntervalHours": 1,
    "enabled": true
  }' \
  http://jellyfin-instance/Plugins/FavoriteTags/Admin/Configuration

# Trigger manual sync
curl -X POST \
  -H "Authorization: Bearer YOUR_TOKEN" \
  http://jellyfin-instance/Plugins/FavoriteTags/Admin/Sync

# Test Sonarr connection
curl -X POST \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "http://sonarr:8989",
    "apiKey": "your-key"
  }' \
  http://jellyfin-instance/Plugins/FavoriteTags/Admin/Test/Sonarr
```

## Rate Limiting

No built-in rate limiting is implemented. Sonarr and Radarr may enforce rate limits on their APIs.

## Timeout Behavior

- Configuration timeout: 30 seconds (configurable)
- Connection test timeout: 10 seconds (hardcoded for safety)
- Manual sync: No timeout (runs to completion)

## Field Validation

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| SonarrUrl | string | conditional | Valid URI, required if Sonarr API key set |
| SonarrApiKey | string | conditional | Required if SonarrUrl set |
| RadarrUrl | string | conditional | Valid URI, required if Radarr API key set |
| RadarrApiKey | string | conditional | Required if RadarrUrl set |
| TagName | string | yes | Non-empty, alphanumeric |
| SyncIntervalHours | number | yes | >= 0.25 |
| Enabled | boolean | yes | - |
| DryRunMode | boolean | yes | - |
| LogLevel | string | yes | Info, Debug, Warning, Error |
| MaxApiRetries | integer | yes | >= 1 |
| ApiTimeoutSeconds | integer | yes | >= 1 |
