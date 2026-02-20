// Favorite Tags Plugin Admin Dashboard

const API_BASE = '/Plugins/FavoriteTags/Admin';

// Initialize dashboard on load
document.addEventListener('DOMContentLoaded', function() {
    loadConfiguration();
    refreshStatus();
    refreshStatistics();
    refreshHistory();

    // Auto-refresh every 30 seconds
    setInterval(refreshStatus, 30000);
    setInterval(refreshStatistics, 60000);
});

// ========== Configuration Management ==========

async function loadConfiguration() {
    try {
        const response = await fetch(`${API_BASE}/Configuration`);
        if (!response.ok) throw new Error('Failed to load configuration');

        const config = await response.json();

        // Populate form fields
        document.getElementById('sonarrUrl').value = config.sonarrUrl || '';
        document.getElementById('sonarrApiKey').value = config.sonarrApiKey || '';
        document.getElementById('radarrUrl').value = config.radarrUrl || '';
        document.getElementById('radarrApiKey').value = config.radarrApiKey || '';
        document.getElementById('tagName').value = config.tagName || 'jellyfin-favorited';
        document.getElementById('syncIntervalHours').value = config.syncIntervalHours || 1;
        document.getElementById('enabled').checked = config.enabled !== false;
        document.getElementById('dryRunMode').checked = config.dryRunMode === true;
        document.getElementById('logLevel').value = config.logLevel || 'Info';
        document.getElementById('maxApiRetries').value = config.maxApiRetries || 3;
        document.getElementById('apiTimeoutSeconds').value = config.apiTimeoutSeconds || 30;

        clearConfigStatus();
    } catch (error) {
        console.error('Error loading configuration:', error);
        showConfigStatus('Error loading configuration: ' + error.message, 'error');
    }
}

async function saveConfiguration(event) {
    event.preventDefault();

    const config = {
        sonarrUrl: document.getElementById('sonarrUrl').value.trim(),
        sonarrApiKey: document.getElementById('sonarrApiKey').value.trim(),
        radarrUrl: document.getElementById('radarrUrl').value.trim(),
        radarrApiKey: document.getElementById('radarrApiKey').value.trim(),
        tagName: document.getElementById('tagName').value.trim(),
        syncIntervalHours: parseFloat(document.getElementById('syncIntervalHours').value),
        enabled: document.getElementById('enabled').checked,
        dryRunMode: document.getElementById('dryRunMode').checked,
        logLevel: document.getElementById('logLevel').value,
        maxApiRetries: parseInt(document.getElementById('maxApiRetries').value),
        apiTimeoutSeconds: parseInt(document.getElementById('apiTimeoutSeconds').value)
    };

    try {
        const response = await fetch(`${API_BASE}/Configuration`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(config)
        });

        if (!response.ok) {
            const error = await response.json();
            if (error.errors) {
                showConfigStatus('Configuration errors:\n' + error.errors.join('\n'), 'error');
            } else {
                throw new Error('Failed to save configuration');
            }
            return;
        }

        showConfigStatus('Configuration saved successfully!', 'success');
        refreshStatus();
    } catch (error) {
        console.error('Error saving configuration:', error);
        showConfigStatus('Error saving configuration: ' + error.message, 'error');
    }
}

function showConfigStatus(message, type) {
    const element = document.getElementById('configStatus');
    element.textContent = message;
    element.className = `message ${type}`;
}

function clearConfigStatus() {
    const element = document.getElementById('configStatus');
    element.textContent = '';
    element.className = 'message';
}

// ========== Status Management ==========

async function refreshStatus() {
    try {
        const response = await fetch(`${API_BASE}/Status`);
        if (!response.ok) throw new Error('Failed to load status');

        const status = await response.json();

        // Update status display
        document.getElementById('lastSyncTime').textContent = status.lastSyncTime ?
            new Date(status.lastSyncTime).toLocaleString() : 'Never';
        document.getElementById('nextSyncTime').textContent = status.nextSyncTime ?
            new Date(status.nextSyncTime).toLocaleString() : 'Unknown';
        document.getElementById('syncRunning').textContent = status.isRunning ? 'Yes' : 'No';
        document.getElementById('statusMessage').textContent = status.message;

        // Update status badge
        const badge = document.getElementById('syncStatus');
        badge.textContent = status.status;
        badge.className = `status-badge status-${status.status.toLowerCase()}`;

        updateButtonStates(status.isRunning);
    } catch (error) {
        console.error('Error refreshing status:', error);
        document.getElementById('statusMessage').textContent = 'Error loading status';
    }
}

function updateButtonStates(isRunning) {
    document.getElementById('manualSyncBtn').disabled = isRunning;
    document.getElementById('dryRunBtn').disabled = isRunning;
    document.getElementById('refreshStatusBtn').disabled = isRunning;
}

// ========== Sync Actions ==========

async function manualSync() {
    if (!confirm('Trigger a manual sync now?')) return;

    await executeSyncAction(`${API_BASE}/Sync`, false);
}

async function dryRunSync() {
    await executeSyncAction(`${API_BASE}/Sync/DryRun`, true);
}

async function executeSyncAction(url, isDryRun) {
    const progressSection = document.getElementById('syncProgress');
    const resultDiv = document.getElementById('syncResult');

    progressSection.style.display = 'block';
    resultDiv.textContent = '';

    try {
        const response = await fetch(url, { method: 'POST' });
        const result = await response.json();

        progressSection.style.display = 'none';

        if (response.ok) {
            const message = `Sync completed:\n` +
                `Items processed: ${result.itemsProcessed}\n` +
                `Tags applied: ${result.tagsApplied}\n` +
                `Tags removed: ${result.tagsRemoved}\n` +
                `Errors: ${result.errorCount}`;

            showSyncResult(message, result.success ? 'success' : 'warning', result.errors);

            // Refresh status and history
            setTimeout(() => {
                refreshStatus();
                refreshStatistics();
                refreshHistory();
            }, 500);
        } else {
            showSyncResult('Sync failed: ' + result.error, 'error');
        }
    } catch (error) {
        progressSection.style.display = 'none';
        showSyncResult('Error executing sync: ' + error.message, 'error');
    }
}

function showSyncResult(message, type, errors = []) {
    const resultDiv = document.getElementById('syncResult');
    resultDiv.textContent = message;
    resultDiv.className = `message ${type}`;

    if (errors && errors.length > 0) {
        showModal('Sync Completed', message, errors);
    }
}

// ========== History Management ==========

async function refreshHistory() {
    try {
        const response = await fetch(`${API_BASE}/History?limit=20`);
        if (!response.ok) throw new Error('Failed to load history');

        const history = await response.json();
        const tbody = document.getElementById('historyBody');

        if (history.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" class="loading">No sync history yet</td></tr>';
            return;
        }

        tbody.innerHTML = history.map(item => `
            <tr>
                <td>${new Date(item.syncTime).toLocaleString()}</td>
                <td><span class="status-badge status-${item.status.toLowerCase()}">${item.status}</span></td>
                <td>${item.itemsProcessed}</td>
                <td>${item.tagsApplied}</td>
                <td>${item.tagsRemoved}</td>
                <td>
                    ${item.errorCount > 0 ?
                        `<span onclick="showHistoryErrors(${JSON.stringify(item.errors).replace(/"/g, '&quot;')})" class="error-link">${item.errorCount}</span>` :
                        '0'}
                </td>
            </tr>
        `).join('');
    } catch (error) {
        console.error('Error loading history:', error);
        const tbody = document.getElementById('historyBody');
        tbody.innerHTML = '<tr><td colspan="6" class="loading">Error loading history</td></tr>';
    }
}

function showHistoryErrors(errors) {
    showModal('Sync Errors', 'Errors encountered during sync:', errors);
}

async function clearHistory() {
    if (!confirm('Clear all sync history? This cannot be undone.')) return;

    try {
        const response = await fetch(`${API_BASE}/History/Clear`, { method: 'POST' });
        if (response.ok) {
            refreshHistory();
            refreshStatistics();
            showSyncResult('History cleared successfully', 'success');
        } else {
            throw new Error('Failed to clear history');
        }
    } catch (error) {
        showSyncResult('Error clearing history: ' + error.message, 'error');
    }
}

// ========== Statistics ==========

async function refreshStatistics() {
    try {
        const response = await fetch(`${API_BASE}/Statistics`);
        if (!response.ok) throw new Error('Failed to load statistics');

        const stats = await response.json();

        document.getElementById('totalSyncs').textContent = stats.total_syncs || 0;
        document.getElementById('successfulSyncs').textContent = stats.successful_syncs || 0;
        document.getElementById('failedSyncs').textContent = stats.failed_syncs || 0;
        document.getElementById('totalItemsProcessed').textContent = stats.total_items_processed || 0;
        document.getElementById('totalTagsApplied').textContent = stats.total_tags_applied || 0;
        document.getElementById('totalTagsRemoved').textContent = stats.total_tags_removed || 0;
    } catch (error) {
        console.error('Error loading statistics:', error);
    }
}

// ========== Connection Testing ==========

async function testSonarrConnection() {
    const url = document.getElementById('sonarrUrl').value.trim();
    const apiKey = document.getElementById('sonarrApiKey').value.trim();

    if (!url || !apiKey) {
        showModal('Validation Error', 'Please enter both Sonarr URL and API key before testing');
        return;
    }

    await testConnection('Sonarr', `${API_BASE}/Test/Sonarr`, url, apiKey);
}

async function testRadarrConnection() {
    const url = document.getElementById('radarrUrl').value.trim();
    const apiKey = document.getElementById('radarrApiKey').value.trim();

    if (!url || !apiKey) {
        showModal('Validation Error', 'Please enter both Radarr URL and API key before testing');
        return;
    }

    await testConnection('Radarr', `${API_BASE}/Test/Radarr`, url, apiKey);
}

async function testConnection(serviceName, url, serverUrl, apiKey) {
    try {
        const response = await fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ url: serverUrl, apiKey: apiKey })
        });

        const result = await response.json();

        if (result.success) {
            showModal(
                `${serviceName} Connection Success`,
                result.details || `Successfully connected to ${serviceName}`
            );
        } else {
            showModal(
                `${serviceName} Connection Failed`,
                result.errorMessage || `Failed to connect to ${serviceName}`
            );
        }
    } catch (error) {
        showModal(
            `${serviceName} Connection Error`,
            'Error testing connection: ' + error.message
        );
    }
}

// ========== Modal ==========

function showModal(title, message, errors = null) {
    const modal = document.getElementById('messageModal');
    document.getElementById('modalTitle').textContent = title;
    document.getElementById('modalMessage').textContent = message;

    const errorDiv = document.getElementById('modalErrors');
    const errorList = document.getElementById('errorList');

    if (errors && errors.length > 0) {
        errorList.innerHTML = errors.map(err => `<li>${err}</li>`).join('');
        errorDiv.style.display = 'block';
    } else {
        errorDiv.style.display = 'none';
    }

    modal.classList.add('active');
}

function closeModal() {
    const modal = document.getElementById('messageModal');
    modal.classList.remove('active');
}

// Close modal when clicking outside
window.onclick = function(event) {
    const modal = document.getElementById('messageModal');
    if (event.target === modal) {
        modal.classList.remove('active');
    }
};

// ========== Utility Functions ==========

function formatDate(dateString) {
    return new Date(dateString).toLocaleString();
}
