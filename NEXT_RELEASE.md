# Next update

- Remove the legacy interval/startup trigger cleanup from `SyncTimerService` in the next plugin release, as requested on November 1, 2026. Keep the internal timer and configurable sync interval. Users who add their own Scheduled Tasks triggers should have those triggers preserved after this change.
- Update the cleanup regression tests, README, and configuration-page help text accordingly.
- This is a future-release note only; do not change the behavior of the currently published 1.0.2.1 release.

## Release note headings

Use the applicable headings: ✨ New Features, ⚙️ Improvements, and 🐛 Bug Fixes.
