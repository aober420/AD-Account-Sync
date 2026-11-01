using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AdAccountDisableSync;

/// <summary>The plugin entry point.</summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    internal event Action? ScheduleChanged;

    /// <inheritdoc />
    public override void UpdateConfiguration(BasePluginConfiguration configuration)
    {
        if (configuration is not PluginConfiguration settings || settings.SyncIntervalMinutes is < 5 or > 1440)
        {
            throw new ArgumentException("Sync interval must be between 5 and 1440 minutes.", nameof(configuration));
        }

        base.UpdateConfiguration(configuration);
        ScheduleChanged?.Invoke();
    }

    /// <summary>Initializes a new instance of the <see cref="Plugin"/> class.</summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>Gets the running plugin instance.</summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "AD Account Sync";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("e8261d23-9083-4f20-a9e8-6cf5952ba31c");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
        => [new PluginPageInfo { Name = Name, DisplayName = Name, MenuIcon = "settings", EnableInMainMenu = true, EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html" }];
}
