# AD Account Sync

Jellyfin 12.4.0 release of AD Account Sync.

## Safety behavior

- Only accounts managed by LDAP-Auth are changed by default.
- It can re-enable accounts when the corresponding setting is enabled.
- An LDAP connection or search failure happens before any Jellyfin account is changed.
- It reuses LDAP-Auth's connection settings and does not store a duplicate password.

## Build and install

1. Install and configure LDAP-Auth v24. Its bind account must be able to read `userAccountControl` and `sAMAccountName`.
2. Build with .NET 10: `dotnet publish -c Release -o publish`.
3. Copy the published files into a new subfolder of Jellyfin's `plugins` directory, then restart Jellyfin.
4. In Dashboard → Scheduled Tasks → AD Account Sync, run **Sync Active Directory account status** once and check the server log. The internal timer runs every 5 minutes by default. Change **Sync interval (minutes)** in plugin settings and Save to use another interval (5–1440 minutes).

## Defaults

- AD username attribute: `sAMAccountName`
- AD user scope: `(objectCategory=person)`
- Only LDAP-Auth-managed Jellyfin users are eligible.

- Sync interval: 5 minutes, managed by the plugin's internal timer.

On startup, version 1.0.2.1 waits for task registration and removes this task's existing interval and startup triggers; custom daily and weekly triggers are preserved. Manual sync remains available. The configured AD query returns both enabled and disabled users so account status can be synchronized in either direction.
