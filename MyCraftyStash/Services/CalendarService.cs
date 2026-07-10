using Microsoft.EntityFrameworkCore;
using MyCraftyStash.Data;
using MyCraftyStash.Models;

namespace MyCraftyStash.Services;

/// <summary>Local data access for calendar events (calendar_events table).</summary>
public class CalendarService
{
    public async Task<List<CalendarEvent>> GetAllAsync()
    {
        using var db = new InventoryDbContext();
        return await db.CalendarEvents.AsNoTracking()
            .OrderBy(e => e.EventDate).ToListAsync();
    }

    /// <summary>Events whose date falls within the given month.</summary>
    public async Task<List<CalendarEvent>> GetForMonthAsync(int year, int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        using var db = new InventoryDbContext();
        return await db.CalendarEvents.AsNoTracking()
            .Where(e => e.EventDate >= start && e.EventDate < end)
            .OrderBy(e => e.EventDate).ToListAsync();
    }

    public async Task<CalendarEvent?> GetAsync(int id)
    {
        using var db = new InventoryDbContext();
        return await db.CalendarEvents.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<int> AddAsync(CalendarEvent ev)
    {
        using var db = new InventoryDbContext();
        ev.CreatedAt = DateTime.Now;
        db.CalendarEvents.Add(ev);
        await db.SaveChangesAsync();
        return ev.Id;
    }

    public async Task UpdateAsync(CalendarEvent ev)
    {
        using var db = new InventoryDbContext();
        var e = await db.CalendarEvents.FindAsync(ev.Id);
        if (e is null) return;
        e.Title = ev.Title;
        e.Description = ev.Description;
        e.EventDate = ev.EventDate;
        e.ReminderMinutesBefore = ev.ReminderMinutesBefore;
        e.Color = ev.Color;
        e.IsAllDay = ev.IsAllDay;
        e.ReminderDismissed = ev.ReminderDismissed;
        e.UpdatedAt = DateTime.Now;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        using var db = new InventoryDbContext();
        var e = await db.CalendarEvents.FindAsync(id);
        if (e is not null) { db.CalendarEvents.Remove(e); await db.SaveChangesAsync(); }
    }
}
