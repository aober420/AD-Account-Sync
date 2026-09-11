# AD Account Sync

Jellyfin 12.0.0 release of AD Account Sync.

## Safety behavior

- Only accounts managed by LDAP-Auth are changed by default.
- It can re-enable accounts when the corresponding setting is enabled.
- An LDAP connection or search failure happens before any Jellyfin account is changed.
- It reuses LDAP-Auth's connection settings and does not store a duplicate password.

## Build and install

1. Install and configure LDAP-Auth v24. Its bind account must be able to read `userAccountControl` and `sAMAccountName`.
2. Build with .NET 10: `dotnet publish -c Release -o publish`.
3. Copy the published files into a new subfolder of Jellyfin's `plugins` directory, then restart Jellyfin.
4. In Dashboard → Scheduled Tasks, run **Sync disabled Active Directory accounts** once and check the server log. The normal 15-minute schedule begins automatically.

## Defaults

- AD username attribute: `sAMAccountName`
- AD user scope: `(objectCategory=person)`
- Only LDAP-Auth-managed Jellyfin users are eligible.

The resulting AD query is `(&(objectCategory=person)(userAccountControl:1.2.840.113556.1.4.803:=2))`.
