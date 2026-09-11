using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Novell.Directory.Ldap;

namespace Jellyfin.Plugin.AdAccountDisableSync;

/// <summary>Synchronizes the enabled state of LDAP-Auth-managed Jellyfin users with AD.</summary>
public sealed class AdAccountDisableSyncTask : IScheduledTask
{
    private const string LdapAuthProviderId = "Jellyfin.Plugin.LDAP_Auth.LdapAuthenticationProviderPlugin";
    private const string UserAccountControlAttribute = "userAccountControl";
    private const int AccountDisabledFlag = 2;
    private readonly IUserManager _userManager;
    private readonly ILogger<AdAccountDisableSyncTask> _logger;

    /// <summary>Initializes a new instance of the task.</summary>
    public AdAccountDisableSyncTask(IUserManager userManager, ILogger<AdAccountDisableSyncTask> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Sync Active Directory account status";

    /// <inheritdoc />
    public string Description => "Disables or, when enabled in settings, re-enables LDAP-Auth users to match Active Directory.";

    /// <inheritdoc />
    public string Category => "Users";

    /// <inheritdoc />
    public string Key => "AdAccountDisableSync";

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        => [new TaskTriggerInfo { Type = TaskTriggerInfoType.IntervalTrigger, IntervalTicks = TimeSpan.FromMinutes(15).Ticks }];

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var configuration = Plugin.Instance?.Configuration
            ?? throw new InvalidOperationException("Plugin configuration is unavailable.");
        if (!configuration.Enabled)
        {
            _logger.LogInformation("AD account synchronization is disabled.");
            return;
        }

        var accountStates = GetAccountStates(LdapAuthSettings.Read(), configuration, cancellationToken);
        var users = _userManager.GetUsers().ToList();
        var disabled = 0;
        var reEnabled = 0;

        for (var index = 0; index < users.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress.Report(users.Count == 0 ? 100 : index * 100d / users.Count);
            var user = users[index];

            if (!configuration.IncludeNonLdapAuthUsers
                && !string.Equals(user.AuthenticationProviderId, LdapAuthProviderId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (accountStates.Disabled.Contains(user.Username) && !user.HasPermission(PermissionKind.IsDisabled))
            {
                user.SetPermission(PermissionKind.IsDisabled, true);
                await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
                disabled++;
                _logger.LogWarning("Disabled Jellyfin user {Username}: the matching AD account is disabled.", user.Username);
            }
            else if (configuration.AutoReEnableUsers
                && accountStates.Enabled.Contains(user.Username)
                && user.HasPermission(PermissionKind.IsDisabled))
            {
                user.SetPermission(PermissionKind.IsDisabled, false);
                await _userManager.UpdateUserAsync(user).ConfigureAwait(false);
                reEnabled++;
                _logger.LogWarning("Re-enabled Jellyfin user {Username}: the matching AD account is enabled.", user.Username);
            }
        }

        progress.Report(100);
        _logger.LogInformation("AD account synchronization completed: {DisabledCount} disabled, {ReEnabledCount} re-enabled.", disabled, reEnabled);
    }

    private DirectoryAccountStates GetAccountStates(
        LdapAuthSettings settings,
        PluginConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var options = new LdapConnectionOptions();
        if (settings.UseSsl)
        {
            options.UseSsl();
        }

        using var connection = new LdapConnection(options);
        connection.Connect(settings.Server, settings.Port);
        if (settings.UseStartTls)
        {
            connection.StartTls();
        }

        connection.Bind(settings.BindUser, settings.BindPassword);
        var constraints = connection.SearchConstraints;
        constraints.ReferralFollowing = true;
        var results = connection.Search(
            settings.BaseDn,
            LdapConnection.ScopeSub,
            configuration.UserFilter,
            [configuration.UsernameAttribute, UserAccountControlAttribute],
            false,
            constraints);
        var disabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var enabled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (results.HasMore())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var attributes = results.Next().GetAttributeSet();
            var username = attributes.GetAttribute(configuration.UsernameAttribute)?.StringValue;
            if (string.IsNullOrWhiteSpace(username))
            {
                continue;
            }

            var accountControl = attributes.GetAttribute(UserAccountControlAttribute)?.StringValue;
            if (int.TryParse(accountControl, out var value) && (value & AccountDisabledFlag) != 0)
            {
                disabled.Add(username);
            }
            else
            {
                enabled.Add(username);
            }
        }

        _logger.LogDebug("AD returned {DisabledCount} disabled and {EnabledCount} enabled account(s).", disabled.Count, enabled.Count);
        return new DirectoryAccountStates(disabled, enabled);
    }

    private sealed record DirectoryAccountStates(HashSet<string> Disabled, HashSet<string> Enabled);
}