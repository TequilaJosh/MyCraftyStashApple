# Port plan: Windows desktop → Mac + iPad (.NET MAUI)

The Windows app is WPF, which doesn't run on Apple platforms. .NET MAUI keeps
us in C#/XAML/CommunityToolkit.Mvvm (the desktop stack) and adds a Windows
head so the app can be developed and demoed on a PC without owning a Mac.
History: the first scaffold was Swift/SwiftUI (commit history has it); it was
replaced with MAUI so development doesn't require Apple hardware or learning
a new stack.

## Phase 1 — Read-only cloud browser (scaffolded)
- Sign in with an `mcs_` API key; SecureStorage (Keychain/DPAPI). ✅
- Inventory grid + search + item detail from `/api/items`. ✅
- Windows head runs locally for development. ✅
- CI compile checks of `net10.0-ios` + `net10.0-maccatalyst` on GitHub macOS
  runners. ✅
- Server side: the `X-Api-Key` support for the read endpoints lives on the
  website repo's `apple-api-key` branch — **merge after testing** (until then
  the app's sign-in works, but the items list 401s).

## Phase 2 — Feel like an app, not a viewer
- Type filter (mirror the desktop's multi-select dropdown + subtype chips).
- Projects list + detail (needs a `/api/projects` endpoint — cloud schema
  only has `cloud_items` today, so the desktop uploader and API grow first).
- Image caching; iPad polish (touch targets, context menus).

## Phase 3 — Offline cache
- Local SQLite mirror (`Microsoft.Data.Sqlite` or EF Core, same as desktop)
  with delta refresh so the stash opens instantly without Wi-Fi.

## Phase 4 — Editing + two-way sync
- Cloud API write endpoints with conflict rules (desktop is currently the
  single writer; last-write-wins per item is the likely starting rule).

## Phase 5 — Distribution
- Apple Developer Program ($99/yr) for signing.
- Mac: notarized .pkg/.dmg from the website, or Mac App Store.
- iPad: TestFlight for the wife-test, then App Store.
- Packaging/signing runs on CI's macOS runners or a rented cloud Mac; a
  physical Mac is only strictly needed for hands-on UI testing of the Apple
  builds (Windows head covers day-to-day).

## What carries over from the desktop codebase
| Desktop (WPF) | MAUI |
|---|---|
| CommunityToolkit.Mvvm VMs | Same package, same patterns |
| C# models/services | Portable with minor changes |
| WPF XAML views | Rewritten in MAUI XAML (similar but not compatible) |
| DPAPI key encryption | `SecureStorage` |
| EF Core + SQLite | Same (Phase 3) |
| Inno Setup + GitHub releases | TestFlight / notarized bundles |
