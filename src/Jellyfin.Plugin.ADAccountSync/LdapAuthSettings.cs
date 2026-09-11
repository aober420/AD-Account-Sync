using System.Reflection;

namespace Jellyfin.Plugin.AdAccountDisableSync;

/// <summary>Read-only LDAP connection settings exposed by LDAP-Auth v24.</summary>
internal sealed record LdapAuthSettings(
    string Server,
    int Port,
    string BindUser,
    string BindPassword,
    string BaseDn,
    bool UseSsl,
    bool UseStartTls)
{
    /// <summary>Reads the active LDAP-Auth configuration without storing a second copy of credentials.</summary>
    public static LdapAuthSettings Read()
    {
        var pluginType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Jellyfin.Plugin.LDAP_Auth.LdapPlugin", throwOnError: false))
            .FirstOrDefault(type => type is not null)
            ?? throw new InvalidOperationException("LDAP-Auth v24 is not loaded. Install and enable LDAP-Auth, then restart Jellyfin.");

        var instance = pluginType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
            ?? throw new InvalidOperationException("LDAP-Auth did not expose its plugin instance.");
        var configuration = pluginType.GetProperty("Configuration", BindingFlags.Public | BindingFlags.Instance)?.GetValue(instance)
            ?? throw new InvalidOperationException("LDAP-Auth configuration is unavailable.");

        return new LdapAuthSettings(
            GetRequiredString(configuration, "LdapServer"),
            GetRequiredInt(configuration, "LdapPort"),
            GetRequiredString(configuration, "LdapBindUser"),
            GetRequiredString(configuration, "LdapBindPassword"),
            GetRequiredString(configuration, "LdapBaseDn"),
            GetOptionalBool(configuration, "UseSsl"),
            GetOptionalBool(configuration, "UseStartTls"));
    }

    private static string GetRequiredString(object configuration, string name)
    {
        var value = configuration.GetType().GetProperty(name)?.GetValue(configuration) as string;
        return !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"LDAP-Auth setting '{name}' is missing or blank.");
    }

    private static int GetRequiredInt(object configuration, string name)
    {
        var value = configuration.GetType().GetProperty(name)?.GetValue(configuration);
        return value is int port && port > 0
            ? port
            : throw new InvalidOperationException($"LDAP-Auth setting '{name}' is missing or invalid.");
    }

    private static bool GetOptionalBool(object configuration, string name)
        => configuration.GetType().GetProperty(name)?.GetValue(configuration) is true;
}
