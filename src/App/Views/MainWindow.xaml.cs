using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;
using WindowsTrayTasks.Reminders;
using DomainPage = WindowsTrayTasks.Domain.Page;

namespace WindowsTrayTasks.Views;

public sealed class TaskRowVm
{
    public bool IsHeading { get; init; }
    public bool IsTask => !IsHeading;

    public Guid? HeadingId { get; init; }
    public string HeadingTitle { get; init; } = "(No heading)";
    public int HeadingCount { get; init; }
    public bool HeadingCollapsed { get; init; }
    public bool IsFocusedHeading { get; init; }
    public string HeadingChevron => HeadingId is null ? "" : HeadingCollapsed ? "▶" : "▾";
    public string HeadingDisplay => $"{HeadingTitle} ({HeadingCount})";
    public string HeadingFocusText => IsFocusedHeading ? "Focused" : "";

    public Guid TaskId { get; init; }
    public string Title { get; init; } = "";
    public string EditTitle { get; set; } = "";
    public bool IsEditing { get; init; }
    public Visibility TitleTextVisibility => IsEditing ? Visibility.Collapsed : Visibility.Visible;
    public Visibility TitleEditorVisibility => IsEditing ? Visibility.Visible : Visibility.Collapsed;

    public TaskState State { get; init; }
    public string StateLabel => State.ToString();
    public string StateGlyph => State switch
    {
        TaskState.Inbox => "○",
        TaskState.Next => "○",
        TaskState.Waiting => "Ⅱ",
        TaskState.Scheduled => "◷",
        TaskState.Someday => "•",
        TaskState.Done => "✓",
        TaskState.Archived => "□",
        _ => "○",
    };

    public DateTime? StartAt { get; init; }
    public DateTime? DueAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? Notes { get; init; }
    public DateTime? ReminderNextFireAt { get; init; }
    public int? ReminderAutoSnoozeMinutes { get; init; }
    public IReadOnlyList<Tag> Tags { get; init; } = [];
    public bool ReminderActive { get; init; }
    public bool ReminderOverdue { get; init; }
    public bool IsExpanded { get; init; }
    public string ExpandGlyph => IsExpanded ? "▾" : "›";
    public Visibility ExpandedVisibility => IsExpanded ? Visibility.Visible : Visibility.Collapsed;

    public string DueText => DueAt is null ? "" : $"due {ToLocal(DueAt.Value)}";
    public string NotesText => string.IsNullOrWhiteSpace(Notes) ? "No notes" : Notes!;
    public string DetailText
    {
        get
        {
            var parts = new List<string>();
            if (StartAt is { } start) parts.Add($"start {ToLocal(start)}");
            if (DueAt is { } due) parts.Add($"due {ToLocal(due)}");
            if (CompletedAt is { } done) parts.Add($"done: {ToLocal(done)}");
            if (Tags.Count > 0) parts.Add(string.Join(" ", Tags.Select(t => "@" + t.DisplayName)));
            if (ReminderActive && ReminderNextFireAt is { } next)
            {
                var repeat = ReminderAutoSnoozeMinutes is { } minutes ? $" · repeats every {minutes}m" : "";
                parts.Add($"{(ReminderOverdue ? "overdue" : "reminder")} {ToLocal(next)}{repeat}");
            }
            return parts.Count == 0 ? "No dates" : string.Join(" · ", parts);
        }
    }

    public string ReminderText
    {
        get
        {
            if (!ReminderActive || ReminderNextFireAt is null) return "";
            var local = ToLocal(ReminderNextFireAt.Value);
            return ReminderOverdue ? $"⏰ overdue · {local}" : $"⏰ {local}";
        }
    }

    public Brush TitleBrush => State == TaskState.Done
        ? new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99))
        : new SolidColorBrush(Color.FromRgb(0x22, 0x22, 0x22));

    public Brush ReminderBrush => ReminderOverdue
        ? new SolidColorBrush(Color.FromRgb(0xC8, 0x3C, 0x3C))
        : new SolidColorBrush(Color.FromRgb(0x55, 0x77, 0xBB));

    public Brush StateBrush => State switch
    {
        TaskState.Inbox => new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
        TaskState.Next => new SolidColorBrush(Color.FromRgb(0x3C, 0x78, 0xDC)),
        TaskState.Waiting => new SolidColorBrush(Color.FromRgb(0xF0, 0xAA, 0x1E)),
        TaskState.Scheduled => new SolidColorBrush(Color.FromRgb(0x6B, 0x7F, 0xC8)),
        TaskState.Someday => new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
        TaskState.Done => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
        TaskState.Archived => new SolidColorBrush(Color.FromRgb(0x77, 0x77, 0x77)),
        _ => Brushes.Gray,
    };

    public string RowKey => IsHeading ? $"h:{HeadingId?.ToString() ?? "none"}" : $"t:{TaskId}";

    private static string ToLocal(DateTime utc)
    {
        var local = utc.ToLocalTime();
        var today = DateTime.Today;
        if (local.Date == today) return local.ToString("HH:mm");
        if (local.Date == today.AddDays(1)) return $"tomorrow {local:HH:mm}";
        if (local.Date == today.AddDays(-1)) return $"yesterday {local:HH:mm}";
        if (local.Date < today.AddDays(7) && local.Date > today) return local.ToString("ddd HH:mm");
        return local.ToString("yyyy-MM-dd HH:mm");
    }
}

public partial class MainWindow : Window
{
    private readonly Database _db;
    private readonly ReminderEngine _reminders;
    private readonly IClock _clock;
    private readonly EntityFactory _entities;
    private readonly Action _quickAdd;

    private readonly HashSet<Guid> _expandedTaskIds = new();
    private List<TaskRowVm> _rows = new();
    private List<DomainPage> _pages = new();
    private Guid _activePageId;
    private string _filterMode = "All";
    private DateFilterBucket _dateBucket = DateFilterBucket.All;
    private string _searchText = "";
    private bool _showEmptyHeadings;
    private bool _includeDone;
    private bool _untaggedOnly;
    private readonly HashSet<string> _selectedTags = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _focusedHeadingId;
    private Guid? _editingTaskId;
    private Point _dragStart;
    private Guid? _dragSourceTaskId;

    public MainWindow(Database db, ReminderEngine reminders, IClock clock, EntityFactory entities, Action quickAdd)
    {
        _db = db;
        _reminders = reminders;
        _clock = clock;
        _entities = entities;
        _quickAdd = quickAdd;
        _activePageId = _db.GetActivePageId();
        InitializeComponent();

        FilterCombo.ItemsSource = new[] { "All", "Inbox", "Next", "Waiting", "Scheduled", "Someday", "Archived" };
        FilterCombo.SelectedIndex = 0;
        DateFilterCombo.ItemsSource = new[] { "All dates", "Today", "Tomorrow", "This week", "Upcoming", "Overdue", "No date" };
        DateFilterCombo.SelectedIndex = 0;

        Loaded += (_, _) => { Refresh(); SearchBox.Focus(); };
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;

        _reminders.StateChanged += () => Dispatcher.BeginInvoke(Refresh);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveCurrentPageViewState();
        e.Cancel = true;
        Hide();
    }

    public void ToggleVisibility()
    {
        if (IsVisible)
        {
            SaveCurrentPageViewState();
            Hide();
        }
        else
        {
            Show();
            Activate();
            SearchBox.Focus();
            Refresh();
        }
    }

    public void Refresh()
    {
        var selectedKey = Selected?.RowKey;
        RefreshPages();
        var tasks = _db.GetTasks(includeArchived: _filterMode == "Archived", pageId: _activePageId);
        var headings = _db.GetHeadings(_activePageId).OrderBy(h => h.SortOrder).ThenBy(h => h.Title).ToList();
        var reminders = _db.GetActiveReminders().ToDictionary(r => r.TaskId, r => r);
        var now = _clock.UtcNow;

        var filtered = ApplyCurrentFilters(tasks, reminders, now).ToList();

        _rows = BuildRows(headings, filtered, reminders, now);
        TaskList.ItemsSource = _rows;
        RenderTagBar(tasks);

        if (selectedKey is not null)
        {
            TaskList.SelectedItem = _rows.FirstOrDefault(r => r.RowKey == selectedKey);
        }

        var visibleActiveCount = _rows.Count(r => r.IsTask && r.State is not TaskState.Done and not TaskState.Archived);
        var pageName = _pages.FirstOrDefault(p => p.Id == _activePageId)?.Name ?? "Tasks";
        var pieces = new List<string> { $"{visibleActiveCount} active actions", pageName };
        var snap = _reminders.Snapshot();
        if (snap.OverdueCount > 0) pieces.Add($"{snap.OverdueCount} overdue");
        if (_reminders.IsPaused) pieces.Add("reminders paused");
        if (_focusedHeadingId.HasValue)
        {
            var focusTitle = _rows.FirstOrDefault(r => r.IsHeading && r.IsFocusedHeading)?.HeadingTitle ?? "heading";
            pieces.Add($"Focused: {focusTitle} · Esc to exit");
        }
        StatusText.Text = string.Join(" · ", pieces);
    }

    private void RefreshPages()
    {
        _pages = _db.GetPages();
        if (_pages.All(p => p.Id != _activePageId))
        {
            _activePageId = _db.GetActivePageId();
        }

        PageTabs.Children.Clear();
        foreach (var page in _pages)
        {
            var button = new Button
            {
                Content = page.Name,
                DataContext = page,
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 4, 0),
                FontWeight = page.Id == _activePageId ? FontWeights.SemiBold : FontWeights.Normal,
                BorderThickness = page.Id == _activePageId ? new Thickness(0, 0, 0, 2) : new Thickness(0),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x86, 0xE8)),
                Background = Brushes.Transparent,
            };
            button.Click += PageTab_Click;
            button.ContextMenu = BuildPageContextMenu(page);
            PageTabs.Children.Add(button);
        }
    }

    private ContextMenu BuildPageContextMenu(DomainPage page)
    {
        var menu = new ContextMenu();

        var rename = new MenuItem { Header = "Rename", DataContext = page };
        rename.Click += RenamePage_Click;
        menu.Items.Add(rename);

        var moveLeft = new MenuItem { Header = "Move Left", DataContext = page, IsEnabled = _pages.IndexOf(page) > 0 };
        moveLeft.Click += MovePageLeft_Click;
        menu.Items.Add(moveLeft);

        var moveRight = new MenuItem { Header = "Move Right", DataContext = page, IsEnabled = _pages.IndexOf(page) < _pages.Count - 1 };
        moveRight.Click += MovePageRight_Click;
        menu.Items.Add(moveRight);

        menu.Items.Add(new Separator());

        var delete = new MenuItem { Header = "Delete", DataContext = page, IsEnabled = !page.IsDefault };
        delete.Click += DeletePage_Click;
        menu.Items.Add(delete);

        return menu;
    }

    private IEnumerable<TaskItem> ApplyCurrentFilters(IEnumerable<TaskItem> tasks, IReadOnlyDictionary<Guid, Reminder> reminders, DateTime now)
        => TaskFilter.Apply(tasks, reminders, new ComposedFilterCriteria(
            PageId: _activePageId,
            StateMode: _filterMode,
            IncludeDone: _includeDone,
            DateBucket: _dateBucket,
            SearchText: _searchText,
            TagNames: _selectedTags,
            UntaggedOnly: _untaggedOnly,
            FocusedHeadingId: _focusedHeadingId,
            NowUtc: now));

    private List<TaskRowVm> BuildRows(
        IReadOnlyList<Heading> headings,
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyDictionary<Guid, Reminder> reminders,
        DateTime now)
    {
        var rows = new List<TaskRowVm>();
        var tasksByHeading = tasks
            .GroupBy(t => HeadingKey(t.HeadingId))
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.SortOrder).ThenBy(t => t.Title).ToList());

        foreach (var heading in headings)
        {
            if (_focusedHeadingId.HasValue && _focusedHeadingId.Value != heading.Id) continue;
            tasksByHeading.TryGetValue(HeadingKey(heading.Id), out var headingTasks);
            headingTasks ??= new List<TaskItem>();
            if (headingTasks.Count == 0 && !_showEmptyHeadings) continue;

            rows.Add(CreateHeadingRow(heading.Id, heading.Title, headingTasks.Count, heading.Collapsed));
            if (!heading.Collapsed)
            {
                rows.AddRange(headingTasks.Select(t => CreateTaskRow(t, heading.Title, reminders, now)));
            }
        }

        var noHeadingTasks = tasksByHeading.TryGetValue(HeadingKey(null), out var looseTasks)
            ? looseTasks
            : new List<TaskItem>();
        if (noHeadingTasks.Count > 0 && (!_focusedHeadingId.HasValue || _focusedHeadingId.Value == Guid.Empty))
        {
            rows.Add(CreateHeadingRow(null, "(No heading)", noHeadingTasks.Count, collapsed: false));
            rows.AddRange(noHeadingTasks.Select(t => CreateTaskRow(t, "(No heading)", reminders, now)));
        }

        return rows;
    }

    private TaskRowVm CreateHeadingRow(Guid? headingId, string title, int count, bool collapsed)
        => new()
        {
            IsHeading = true,
            HeadingId = headingId,
            HeadingTitle = title,
            HeadingCount = count,
            HeadingCollapsed = collapsed,
            IsFocusedHeading = _focusedHeadingId.HasValue && _focusedHeadingId.Value == (headingId ?? Guid.Empty),
        };

    private TaskRowVm CreateTaskRow(TaskItem task, string headingTitle, IReadOnlyDictionary<Guid, Reminder> reminders, DateTime now)
    {
        reminders.TryGetValue(task.Id, out var rem);
        return new TaskRowVm
        {
            IsHeading = false,
            HeadingId = task.HeadingId,
            HeadingTitle = headingTitle,
            TaskId = task.Id,
            Title = task.Title,
            EditTitle = task.Title,
            IsEditing = _editingTaskId == task.Id,
            State = task.State,
            StartAt = task.StartAt,
            DueAt = task.DueAt,
            CompletedAt = task.CompletedAt,
            Notes = task.Notes,
            Tags = task.Tags,
            ReminderActive = rem is not null,
            ReminderNextFireAt = rem?.NextFireAt,
            ReminderAutoSnoozeMinutes = rem?.AutoSnoozeIntervalMinutes,
            ReminderOverdue = rem?.NextFireAt is { } next && next <= now,
            IsExpanded = _expandedTaskIds.Contains(task.Id),
        };
    }

    private bool HeadingMatchesFocus(Guid? headingId)
        => _focusedHeadingId is { } focused && focused == HeadingKey(headingId);

    private static Guid HeadingKey(Guid? headingId) => headingId ?? Guid.Empty;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = SearchBox.Text;
        Refresh();
    }

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (string.IsNullOrEmpty(SearchBox.Text)) { Hide(); }
            else { SearchBox.Clear(); }
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            TaskList.Focus();
            if (TaskList.SelectedIndex < 0 && _rows.Count > 0)
                TaskList.SelectedIndex = 0;
            e.Handled = true;
        }
    }

    private void FilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _filterMode = (string?)FilterCombo.SelectedItem ?? "All";
        Refresh();
    }

    private void DateFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _dateBucket = ((string?)DateFilterCombo.SelectedItem) switch
        {
            "Today" => DateFilterBucket.Today,
            "Tomorrow" => DateFilterBucket.Tomorrow,
            "This week" => DateFilterBucket.ThisWeek,
            "Upcoming" => DateFilterBucket.Upcoming,
            "Overdue" => DateFilterBucket.Overdue,
            "No date" => DateFilterBucket.NoDate,
            _ => DateFilterBucket.All,
        };
        Refresh();
    }

    private void ShowEmptyHeadingsCheck_Changed(object sender, RoutedEventArgs e)
    {
        _showEmptyHeadings = ShowEmptyHeadingsCheck.IsChecked == true;
        Refresh();
    }

    private void ShowDoneCheck_Changed(object sender, RoutedEventArgs e)
    {
        _includeDone = ShowDoneCheck.IsChecked == true;
        Refresh();
    }

    private void NewTask_Click(object sender, RoutedEventArgs e) => _quickAdd();

    private void RenderTagBar(IReadOnlyList<TaskItem> pageTasks)
    {
        TagBar.Children.Clear();

        var untaggedCount = pageTasks.Count(t => t.Tags.Count == 0 && t.State != TaskState.Archived);
        TagBar.Children.Add(CreateTagButton("Untagged", $"Untagged ({untaggedCount})", _untaggedOnly));
        TagBar.Children.Add(CreateTagButton("All", "All", !_untaggedOnly && _selectedTags.Count == 0));

        var counts = _db.GetTagTaskCounts(_activePageId);
        foreach (var tag in _db.GetTags(_activePageId))
        {
            counts.TryGetValue(tag.Id, out var count);
            TagBar.Children.Add(CreateTagButton(tag.Name, $"@{tag.DisplayName} ({count})", _selectedTags.Contains(tag.Name)));
        }
    }

    private Button CreateTagButton(string key, string text, bool selected)
    {
        var button = new Button
        {
            Content = text,
            DataContext = key,
            Padding = new Thickness(6, 1, 6, 1),
            Margin = new Thickness(0, 0, 4, 2),
            FontSize = 11,
            Background = selected ? new SolidColorBrush(Color.FromRgb(0xE3, 0xF0, 0xFF)) : Brushes.Transparent,
            BorderBrush = selected ? new SolidColorBrush(Color.FromRgb(0x8D, 0xB8, 0xE8)) : new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
        };
        button.Click += TagButton_Click;
        return button;
    }

    private void TagButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not string key) return;
        if (key == "All")
        {
            _untaggedOnly = false;
            _selectedTags.Clear();
        }
        else if (key == "Untagged")
        {
            _untaggedOnly = !_untaggedOnly;
            if (_untaggedOnly) _selectedTags.Clear();
        }
        else
        {
            _untaggedOnly = false;
            var exclusive = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (exclusive) _selectedTags.Clear();
            if (!_selectedTags.Add(key)) _selectedTags.Remove(key);
        }
        Refresh();
    }

    private void PageTab_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DomainPage page) return;
        SetActivePage(page);
    }

    private void SetActivePage(DomainPage page)
    {
        SaveCurrentPageViewState();
        _activePageId = page.Id;
        _db.SaveActivePageId(_activePageId);
        _selectedTags.Clear();
        _untaggedOnly = false;
        RestorePageViewState(page);
        Refresh();
    }

    private void RenamePage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DomainPage page) return;
        var prompt = new SimplePromptWindow("Rename page", "Page name:") { Owner = this };
        if (prompt.ShowDialog() != true || string.IsNullOrWhiteSpace(prompt.Result)) return;
        page.Name = prompt.Result.Trim();
        try
        {
            _db.SavePage(page);
            Refresh();
        }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            MessageBox.Show(this, "A page with that name already exists.", "Pages", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void MovePageLeft_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DomainPage page) MovePage(page, -1);
    }

    private void MovePageRight_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DomainPage page) MovePage(page, 1);
    }

    private void MovePage(DomainPage page, int delta)
    {
        var index = _pages.FindIndex(p => p.Id == page.Id);
        var targetIndex = index + delta;
        if (index < 0 || targetIndex < 0 || targetIndex >= _pages.Count) return;
        var target = _pages[targetIndex];
        (page.SortOrder, target.SortOrder) = (target.SortOrder, page.SortOrder);
        _db.SavePage(page);
        _db.SavePage(target);
        Refresh();
    }

    private void DeletePage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DomainPage page || page.IsDefault) return;
        if (_db.GetHeadings(page.Id).Count > 0 || _db.GetTasks(includeArchived: true, pageId: page.Id).Count > 0)
        {
            MessageBox.Show(this, "Only empty pages can be deleted. Move or delete the page's content first.", "Pages", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        page.DeletedAt = _clock.UtcNow;
        _db.SavePage(page);
        if (_activePageId == page.Id)
        {
            SetActivePage(_db.GetDefaultPage());
        }
        else
        {
            Refresh();
        }
    }

    private void AddPage_Click(object sender, RoutedEventArgs e)
    {
        var baseName = "New Page";
        var existing = _db.GetPages().Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = baseName;
        var n = 2;
        while (existing.Contains(name))
        {
            name = $"{baseName} {n++}";
        }

        var page = _entities.CreatePage(name);
        _db.SavePage(page);
        SaveCurrentPageViewState();
        _activePageId = page.Id;
        _db.SaveActivePageId(page.Id);
        RestorePageViewState(page);
        Refresh();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsInlineEditorFocused()) return;
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        if (e.Key == Key.Escape && !SearchBox.IsKeyboardFocusWithin)
        {
            if (_focusedHeadingId.HasValue)
            {
                _focusedHeadingId = null;
                Refresh();
            }
            else
            {
                Hide();
            }
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.F || e.Key == Key.Oem2 && !ctrl)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.N)
        {
            _quickAdd();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.H)
        {
            CreateHeading();
            e.Handled = true;
            return;
        }

        var alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
        if (ctrl && e.Key == Key.Tab)
        {
            SwitchPage((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? -1 : 1);
            e.Handled = true;
            return;
        }

        if (ctrl && alt)
        {
            var number = NumberFromKey(e.Key);
            if (number is >= 1 and <= 9)
            {
                JumpToPage(number.Value - 1);
                e.Handled = true;
            }
        }
    }

    private TaskRowVm? Selected => TaskList.SelectedItem as TaskRowVm;

    private void TaskList_KeyDown(object sender, KeyEventArgs e)
    {
        if (IsInlineEditorFocused()) return;
        var sel = Selected;
        if (sel is null) return;

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

        if (ctrl && shift)
        {
            if (sel.IsTask && (e.Key == Key.Up || e.Key == Key.Down))
            {
                MoveTaskWithinHeading(sel.TaskId, e.Key == Key.Up ? -1 : 1);
                e.Handled = true;
                return;
            }

            if (sel.IsTask && (e.Key == Key.Left || e.Key == Key.Right))
            {
                MoveTaskToAdjacentHeading(sel.TaskId, e.Key == Key.Left ? -1 : 1);
                e.Handled = true;
                return;
            }

            if (sel.IsHeading && (e.Key == Key.Up || e.Key == Key.Down))
            {
                MoveHeading(sel.HeadingId, e.Key == Key.Up ? -1 : 1);
                e.Handled = true;
                return;
            }
        }

        if (ctrl && e.Key == Key.Enter && sel.IsTask)
        {
            EditTask(sel.TaskId);
            e.Handled = true;
            return;
        }

        if ((e.Key == Key.Enter || e.Key == Key.F2) && sel.IsTask)
        {
            BeginInlineEdit(sel.TaskId);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && sel.IsTask)
        {
            _db.DeleteTask(sel.TaskId);
            _db.DeleteRemindersForTask(sel.TaskId);
            Refresh();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Space)
        {
            if (sel.IsHeading)
            {
                ToggleHeadingFocus(sel.HeadingId);
            }
            else
            {
                ToggleExpanded(sel.TaskId);
            }
            e.Handled = true;
            return;
        }

        if (sel.IsTask && e.Key >= Key.D1 && e.Key <= Key.D7)
        {
            SetState(sel.TaskId, (TaskState)(e.Key - Key.D1 + 1));
            e.Handled = true;
            return;
        }
        if (sel.IsTask && e.Key >= Key.NumPad1 && e.Key <= Key.NumPad7)
        {
            SetState(sel.TaskId, (TaskState)(e.Key - Key.NumPad1 + 1));
            e.Handled = true;
            return;
        }

        if (sel.IsTask && ctrl && e.Key == Key.D)
        {
            EditTask(sel.TaskId, focusReminder: true);
            e.Handled = true;
        }
    }

    private void TaskList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (Selected is { IsTask: true } sel) BeginInlineEdit(sel.TaskId);
    }

    private void TaskList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(TaskList);
        _dragSourceTaskId = RowFromOriginalSource(e.OriginalSource as DependencyObject)?.TaskId;
    }

    private void TaskList_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSourceTaskId is not { } taskId) return;
        var current = e.GetPosition(TaskList);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        DragDrop.DoDragDrop(TaskList, taskId.ToString(), DragDropEffects.Move);
        _dragSourceTaskId = null;
    }

    private void TaskList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void TaskList_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.StringFormat)) return;
        if (!Guid.TryParse(e.Data.GetData(DataFormats.StringFormat) as string, out var taskId)) return;
        var target = RowFromOriginalSource(e.OriginalSource as DependencyObject);
        if (target is null || target.TaskId == taskId) return;

        if (target.IsHeading)
        {
            MoveTaskToHeadingEnd(taskId, target.HeadingId);
        }
        else
        {
            MoveTaskBeforeTask(taskId, target.TaskId);
        }
    }

    private void HeadingCollapse_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm { IsHeading: true, HeadingId: { } headingId }) return;
        var heading = _db.GetHeadings().FirstOrDefault(h => h.Id == headingId);
        if (heading is null) return;
        heading.Collapsed = !heading.Collapsed;
        _db.SaveHeading(heading);
        Refresh();
    }

    private void StateButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm { IsTask: true } row)
        {
            SetState(row.TaskId, NextCycleState(row.State));
        }
    }

    private void ExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm { IsTask: true } row)
        {
            ToggleExpanded(row.TaskId);
        }
    }

    private void InlineTitleBox_KeyDown(object sender, KeyEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (e.Key == Key.Enter)
        {
            CommitInlineEdit(row);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelInlineEdit();
            e.Handled = true;
        }
    }

    private void InlineTitleBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm row && _editingTaskId == row.TaskId)
        {
            CommitInlineEdit(row);
        }
    }

    private void BeginInlineEdit(Guid taskId)
    {
        _editingTaskId = taskId;
        Refresh();
        var row = _rows.FirstOrDefault(r => r.TaskId == taskId);
        if (row is null) return;
        TaskList.SelectedItem = row;
        TaskList.ScrollIntoView(row);
        Dispatcher.BeginInvoke(() =>
        {
            var container = TaskList.ItemContainerGenerator.ContainerFromItem(row) as DependencyObject;
            var box = container is null ? null : FindVisualChild<TextBox>(container);
            if (box is null) return;
            box.Focus();
            box.SelectAll();
        });
    }

    private void CommitInlineEdit(TaskRowVm row)
    {
        if (_editingTaskId != row.TaskId) return;
        var title = row.EditTitle.Trim();
        if (title.Length == 0)
        {
            BeginInlineEdit(row.TaskId);
            return;
        }

        var task = _db.GetTasks(includeArchived: true).FirstOrDefault(t => t.Id == row.TaskId);
        if (task is not null && task.Title != title)
        {
            task.Title = title;
            _db.SaveTask(task);
        }
        _editingTaskId = null;
        Refresh();
    }

    private void CancelInlineEdit()
    {
        _editingTaskId = null;
        Refresh();
    }

    private void ToggleExpanded(Guid taskId)
    {
        if (!_expandedTaskIds.Add(taskId))
        {
            _expandedTaskIds.Remove(taskId);
        }
        Refresh();
    }

    private void ToggleHeadingFocus(Guid? headingId)
    {
        var key = headingId ?? Guid.Empty;
        _focusedHeadingId = _focusedHeadingId == key ? null : key;
        Refresh();
    }

    private void EditTask(Guid taskId, bool focusReminder = false)
    {
        var task = _db.GetTasks(includeArchived: true).FirstOrDefault(t => t.Id == taskId);
        if (task is null) return;
        var dlg = new TaskEditorWindow(_db, _clock, _entities, task, focusReminder) { Owner = this };
        if (dlg.ShowDialog() == true) Refresh();
    }

    private void SetState(Guid taskId, TaskState state)
    {
        var task = _db.GetTasks(includeArchived: true).FirstOrDefault(t => t.Id == taskId);
        if (task is null) return;
        var wasDone = task.State == TaskState.Done;
        task.State = state;
        if (state == TaskState.Done)
        {
            task.CompletedAt ??= _clock.UtcNow;
            _reminders.Acknowledge(taskId);
        }
        else if (wasDone)
        {
            task.CompletedAt = null;
        }

        if (state == TaskState.Archived) task.ArchivedAt ??= _clock.UtcNow;
        if (state != TaskState.Archived) task.ArchivedAt = null;

        _db.SaveTask(task);
        Refresh();
    }

    private static TaskState NextCycleState(TaskState state) => state switch
    {
        TaskState.Inbox => TaskState.Next,
        TaskState.Next => TaskState.Waiting,
        TaskState.Waiting => TaskState.Scheduled,
        TaskState.Scheduled => TaskState.Someday,
        TaskState.Someday => TaskState.Done,
        TaskState.Done => TaskState.Inbox,
        TaskState.Archived => TaskState.Inbox,
        _ => TaskState.Inbox,
    };

    private void CreateHeading()
    {
        var prompt = new SimplePromptWindow("New heading", "Heading title:") { Owner = this };
        if (prompt.ShowDialog() == true && !string.IsNullOrWhiteSpace(prompt.Result))
        {
            _db.SaveHeading(_entities.CreateHeading(_activePageId, prompt.Result.Trim()));
            Refresh();
        }
    }

    private void MoveTaskWithinHeading(Guid taskId, int delta)
    {
        var tasks = _db.GetTasks(includeArchived: true, pageId: _activePageId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .ToList();
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        if (task is null) return;
        var siblings = tasks.Where(t => t.HeadingId == task.HeadingId).ToList();
        var index = siblings.FindIndex(t => t.Id == taskId);
        var targetIndex = index + delta;
        if (index < 0 || targetIndex < 0 || targetIndex >= siblings.Count) return;
        var target = siblings[targetIndex];
        (task.SortOrder, target.SortOrder) = (target.SortOrder, task.SortOrder);
        _db.SaveTask(task);
        _db.SaveTask(target);
        Refresh();
    }

    private void MoveTaskToAdjacentHeading(Guid taskId, int delta)
    {
        var task = _db.GetTasks(includeArchived: true, pageId: _activePageId).FirstOrDefault(t => t.Id == taskId);
        if (task is null) return;
        var headingIds = new List<Guid?> { null };
        headingIds.AddRange(_db.GetHeadings(_activePageId).OrderBy(h => h.SortOrder).Select(h => (Guid?)h.Id));
        var index = headingIds.FindIndex(id => id == task.HeadingId);
        if (index < 0) index = 0;
        var targetIndex = index + delta;
        if (targetIndex < 0 || targetIndex >= headingIds.Count) return;

        var targetHeadingId = headingIds[targetIndex];
        task.HeadingId = targetHeadingId;
        var siblings = _db.GetTasks(includeArchived: true, pageId: _activePageId)
            .Where(t => t.HeadingId == targetHeadingId && t.Id != task.Id)
            .OrderBy(t => t.SortOrder)
            .ToList();
        task.SortOrder = SortOrderMath.Between(siblings.LastOrDefault()?.SortOrder, null);
        _db.SaveTask(task);
        Refresh();
    }

    private void MoveHeading(Guid? headingId, int delta)
    {
        if (!headingId.HasValue) return;
        var headings = _db.GetHeadings(_activePageId).OrderBy(h => h.SortOrder).ThenBy(h => h.Title).ToList();
        var index = headings.FindIndex(h => h.Id == headingId.Value);
        var targetIndex = index + delta;
        if (index < 0 || targetIndex < 0 || targetIndex >= headings.Count) return;
        var heading = headings[index];
        var target = headings[targetIndex];
        (heading.SortOrder, target.SortOrder) = (target.SortOrder, heading.SortOrder);
        _db.SaveHeading(heading);
        _db.SaveHeading(target);
        Refresh();
    }

    private void MoveTaskToHeadingEnd(Guid taskId, Guid? headingId)
    {
        var task = _db.GetTasks(includeArchived: true, pageId: _activePageId).FirstOrDefault(t => t.Id == taskId);
        if (task is null) return;
        var siblings = _db.GetTasks(includeArchived: true, pageId: _activePageId)
            .Where(t => t.HeadingId == headingId && t.Id != taskId)
            .OrderBy(t => t.SortOrder)
            .ToList();
        task.HeadingId = headingId;
        task.SortOrder = SortOrderMath.Between(siblings.LastOrDefault()?.SortOrder, null);
        _db.SaveTask(task);
        Refresh();
    }

    private void MoveTaskBeforeTask(Guid taskId, Guid targetTaskId)
    {
        var tasks = _db.GetTasks(includeArchived: true, pageId: _activePageId)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .ToList();
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        var target = tasks.FirstOrDefault(t => t.Id == targetTaskId);
        if (task is null || target is null) return;
        var siblings = tasks.Where(t => t.HeadingId == target.HeadingId && t.Id != taskId).ToList();
        var targetIndex = siblings.FindIndex(t => t.Id == targetTaskId);
        var previous = targetIndex > 0 ? siblings[targetIndex - 1].SortOrder : (double?)null;
        var next = target.SortOrder;
        task.HeadingId = target.HeadingId;
        task.SortOrder = SortOrderMath.Between(previous, next);
        _db.SaveTask(task);
        Refresh();
    }

    private void SwitchPage(int delta)
    {
        if (_pages.Count == 0) RefreshPages();
        if (_pages.Count == 0) return;
        var index = _pages.FindIndex(p => p.Id == _activePageId);
        if (index < 0) index = 0;
        var next = (index + delta + _pages.Count) % _pages.Count;
        SetActivePage(_pages[next]);
    }

    private void JumpToPage(int index)
    {
        if (_pages.Count == 0) RefreshPages();
        if (index < 0 || index >= _pages.Count) return;
        SetActivePage(_pages[index]);
    }

    private static int? NumberFromKey(Key key)
    {
        if (key >= Key.D1 && key <= Key.D9) return key - Key.D0;
        if (key >= Key.NumPad1 && key <= Key.NumPad9) return key - Key.NumPad0;
        return null;
    }

    private void SaveCurrentPageViewState()
    {
        var page = _db.GetPage(_activePageId);
        if (page is null) return;
        page.LastFilterView = _filterMode;
        page.LastFocusedHeadingId = _focusedHeadingId == Guid.Empty ? null : _focusedHeadingId;
        page.LastSearchText = _searchText;
        _db.SavePage(page);
    }

    private void RestorePageViewState(DomainPage page)
    {
        _filterMode = page.LastFilterView;
        _focusedHeadingId = page.LastFocusedHeadingId;
        _searchText = page.LastSearchText ?? "";
        SearchBox.Text = _searchText;
        FilterCombo.SelectedItem = _filterMode;
    }

    private static bool IsInlineEditorFocused()
        => Keyboard.FocusedElement is TextBox tb && tb.Name != "SearchBox";

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T found) return found;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }

    private static TaskRowVm? RowFromOriginalSource(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: TaskRowVm row }) return row;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }
}
