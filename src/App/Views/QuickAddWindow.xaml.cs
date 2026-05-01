using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;
using DomainPage = WindowsTrayTasks.Domain.Page;

namespace WindowsTrayTasks.Views;

public partial class QuickAddWindow : Window
{
    private readonly Database _db;
    private readonly IClock _clock;
    private readonly EntityFactory _entities;
    private readonly Action _onSaved;

    public QuickAddWindow(Database db, IClock clock, EntityFactory entities, Action onSaved)
    {
        _db = db;
        _clock = clock;
        _entities = entities;
        _onSaved = onSaved;
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ReloadPages();
        ReloadHeadings();
        BuildPresetButtons();
        TitleBox.Focus();
        HintText.Text = "Save: Enter · Cancel: Esc · Reminder: +5m, +2h, tomorrow 9, next workday";
    }

    private void BuildPresetButtons()
    {
        ReminderPresets.Children.Clear();
        foreach (var preset in Domain.QuickPresets.All)
        {
            var btn = new System.Windows.Controls.Button
            {
                Content = preset.Label,
                Tag = preset.Shorthand,
                Padding = new System.Windows.Thickness(5, 1, 5, 1),
                Margin = new System.Windows.Thickness(0, 0, 4, 2),
                FontSize = 10,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC)),
            };
            btn.Click += (_, _) => ReminderBox.Text = (string)btn.Tag;
            ReminderPresets.Children.Add(btn);
        }
    }

    public void Reset()
    {
        TitleBox.Text = "";
        ReminderBox.Text = "";
        ReloadPages();
        ReloadHeadings();
        TitleBox.Focus();
    }

    private void ReloadHeadings()
    {
        var page = PageCombo.SelectedItem as DomainPage;
        var pageId = page?.Id ?? _db.GetActivePageId();
        var none = new Heading { Id = Guid.Empty, Title = "(No heading)" };
        var list = new List<Heading> { none };
        list.AddRange(_db.GetHeadings(pageId));
        var prev = HeadingCombo.SelectedItem as Heading;
        HeadingCombo.ItemsSource = list;
        HeadingCombo.SelectedItem = list.FirstOrDefault(h => prev != null && h.Id == prev.Id) ?? none;
    }

    private void ReloadPages()
    {
        var pages = _db.GetPages();
        var activePageId = _db.GetActivePageId();
        PageCombo.ItemsSource = pages;
        PageCombo.SelectedItem = pages.FirstOrDefault(p => p.Id == activePageId) ?? pages.FirstOrDefault();
    }

    public void ShowQuickAdd()
    {
        Reset();
        if (!IsVisible) Show();
        Activate();
        TitleBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => HideToBackground();

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TitleBox.Text)) { TitleBox.Focus(); return; }

        DateTime? reminderUtc = null;
        var reminderText = ReminderBox.Text.Trim();
        if (!string.IsNullOrEmpty(reminderText))
        {
            reminderUtc = DateInputParser.Parse(reminderText, _clock.UtcNow);
            if (reminderUtc is null)
            {
                MessageBox.Show(this, "Reminder time could not be parsed. Examples: 2026-05-01 14:30, +5m, +2h, +1d", "Quick Add");
                ReminderBox.Focus();
                return;
            }
        }

        var heading = HeadingCombo.SelectedItem as Heading;
        var page = PageCombo.SelectedItem as DomainPage;
        var pageId = page?.Id ?? _db.GetActivePageId();
        var task = _entities.CreateTask(
            pageId,
            TitleBox.Text.Trim(),
            heading is null || heading.Id == Guid.Empty ? null : heading.Id,
            TaskState.Action);
        _db.SaveTask(task);

        if (reminderUtc.HasValue)
        {
            var rem = _entities.CreateReminder(task.Id, reminderUtc.Value);
            _db.SaveReminder(rem);
        }

        _onSaved();

        if (KeepOpenCheck.IsChecked == true)
        {
            Reset();
        }
        else
        {
            HideToBackground();
        }
    }

    private void HideToBackground()
    {
        Hide();
    }

    private void PageCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (HeadingCombo is not null) ReloadHeadings();
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            HideToBackground();
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }
}
