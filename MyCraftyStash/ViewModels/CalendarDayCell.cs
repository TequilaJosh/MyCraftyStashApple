using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MyCraftyStash.Models;

namespace MyCraftyStash.ViewModels;

/// <summary>One cell in the month grid (day number + that day's events).</summary>
public partial class CalendarDayCell : ObservableObject
{
    public DateTime Date { get; set; }
    public bool IsCurrentMonth { get; set; }
    public int DayNumber => Date.Day;

    [ObservableProperty] public partial bool IsToday { get; set; }
    [ObservableProperty] public partial bool IsSelected { get; set; }

    public ObservableCollection<CalendarEvent> Events { get; } = new();
    [ObservableProperty] public partial bool HasEvents { get; set; }

    // Muted day number for other-month cells.
    public bool IsOtherMonth => !IsCurrentMonth;
}
