export default function (view) {
    const pluginId = 'a4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4';

    const FavoriteTagsConfig = {
        loadConfiguration: function () {
            console.log('[FavoriteTags] Loading configuration');
            Dashboard.showLoadingMsg();

            return ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                console.log('[FavoriteTags] Configuration loaded:', config);
                document.querySelector('#sonarrUrl').value = config.SonarrUrl || '';
                document.querySelector('#sonarrApiKey').value = config.SonarrApiKey || '';
                document.querySelector('#radarrUrl').value = config.RadarrUrl || '';
                document.querySelector('#radarrApiKey').value = config.RadarrApiKey || '';
                document.querySelector('#tagName').value = config.TagName || 'jellyfin-favorited';
                document.querySelector('#syncIntervalHours').value = config.SyncIntervalHours || 1;
                Dashboard.hideLoadingMsg();
            }).catch(function (err) {
                console.error('[FavoriteTags] Error loading configuration:', err);
                Dashboard.hideLoadingMsg();
            });
        },

        saveConfiguration: function () {
            console.log('[FavoriteTags] Saving configuration');
            Dashboard.showLoadingMsg();

            return ApiClient.getPluginConfiguration(pluginId).then(function (config) {
                config.SonarrUrl = document.querySelector('#sonarrUrl').value;
                config.SonarrApiKey = document.querySelector('#sonarrApiKey').value;
                config.RadarrUrl = document.querySelector('#radarrUrl').value;
                config.RadarrApiKey = document.querySelector('#radarrApiKey').value;
                config.TagName = document.querySelector('#tagName').value;
                config.SyncIntervalHours = parseFloat(document.querySelector('#syncIntervalHours').value);

                console.log('[FavoriteTags] Saving config:', config);
                return ApiClient.updatePluginConfiguration(pluginId, config).then(function (result) {
                    console.log('[FavoriteTags] Configuration saved:', result);
                    Dashboard.processPluginConfigurationUpdateResult(result);
                }).catch(function (err) {
                    console.error('[FavoriteTags] Error saving configuration:', err);
                    Dashboard.hideLoadingMsg();
                });
            }).catch(function (err) {
                console.error('[FavoriteTags] Error getting configuration:', err);
                Dashboard.hideLoadingMsg();
            });
        }
    };

    console.log('[FavoriteTags] Configuration controller loaded');

    // Load configuration on view show
    view.addEventListener('viewshow', function () {
        console.log('[FavoriteTags] View show event fired');
        FavoriteTagsConfig.loadConfiguration();
    });

    // Handle form submission
    const form = document.querySelector('#favoriteTagsConfigurationForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            console.log('[FavoriteTags] Form submit event fired');
            e.preventDefault();
            FavoriteTagsConfig.saveConfiguration();
            return false;
        });
    }

    console.log('[FavoriteTags] Configuration controller fully initialized');
}
