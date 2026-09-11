using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AdAccountDisableSync;

/// <summary>The plugin entry point.</summary>
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
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