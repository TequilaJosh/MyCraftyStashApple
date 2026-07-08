# My Crafty Stash — Mac + iPad (.NET MAUI)

A native Mac + iPad build of [My Crafty Stash](https://github.com/TequilaJosh/MyCraftyStash),
the Windows craft-inventory app. **Standalone, not a cloud viewer** — the app
keeps its own local database on the device and opens straight to your stash,
no account or API key. Built with .NET MAUI so it's C# + XAML +
CommunityToolkit.Mvvm + EF Core/SQLite — the same stack as the Windows app.

**Targets:** iPadOS (`net10.0-ios`), macOS (`net10.0-maccatalyst`), and a
Windows head (`net10.0-windows...`) used for day-to-day development so the app
can be run and clicked on a PC while the identical code ships to Apple.

**Status — Phase 1 (Inventory):** the app stands up the full SQLite schema on
first run (21 tables, shared verbatim with the Windows app), and Inventory is
a working vertical slice: searchable card grid, add / edit / delete, and a
detail view — all reading and writing the local database. Remaining desktop
features (Projects, Wishlist, Stock Tracking, images, etc.) are being ported
screen by screen; see [docs/PORT-PLAN.md](docs/PORT-PLAN.md).

## Building

```bash
dotnet workload install maui
cd MyCraftyStash
# Run on Windows (development):
dotnet build -t:Run -f net10.0-windows10.0.19041.0
# Compile-check the Apple targets (requires macOS + Xcode):
dotnet build -f net10.0-ios
dotnet build -f net10.0-maccatalyst
```

No Mac handy? Every push to `main` compile-checks the iOS and Mac Catalyst
targets on GitHub's macOS runners (`.github/workflows/build.yml`). That's the
source of truth for "does it build on Apple" until real hardware arrives; the
Windows head is how we see and use the app meanwhile.

## Architecture notes

- **Data layer is shared with the Windows app, verbatim.** The 21 EF Core
  entity models and `InventoryDbContext` were copied unchanged (same
  namespaces, same snake_case table map). Only `AppPaths` differs — it points
  the SQLite file at MAUI's per-app data directory (sandbox-safe on every
  platform) instead of the Windows install folder.
- The DB is created with `EnsureCreated()` on first run. It uses WAL mode, so
  the schema/data live in `inventory.db` + `inventory.db-wal`.
- `Services/InventoryService.cs` is a lean, MAUI-focused subset of the
  desktop's 2,700-line service — just what the current screens need.
- Images: the desktop stores photos as base64 (often a `data:` URI) inline in
  `image_url`; `ImageStringConverter` turns that into a MAUI `ImageSource`
  (also handles http URLs and file paths).
