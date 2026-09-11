# AD Account Sync

Jellyfin 12.0.0 release of AD Account Sync.

It keeps LDAP-Auth-managed Jellyfin user accounts aligned with AD account status:

- Disables the matching Jellyfin user when their AD account is disabled.
- Re-enables the matching Jellyfin user when their AD account is enabled, if that option is enabled.
- Uses the LDAP server, bind account, Base DN, and TLS settings already configured in LDAP-Auth.
- Runs every 15 minutes, or can be run immediately from Jellyfin Dashboard → Scheduled Tasks.

## Install from the Jellyfin repository

1. In Jellyfin, go to **Dashboard → Plugins → Repositories**.
2. Select **Add**.
3. Paste this repository URL:

```text
https://raw.githubusercontent.com/aober420/AD-Account-Sync/main/manifest.json
```

4. Save, then open **Dashboard → Plugins → Catalog**.
5. Find **AD Account Sync**, install it, and restart Jellyfin when prompted.

## Configure

Open **Dashboard → Plugins → AD Account Sync**.

The defaults are designed for LDAP-Auth users:

- Username attribute: `sAMAccountName`
- User filter: `(objectCategory=person)`
- Local Jellyfin users: excluded
- Re-enable users when their AD account is enabled: enabled

Use **Dashboard → Scheduled Tasks → Sync Active Directory account status** to test a change immediately.

## Requirements

- Jellyfin 12.0.0
- LDAP-Auth v24 configured and working
- The LDAP bind account must be allowed to read `userAccountControl` and the configured username attribute

## Releases

Downloadable builds are available on the [Releases page](https://github.com/aober420/AD-Account-Sync/releases).