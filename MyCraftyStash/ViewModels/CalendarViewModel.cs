using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyCraftyStash.Models;
using MyCraftyStash.Services;

namespace MyCraftyStash.ViewModels;

/// <summary>
/// Month-grid calendar — clone of the desktop Calendar (user events only; the
/// desktop's Taylored-Expressions scraper overlays are omitted). Prev/next/
/// Today navigation, a 6×7 grid, a selected-day event list, and an inline
/// add/edit form with all-day/time, reminder, and colour.
/// </summary>
public partial class CalendarViewModel : ObservableObject
{
    private readonly CalendarService _service;

    public CalendarViewModel(CalendarService service)
    {
        _service = service;
        var now = DateTime.Today;
        CurrentYear = now.Year;
        CurrentMonth = now.Month;
    }

    [ObservableProperty] public partial int CurrentYear { get; set; }
    [ObservableProperty] public partial int CurrentMonth { get; set; }
    [ObservableProperty] public partial string CurrentMonthLabel { get; set; } = "";
    public ObservableCollection<CalendarDayCell> DayCells { get; } = new();

    [ObservableProperty] public partial CalendarDayCell? SelectedDay { get; set; }
    [ObservableProperty] public partial string SelectedDayLabel { get; set; } = "Select a day";
    [ObservableProperty] public partial string SelectedDayCountLabel { get; set; } = "";
    [ObservableProperty] public partial bool HasSelectedDay { get; set; }
    public ObservableCollection<CalendarEvent> SelectedDayEvents { get; } = new();
    [ObservableProperty] public partial bool SelectedDayEmpty { get; set; }

    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial string? StatusMessage { get; set; }

    // ── Form ──────────────────────────────────────────────────────────────────
    private CalendarEvent? _editing;
    [ObservableProperty] public partial bool IsFormOpen { get; set; }
    [ObservableProperty] public partial string FormHeading { get; set; } = "New Event";
    [ObservableProperty] public partial string? ErrorMessage { get; set; }
    [ObservableProperty] public partial string? EditTitle { get; set; }
    [ObservableProperty] public partial string? EditDescription { get; set; }
    [ObservableProperty] public partial DateTime EditDate { get; set; } = DateTime.Today;
    [ObservableProperty] public partial bool EditIsAllDay { get; set; } = true;
    public bool ShowTimePicker => !EditIsAllDay;
    partial void OnEditIsAllDayChanged(bool value) => OnPropertyChanged(nameof(ShowTimePicker));
    [ObservableProperty] public partial string EditTimeHour { get; set; } = "12";
    [ObservableProperty] public partial string EditTimeMinute { get; set; } = "00";
    [ObservableProperty] public partial string EditTimeAmPm { get; set; } = "PM";
    [ObservableProperty] public partial string EditReminderLabel { get; set; } = "1 Day Before";
    [ObservableProperty] public partial string EditColor { get; set; } = "#D61F26";

    public List<string> AmPmOptions { get; } = new() { "AM", "PM" };
    public List<string> ReminderOptions { get; } = new()
    {
        "At Event Time", "15 Minutes Before", "30 Minutes Before", "1 Hour Before",
        "2 Hours Before", "1 Day Before", "2 Days Before", "1 Week Before",
    };
    public List<string> ColorOptions { get; } = new()
    {
        "#D61F26", "#2563EB", "#16A34A", "#D97706", "#7C3AED", "#DB2777", "#0891B2", "#374151",
    };

    private static int ReminderToMinutes(string label) => label switch
    {
        "At Event Time" => 0,
        "15 Minutes Before" => 15,
        "30 Minutes Before" => 30,
        "1 Hour Before" => 60,
        "2 Hours Before" => 120,
        "1 Day Before" => 1440,
        "2 Days Before" => 2880,
        "1 Week Before" => 10080,
        _ => 1440,
    };

    private static string MinutesToReminder(int minutes) => minutes switch
    {
        0 => "At Event Time",
        15 => "15 Minutes Before",
        30 => "30 Minutes Before",
        60 => "1 Hour Before",
        120 => "2 Hours Before",
        1440 => "1 Day Before",
        2880 => "2 Days Before",
        10080 => "1 Week Before",
        _ => "1 Day Before",
    };

    // ═════════════════════════ LOAD / NAVIGATE ═════════════════════════

    [RelayCommand]
    public async Task LoadCalendar()
    {
        IsLoading = true;
        try
        {
            CurrentMonthLabel = new DateTime(CurrentYear, CurrentMonth, 1).ToString("MMMM yyyy");
            var events = await _service.GetForMonthAsync(CurrentYear, CurrentMonth);
            var byDay = events.GroupBy(e => e.EventDate.Date).ToDictionary(g => g.Key, g => g.ToList());

            DayCells.Clear();
            var first = new DateTime(CurrentYear, CurrentMonth, 1);
            int offset = (int)first.DayOfWeek;           // Sunday = 0
            var gridStart = first.AddDays(-offset);
            var today = DateTime.Today;

            for (int i = 0; i < 42; i++)
            {
                var date = gridStart.AddDays(i);
                var cell = new CalendarDayCell
                {
                    Date = date,
                    IsCurrentMonth = date.Month == CurrentMonth && date.Year == CurrentYear,
                    IsToday = date == today,
                };
                if (byDay.TryGetValue(date.Date, out var dayEvents))
                    foreach (var e in dayEvents) cell.Events.Add(e);
                cell.HasEvents = cell.Events.Count > 0;
                DayCells.Add(cell);
            }

            // Preserve/refresh the selected day if it's still in view.
            if (SelectedDay is not null)
            {
                var match = DayCells.FirstOrDefault(c => c.Date == SelectedDay.Date);
                if (match is not null) SelectDay(match);
                else ClearSelection();
            }
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task PreviousMonth()
    {
        var d = new DateTime(CurrentYear, CurrentMonth, 1).AddMonths(-1);
        CurrentYear = d.Year; CurrentMonth = d.Month;
        await LoadCalendar();
    }

    [RelayCommand]
    private async Task NextMonth()
    {
        var d = new DateTime(CurrentYear, CurrentMonth, 1).AddMonths(1);
        CurrentYear = d.Year; CurrentMonth = d.Month;
        await LoadCalendar();
    }

    [RelayCommand]
    private async Task GoToToday()
    {
        var today = DateTime.Today;
        CurrentYear = today.Year; CurrentMonth = today.Month;
        await LoadCalendar();
        var cell = DayCells.FirstOrDefault(c => c.Date == today);
        if (cell is not null) SelectDay(cell);
    }

    [RelayCommand]
    private void SelectDayCell(CalendarDayCell cell) => SelectDay(cell);

    private void SelectDay(CalendarDayCell cell)
    {
        foreach (var c in DayCells) c.IsSelected = false;
        cell.IsSelected = true;
        SelectedDay = cell;
        HasSelectedDay = true;
        SelectedDayLabel = cell.Date.ToString("dddd, MMMM d");
        SelectedDayEvents.Clear();
        foreach (var e in cell.Events.OrderBy(e => e.EventDateTime)) SelectedDayEvents.Add(e);
        SelectedDayCountLabel = $"{SelectedDayEvents.Count} event(s)";
        SelectedDayEmpty = SelectedDayEvents.Count == 0;
        IsFormOpen = false;
    }

    private void ClearSelection()
    {
        SelectedDay = null;
        HasSelectedDay = false;
        SelectedDayLabel = "Select a day";
        SelectedDayCountLabel = "";
        SelectedDayEvents.Clear();
        SelectedDayEmpty = false;
    }

    // ═════════════════════════ ADD / EDIT ═════════════════════════

    [RelayCommand]
    private void StartAdd()
    {
        _editing = null;
        FormHeading = "New Event";
        ErrorMessage = null;
        EditTitle = null;
        EditDescription = null;
        EditDate = SelectedDay?.Date ?? DateTime.Today;
        EditIsAllDay = true;
        EditTimeHour = "12"; EditTimeMinute = "00"; EditTimeAmPm = "PM";
        EditReminderLabel = "1 Day Before";
        EditColor = "#D61F26";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void StartEdit(CalendarEvent ev)
    {
        _editing = ev;
        FormHeading = "Edit Event";
        ErrorMessage = null;
        EditTitle = ev.Title;
        EditDescription = ev.Description;
        EditDate = ev.EventDate.Date;
        EditIsAllDay = ev.IsAllDay;
        if (ev.EventTime is TimeSpan t)
        {
            int h = t.Hours;
            EditTimeAmPm = h >= 12 ? "PM" : "AM";
            int h12 = h % 12; if (h12 == 0) h12 = 12;
            EditTimeHour = h12.ToString();
            EditTimeMinute = t.Minutes.ToString("00");
        }
        EditReminderLabel = MinutesToReminder(ev.ReminderMinutesBefore);
        EditColor = ev.Color ?? "#D61F26";
        IsFormOpen = true;
    }

    [RelayCommand]
    private void CancelForm() => IsFormOpen = false;

    [RelayCommand]
    private void SetColor(string color) => EditColor = color;

    [RelayCommand]
    private async Task SaveEvent()
    {
        if (string.IsNullOrWhiteSpace(EditTitle))
        {
            ErrorMessage = "Event title is required.";
            return;
        }

        TimeSpan? time = null;
        if (!EditIsAllDay)
        {
            int.TryParse(EditTimeHour, out int h);
            int.TryParse(EditTimeMinute, out int m);
            if (EditTimeAmPm == "PM" && h < 12) h += 12;
            if (EditTimeAmPm == "AM" && h == 12) h = 0;
            time = new TimeSpan(Math.Clamp(h, 0, 23), Math.Clamp(m, 0, 59), 0);
        }

        int minutes = ReminderToMinutes(EditReminderLabel);

        if (_editing is null)
        {
            await _service.AddAsync(new CalendarEvent
            {
                Title = EditTitle.Trim(),
                Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim(),
                EventDate = EditDate.Date,
                EventTime = time,
                IsAllDay = EditIsAllDay,
                ReminderMinutesBefore = minutes,
                Color = EditColor,
            });
            StatusMessage = $"Event \"{EditTitle.Trim()}\" added!";
        }
        else
        {
            _editing.Title = EditTitle.Trim();
            _editing.Description = string.IsNullOrWhiteSpace(EditDescription) ? null : EditDescription.Trim();
            _editing.EventDate = EditDate.Date;
            _editing.EventTime = time;
            _editing.IsAllDay = EditIsAllDay;
            _editing.ReminderMinutesBefore = minutes;
            _editing.Color = EditColor;
            _editing.ReminderDismissed = false;
            await _service.UpdateAsync(_editing);
            StatusMessage = $"Event \"{EditTitle.Trim()}\" updated!";
        }

        IsFormOpen = false;
        await LoadCalendar();
        _ = ClearStatusSoon();
    }

    [RelayCommand]
    private async Task DeleteEvent(CalendarEvent ev)
    {
        await _service.DeleteAsync(ev.Id);
        StatusMessage = $"Deleted \"{ev.Title}\".";
        await LoadCalendar();
        _ = ClearStatusSoon();
    }

    private async Task ClearStatusSoon()
    {
        var msg = StatusMessage;
        await Task.Delay(4000);
        if (StatusMessage == msg) StatusMessage = null;
    }
}
