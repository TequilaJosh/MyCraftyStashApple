# Port plan: Windows → Mac + iPad

The Windows app is WPF, which doesn't exist on Apple platforms, so this is a
rewrite, not a port of code. The plan sequences it so something useful ships
at every phase, and so nothing blocks on buying a Mac until we actually need
to run the app on one.

## Phase 1 — Read-only cloud browser (scaffolded)
- Sign in with an `mcs_` API key; Keychain storage. ✅
- Inventory grid + search + item detail from `/api/items`. ✅
- Server side: `/api/items` and `/api/stash/summary` accept `X-Api-Key`
  (shipped in the website repo alongside this scaffold). ✅
- CI compile checks on GitHub macOS runners. ✅
- **Needs a Mac (or a friend with one) only to actually run it.**

## Phase 2 — Feel like an app, not a viewer
- Type filter chips (mirror the desktop's multi-select dropdown).
- Projects list + detail (needs a `/api/projects` read endpoint — the cloud
  schema only has `cloud_items` today, so the desktop uploader and API grow
  projects first).
- Image caching so the grid doesn't refetch SAS URLs on every visit.
- iPad polish: swipe, context menus, keyboard shortcuts on Mac.

## Phase 3 — Offline cache
- Local SQLite (GRDB) mirror of the cloud data with delta refresh, so the
  stash opens instantly and works in a craft room with no Wi-Fi.

## Phase 4 — Editing + two-way sync
- The big one: cloud API needs write endpoints with conflict rules
  (desktop is currently the single writer; last-write-wins per item is the
  likely starting rule). Add/edit items, stock adjustments, projects.

## Phase 5 — Distribution
- Apple Developer Program ($99/yr) for signing.
- Mac: Developer ID + notarization (distribute from the website like the
  Windows app), or Mac App Store.
- iPad: TestFlight for the wife-test, then App Store.

## Hardware reality check
Running, debugging, screenshots, packaging, and notarization all require
macOS. Options, cheapest first: a used M1 Mac mini (~$300), MacStadium /
Scaleway cloud Mac rental, or keep leaning on CI runners (compile checks
only — you can't see the UI). Xcode does not run on Windows; there is no
workaround for that.

## Things that don't carry over (and their replacements)
| Windows | Apple |
|---|---|
| WPF XAML views | SwiftUI |
| CommunityToolkit.Mvvm | ObservableObject / @Observable |
| EF Core + SQLite | GRDB (Phase 3) |
| DPAPI key encryption | Keychain |
| Magick.NET | Core Graphics / CIImage (only if we ever process images on-device) |
| Inno Setup + GitHub releases | Notarized .dmg / TestFlight / App Store |
