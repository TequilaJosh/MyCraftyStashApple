# Port plan: Windows desktop → Mac + iPad (.NET MAUI)

The Mac/iPad app is a **standalone** rebuild of the Windows app — its own
local SQLite database, full add/edit, no account or cloud dependency — same as
the desktop. .NET MAUI keeps us in C#/XAML/CommunityToolkit.Mvvm/EF Core (the
desktop stack) and adds a Windows head so the app runs on a PC without a Mac.

History: scaffolded first as a Swift/SwiftUI read-only *cloud viewer*, then
rebuilt as MAUI, then re-scoped to this standalone local-database app (the
cloud-viewer approach was the wrong shape — the goal is everything the Windows
app does, on Mac).

## Foundation (done)
- Ported the 21 EF Core entity models + `InventoryDbContext` from the Windows
  app, verbatim. `AppPaths` rewritten for MAUI's per-app data dir. ✅
- `EnsureCreated()` builds the full 21-table schema on first run (verified on
  the Windows head). ✅
- Lean local `InventoryService` (list/search/get/add/update/delete/count). ✅

## Phase 1 — Inventory (done)
- Card grid + search, add/edit form, detail view, delete — all local CRUD. ✅
- Opens straight to Inventory; no sign-in. ✅

## Phase 2 — Inventory depth
- Type/subtype/theme filters (mirror the desktop's dropdown + chips).
- Photos: add/capture/pick images (MediaPicker), multi-image gallery, thumbs.
- Bought/sold tracking, purchases & sales history, stock adjustments.
- Config-driven Type/Subtype/Theme lists (desktop reads these from config;
  decide local defaults vs. an editable settings screen).

## Phase 3 — The other sections
- Projects (list, detail, linked items, creations).
- Wishlist, Stock Tracking, Address Book, Calendar, Inspiration.
- Each is: port the read/query into `InventoryService` (or a sibling
  service), then build the MAUI page.

## Phase 4 — Import / sync
- Import an existing Windows `inventory.db` (same schema — likely just a file
  copy + pointer, or a guided import).
- Optional: reuse the existing cloud API for cross-device sync later. (The
  website's `apple-api-key` branch is NOT needed for this standalone app; it
  was for the abandoned cloud-viewer approach.)

## Phase 5 — Distribution
- Apple Developer Program ($99/yr); notarized Mac build + TestFlight for iPad.
- A physical Mac is only needed for hands-on Apple UI testing; CI covers
  "does it build," the Windows head covers day-to-day use.

## Known follow-ups
- NU1903: EF Core 10's SQLite provider pulls a transitively-flagged
  `SQLitePCLRaw.lib.e_sqlite3` 2.1.11. Revisit when a patched EF Core 10.0.x /
  SQLitePCLRaw lands; don't hand-pin the native lib (it must match per-platform
  for iOS/Mac).

## What carries over from the desktop codebase
| Desktop (WPF) | MAUI |
|---|---|
| EF Core entity models + DbContext | **Copied verbatim** (same namespaces) |
| CommunityToolkit.Mvvm VMs | Same package, same patterns |
| InventoryService (2,700 lines, WPF-coupled) | Lean re-implementation per screen |
| WPF XAML views | Rebuilt in MAUI XAML |
| Windows install-folder DB path | MAUI per-app data dir (`AppPaths`) |
| Inno Setup + GitHub releases | TestFlight / notarized bundles |
