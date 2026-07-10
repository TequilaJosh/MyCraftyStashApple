using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>Local data access for mailing contacts (address_book table).</summary>
public class AddressBookService
{
    public async Task<List<AddressBookEntry>> GetAllAsync(string? search = null)
    {
        using var db = new InventoryDbContext();
        IQueryable<AddressBookEntry> q = db.AddressBookEntries.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(e =>
                EF.Functions.Like(e.FirstName, $"%{s}%") ||
                (e.LastName != null && EF.Functions.Like(e.LastName, $"%{s}%")) ||
                (e.City != null && EF.Functions.Like(e.City, $"%{s}%")) ||
                (e.Email != null && EF.Functions.Like(e.Email, $"%{s}%")));
        }
        return await q.OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync();
    }

    public async Task<AddressBookEntry?> GetAsync(int id)
    {
        using var db = new InventoryDbContext();
        return await db.AddressBookEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<int> AddAsync(AddressBookEntry entry)
    {
        using var db = new InventoryDbContext();
        entry.CreatedAt = DateTime.Now;
        db.AddressBookEntries.Add(entry);
        await db.SaveChangesAsync();
        return entry.Id;
    }

    public async Task UpdateAsync(AddressBookEntry entry)
    {
        using var db = new InventoryDbContext();
        var e = await db.AddressBookEntries.FindAsync(entry.Id);
        if (e is null) return;
        e.FirstName = entry.FirstName;
        e.LastName = entry.LastName;
        e.AddressLine1 = entry.AddressLine1;
        e.AddressLine2 = entry.AddressLine2;
        e.City = entry.City;
        e.State = entry.State;
        e.ZipCode = entry.ZipCode;
        e.Country = entry.Country;
        e.Phone = entry.Phone;
        e.Email = entry.Email;
        e.Notes = entry.Notes;
        e.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new InventoryDbContext();
        var e = await db.AddressBookEntries.FindAsync(id);
        if (e is not null) { db.AddressBookEntries.Remove(e); await db.SaveChangesAsync(); }
    }
}
