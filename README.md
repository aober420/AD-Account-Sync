<img width="1672" height="941" alt="jellyfin-plugin-AD-Account-Sync" src="https://github.com/user-attachments/assets/ab267603-aaf4-4361-b2ba-62ca0e56738e" />

# AD Account Sync

Jellyfin 12.4.0 release of AD Account Sync.

It keeps LDAP-Auth-managed Jellyfin user accounts aligned with AD account status:

- Disables the matching Jellyfin user when their AD account is disabled.
- Re-enables the matching Jellyfin user when their AD account is enabled, if that option is enabled.
- Uses the LDAP server, bind account, Base DN, and TLS settings already configured in LDAP-Auth.
- Runs on its own timer every 5 minutes by default; choose 5–1440 minutes in the plugin settings.
- Can be run immediately from Jellyfin Dashboard → Scheduled Tasks → AD Account Sync.

## Install from the Jellyfin repository

1. In Jellyfin, go to **Dashboard → Plugins → Repositories**.
2. Select **Add**.
3. Paste this repository URL:

```text
https://raw.githubusercontent.com/aober420/AD-Account-Sync/main/manifest.json
```

4. Save, then open **Dashboard → Plugins → Catalog**.
5. Find **AD Account Sync**, install it, and restart Jellyfin when prompted

## Configure

Open **Dashboard → Plugins → AD Account Sync**.

The defaults are designed for LDAP-Auth users:

- Username attribute: `sAMAccountName`
- User filter: `(objectCategory=person)`
- Local Jellyfin users: excluded
- Re-enable users when their AD account is enabled: enabled
- Sync interval: 5 minutes (minimum 5; maximum 1440)

Set **Sync interval (minutes)** and select **Save** to restart the plugin timer with the chosen interval. Disabling synchronization pauses automatic syncing.

After updating to 1.0.2.1, restart Jellyfin. The plugin waits for task registration, then automatically removes its old interval and startup triggers from Scheduled Tasks. Custom daily or weekly triggers are preserved. You do not need an interval or startup trigger for automatic syncing.

Use **Dashboard → Scheduled Tasks → AD Account Sync → Sync Active Directory account status** to test a change immediately. Manual and timed runs share Jellyfin's task worker to prevent overlapping synchronization.

This release does not modify Jellyfin Web files or require permission to write to the web directory.

## Requirements

- Jellyfin 12.4.0
- LDAP-Auth v24 configured and working
- The LDAP bind account must be allowed to read `userAccountControl` and the configured username attribute

## Releases

Downloadable builds are available on the [Releases page](https://github.com/aober420/AD-Account-Sync/releases).

## License

Copyright (c) 2026 aober420 (Aober). Plugin source is licensed under **GPL-3.0-only**. See [LICENSE](LICENSE) and [NOTICE](NOTICE). Third-party components retain their respective licenses.
