using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.FavoriteTags.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.FavoriteTags;

/// <summary>
/// The Favorite Tags plugin main class.
/// </summary>
public class FavoriteTagsPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FavoriteTagsPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public FavoriteTagsPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the plugin instance.
    /// </summary>
    public static FavoriteTagsPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Favorite Tags";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a4b4b4b4-b4b4-b4b4-b4b4-b4b4b4b4b4b4");

    /// <inheritdoc />
    public override string? Description => "Automatically sync Jellyfin user favorites to Sonarr/Radarr tags";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.config.html"
            }
        ];
    }

    /// <summary>
    /// Update configuration.
    /// </summary>
    /// <param name="newConfig">New configuration.</param>
    public void UpdateConfiguration(PluginConfiguration newConfig)
    {
        Configuration = newConfig;
        SaveConfiguration();
    }
}
