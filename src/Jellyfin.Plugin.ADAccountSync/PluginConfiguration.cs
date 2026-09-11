using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AdAccountDisableSync;

/// <summary>Configuration for AD account disable synchronization.</summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets a value indicating whether the scheduled task makes changes.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets the AD attribute matching the Jellyfin username.</summary>
    public string UsernameAttribute { get; set; } = "sAMAccountName";

    /// <summary>Gets or sets an LDAP filter restricting AD users considered by the task.</summary>
    public string UserFilter { get; set; } = "(objectCategory=person)";

    /// <summary>Gets or sets a value indicating whether ordinary Jellyfin users can be matched by name.</summary>
    public bool IncludeNonLdapAuthUsers { get; set; }

    /// <summary>Gets or sets a value indicating whether matching enabled AD accounts re-enable Jellyfin users.</summary>
    public bool AutoReEnableUsers { get; set; } = true;
}
