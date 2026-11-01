# Next update

- Remove the legacy interval/startup trigger cleanup from `SyncTimerService` in the next plugin release, as requested on November 1, 2026. Keep the internal timer and configurable sync interval. Users who add their own Scheduled Tasks triggers should have those triggers preserved after this change.
- Update the cleanup regression tests, README, and configuration-page help text accordingly.
- Add a read-only last-run display to the plugin settings page showing the most recent sync run's date and time in the viewer's local timezone. Include manual and internal-timer runs, show "Never run" before the first run, and distinguish successful from failed runs so a failed attempt does not appear as a successful sync. Reuse Jellyfin's persisted task history where possible so the information survives server restarts.
- This is a future-release note only; do not change the behavior of the currently published 1.0.2.1 release.

## Release note headings

Use the applicable headings: ✨ New Features, ⚙️ Improvements, and 🐛 Bug Fixes.
