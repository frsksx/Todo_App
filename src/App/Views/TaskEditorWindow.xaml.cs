using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;

namespace WindowsTrayTasks.Views;

public partial class TaskEditorWindow : Window
{
    private readonly Database _db;
    private readonly IClock _clock;
    private readonly EntityFactory _entities;
    private readonly TaskItem _task;
    private readonly bool _focusReminder;

    public TaskEditorWindow(Database db, IClock clock, EntityFactory entities, TaskItem task, bool focusReminder = false)
    {
        _db = db;
        _clock = clock;
        _entities = entities;
        _task = task;
        _focusReminder = focusReminder;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        StateCombo.ItemsSource = Enum.GetValues<TaskState>();
        StateCombo.SelectedItem = _task.State;

        var headings = _db.GetHeadings(_task.PageId);
        var withNone = new List<Heading> { new() { Id = Guid.Empty, Title = "(No heading)" } };
        withNone.AddRange(headings);
        HeadingCombo.ItemsSource = withNone;
        HeadingCombo.SelectedItem = withNone.FirstOrDefault(h => h.Id == (_task.HeadingId ?? Guid.Empty)) ?? withNone[0];

        AutoSnoozeCombo.ItemsSource = new[] { 1, 5, 10, 15, 30, 60 };
        var existingReminder = _db.GetReminderForTask(_task.Id);
        AutoSnoozeCombo.SelectedItem = existingReminder?.AutoSnoozeIntervalMinutes ?? 5;

        TitleBox.Text = _task.Title;
        NotesBox.Text = _task.Notes ?? "";
        StartBox.Text = FormatLocal(_task.StartAt);
        DueBox.Text = FormatLocal(_task.DueAt);
        ReminderBox.Text = FormatLocal(existingReminder?.NextFireAt ?? existingReminder?.FireAt);

        HintText.Text = "Save: Enter · Cancel: Esc · Reminder shorthand: +5m, +2h, +1d";

        if (_focusReminder) ReminderBox.Focus();
        else { TitleBox.Focus(); TitleBox.SelectAll(); }
    }

    private static string FormatLocal(DateTime? utc)
        => utc is null ? "" : utc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private DateTime? ParseDateTimeInput(string text)
        => DateInputParser.Parse(text, _clock.UtcNow);

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text))
        {
            MessageBox.Show(this, "Title is required.", "Tasks", MessageBoxButton.OK, MessageBoxImage.Information);
            TitleBox.Focus();
            return;
        }

        var startInput = StartBox.Text.Trim();
        var dueInput = DueBox.Text.Trim();
        var reminderInput = ReminderBox.Text.Trim();

        DateTime? start = null, due = null, reminder = null;
        try
        {
            start = ParseDateTimeInput(startInput);
            due = ParseDateTimeInput(dueInput);
            reminder = ParseDateTimeInput(reminderInput);
        }
        catch { /* fall through to validation */ }

        if (!string.IsNullOrEmpty(startInput) && start is null)
        { MessageBox.Show(this, "Start date could not be parsed.", "Tasks"); StartBox.Focus(); return; }
        if (!string.IsNullOrEmpty(dueInput) && due is null)
        { MessageBox.Show(this, "Due date could not be parsed.", "Tasks"); DueBox.Focus(); return; }
        if (!string.IsNullOrEmpty(reminderInput) && reminder is null)
        { MessageBox.Show(this, "Reminder time could not be parsed.", "Tasks"); ReminderBox.Focus(); return; }

        _task.Title = TitleBox.Text.Trim();
        _task.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text;
        _task.State = (TaskState)(StateCombo.SelectedItem ?? TaskState.Action);
        _task.StartAt = start;
        _task.DueAt = due;

        var heading = HeadingCombo.SelectedItem as Heading;
        _task.HeadingId = (heading is null || heading.Id == Guid.Empty) ? null : heading.Id;

        if (_task.State == TaskState.Done && _task.CompletedAt is null) _task.CompletedAt = _clock.UtcNow;
        if (_task.State != TaskState.Done) _task.CompletedAt = null;

        if (_task.SortOrder == 0)
            _task.SortOrder = EntityFactory.DefaultSortOrder(_clock.UtcNow);

        _db.SaveTask(_task);

        var existing = _db.GetReminderForTask(_task.Id);
        if (reminder is null)
        {
            if (existing is not null)
            {
                existing.Enabled = false;
                existing.Status = ReminderStatus.Disabled;
                _db.SaveReminder(existing);
            }
        }
        else
        {
            var rem = existing ?? _entities.CreateReminder(_task.Id, reminder.Value);
            rem.FireAt = reminder.Value;
            rem.NextFireAt = reminder.Value;
            rem.Enabled = true;
            rem.Status = ReminderStatus.Active;
            rem.AutoSnoozeIntervalMinutes = (int)(AutoSnoozeCombo.SelectedItem ?? 5);
            rem.AutoSnoozeEnabled = true;
            _db.SaveReminder(rem);
        }

        DialogResult = true;
        Close();
    }
}
