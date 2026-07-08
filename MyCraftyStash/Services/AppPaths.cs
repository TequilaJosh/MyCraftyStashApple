using System.IO;

namespace MyCraftyStash.Services;

/// <summary>
/// Single source of truth for where the app reads and writes data.
/// On every Apple/Windows target the per-app data directory
/// (<see cref="FileSystem.AppDataDirectory"/>) is a sandbox-safe, writable
/// location, so the SQLite database lives there. Mirrors the Windows desktop
/// app's AppPaths API (InventoryConnectionString) so the shared DbContext
/// works unchanged.
/// </summary>
public static class AppPaths
{
    public static string DataRoot => FileSystem.Current.AppDataDirectory;

    public static string InventoryDbPath => Path.Combine(DataRoot, "inventory.db");

    public static string InventoryConnectionString => $"Data Source={InventoryDbPath}";
}
