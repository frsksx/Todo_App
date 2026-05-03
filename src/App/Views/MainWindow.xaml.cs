using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;
using WindowsTrayTasks.Reminders;
using DomainPage = WindowsTrayTasks.Domain.Page;

namespace WindowsTrayTasks.Views;

public sealed class TaskRowVm
{
    public bool IsHeading { get; init; }
    public bool IsTask => !IsHeading;
    public bool IsNewTask { get; init; }

    public Guid? HeadingId { get; init; }
    public string HeadingTitle { get; init; } = "(No heading)";
    public int HeadingCount { get; init; }
    public bool HeadingCollapsed { get; init; }
    public bool IsFocusedHeading { get; init; }
    public bool IsNewHeading { get; init; }
    public bool IsEditingHeading { get; init; }
    public Guid? PageId { get; init; }
    public string EditHeadingTitle { get; set; } = "";
    public string HeadingChevron => IsNewHeading || HeadingId is null ? "" : HeadingCollapsed ? "▶" : "▾";
    public string HeadingDisplay => HeadingTitle;
    public string HeadingFocusText => IsFocusedHeading ? "Focused" : "";
    public Visibility HeadingTitleVisibility => IsEditingHeading ? Visibility.Collapsed : Visibility.Visible;
    public Visibility HeadingEditorVisibility => IsEditingHeading ? Visibility.Visible : Visibility.Collapsed;

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
        TaskState.Action => "\u25CF",
        TaskState.Next => "\u25B6",
        TaskState.OnHold => "\u23F8",
        TaskState.Waiting => "\u231B",
        TaskState.Someday => "\u2601",
        TaskState.Done => "\u2713",
        TaskState.Archived => "A",
        _ => "\u25CF",
    };

    public DateTime? StartAt { get; init; }
    public DateTime? DueAt { get; init; }
    public string? Recurrence { get; init; }
    public string? Link { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? Notes { get; init; }
    public DateTime? ReminderNextFireAt { get; init; }
    public int? ReminderAutoSnoozeMinutes { get; init; }
    public bool ReminderAutoSnoozeEnabled { get; init; }
    public IReadOnlyList<Tag> Tags { get; init; } = [];
    public TaskPriority? Priority { get; init; }
    public int? EffortHours { get; init; }
    public bool ReminderActive { get; init; }
    public bool ReminderOverdue { get; init; }
    public bool IsExpanded { get; init; }
    public bool IsSettingsExpanded { get; init; }
    public bool IsEditingNotes { get; init; }
    public string EditNotes { get; set; } = "";
    public string ExpandGlyph => IsExpanded ? "▾" : "›";
    public string SettingsGlyph => "\u2699";
    public Visibility ExpandedVisibility => IsExpanded ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SettingsVisibility => IsSettingsExpanded ? Visibility.Visible : Visibility.Collapsed;
    public Visibility NotesTextVisibility => IsEditingNotes ? Visibility.Collapsed : Visibility.Visible;
    public Visibility NotesEditorVisibility => IsEditingNotes ? Visibility.Visible : Visibility.Collapsed;

    public bool IsEditingStart { get; init; }
    public string EditStart { get; set; } = "";
    public bool IsEditingDue { get; init; }
    public string EditDue { get; set; } = "";

    public string StartChipText => StartAt.HasValue ? $"→ {ToLocal(StartAt.Value)}" : "→ set start";
    public string DueChipText => DueAt.HasValue ? $"↗ {ToLocal(DueAt.Value)}" : "↗ set due";
    public DateTime? StartLocalDate => StartAt?.ToLocalTime().Date;
    public DateTime? DueLocalDate => DueAt?.ToLocalTime().Date;
    public string ClearStartText => StartAt.HasValue ? "✕" : "";
    public string ClearDueText => DueAt.HasValue ? "✕" : "";
    public Visibility StartChipVisibility => IsEditingStart ? Visibility.Collapsed : Visibility.Visible;
    public Visibility DueChipVisibility => IsEditingDue ? Visibility.Collapsed : Visibility.Visible;
    public Visibility StartEditorVisibility => IsEditingStart ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DueEditorVisibility => IsEditingDue ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ClearStartVisibility => StartAt.HasValue && !IsEditingStart ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ClearDueVisibility => DueAt.HasValue && !IsEditingDue ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CompletedChipVisibility => Visibility.Collapsed;
    public string CompletedChipText => "";

    public string DueText
    {
        get
        {
            if (DueAt is null) return "";
            var dueDate = DueAt.Value.ToLocalTime().Date;
            var days = (dueDate - DateTime.Today).Days;
            if (days < 0) return $"{-days}d ago";
            if (days == 0) return "Today";
            if (days <= 14) return $"in {days}d";
            return DueAt.Value.ToLocalTime().ToString("yyyy-MM-dd");
        }
    }
    public string NotesText => string.IsNullOrWhiteSpace(Notes) ? "No notes" : Notes!;
    public string RecurrenceText => string.IsNullOrWhiteSpace(Recurrence) ? "" : $"repeat {Recurrence}";
    public Visibility RecurrenceVisibility => string.IsNullOrWhiteSpace(Recurrence) ? Visibility.Collapsed : Visibility.Visible;
    public string LinkText => string.IsNullOrWhiteSpace(Link) ? "" : "link";
    public Visibility LinkVisibility => string.IsNullOrWhiteSpace(Link) ? Visibility.Collapsed : Visibility.Visible;
    public string TagsText => Tags.Count > 0 ? string.Join("  ", Tags.Select(t => "@" + t.DisplayName)) : "";
    public Visibility TagsVisibility => Tags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public string TagsRowText => Tags.Count > 0 ? string.Join(" ", Tags.Select(t => "@" + t.DisplayName)) : "";
    public Visibility TagsRowVisibility => Tags.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public string PriorityLabel => Priority switch
    {
        TaskPriority.Low => "↓ Low",
        TaskPriority.Medium => "→ Med",
        TaskPriority.High => "↑ High",
        _ => "priority",
    };
    public Brush PriorityBrush => Priority switch
    {
        TaskPriority.High => new SolidColorBrush(Color.FromRgb(0xC8, 0x4A, 0x3D)),
        TaskPriority.Medium => new SolidColorBrush(Color.FromRgb(0xB9, 0x60, 0x26)),
        _ => new SolidColorBrush(Color.FromRgb(0x2F, 0x7D, 0x8C)),
    };
    public string EffortLabel => EffortHours.HasValue ? $"{EffortHours}h" : "effort";
    public Visibility EffortVisibility => EffortHours.HasValue ? Visibility.Visible : Visibility.Collapsed;
    public bool HasLink => !string.IsNullOrWhiteSpace(Link);

    private static readonly Regex _tagStripPattern =
        new(@"\s*@[\p{L}\p{N}_][\p{L}\p{N}_\-.]*", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string DisplayTitle
    {
        get
        {
            if (!Title.Contains('@')) return Title;
            var stripped = _tagStripPattern.Replace(Title, " ");
            return Regex.Replace(stripped, @"\s+", " ").Trim();
        }
    }

    public FontWeight TitleFontWeight => IsExpanded ? FontWeights.Bold : FontWeights.Normal;

    public string StartRightText => StartAt.HasValue ? $"→ {ToLocal(StartAt.Value)}" : "→ start";
    public string DueRightText => DueAt.HasValue ? $"↗ {DueText}" : "↗ due";
    // In the expanded section, hide due date display (shown in title row) and its clear button.
    // Only show the "↗ due" prompt to allow setting when no date is set.
    public Visibility DueExpandedChipVisibility => DueAt.HasValue || IsEditingDue
        ? Visibility.Collapsed : Visibility.Visible;
    public Visibility ClearDueExpandedVisibility => Visibility.Collapsed;
    public string ReminderDetailText
    {
        get
        {
            if (!ReminderActive || ReminderNextFireAt is null) return "";
            var repeat = ReminderAutoSnoozeEnabled && ReminderAutoSnoozeMinutes is { } m ? $" · nags every {m}m" : "";
            return ReminderOverdue
                ? $"⏰ overdue · {ToLocal(ReminderNextFireAt.Value)}{repeat}"
                : $"⏰ {ToLocal(ReminderNextFireAt.Value)}{repeat}";
        }
    }
    public Visibility ReminderDetailVisibility => ReminderActive ? Visibility.Visible : Visibility.Collapsed;

    public string ReminderText
    {
        get
        {
            if (!ReminderActive || ReminderNextFireAt is null) return "";
            var local = ToLocal(ReminderNextFireAt.Value);
            var repeat = ReminderAutoSnoozeEnabled && ReminderAutoSnoozeMinutes is { } m ? $" · every {m}m" : "";
            return ReminderOverdue ? $"⏰ overdue · {local}{repeat}" : $"⏰ {local}";
        }
    }

    public Brush TitleBrush => State is TaskState.Done or TaskState.Someday or TaskState.Archived
        ? new SolidColorBrush(Color.FromRgb(0x92, 0x98, 0x9C))
        : new SolidColorBrush(Color.FromRgb(0x24, 0x2B, 0x2E));

    public string? TitleToolTip => HasLink ? $"Ctrl+click to open {Link}" : null;

    public System.Windows.TextDecorationCollection? TitleDecorations
    {
        get
        {
            if (State != TaskState.Done && !HasLink) return null;

            var decorations = new System.Windows.TextDecorationCollection();
            if (State == TaskState.Done)
            {
                decorations.Add(new System.Windows.TextDecoration
                {
                    Location = System.Windows.TextDecorationLocation.Strikethrough,
                });
            }
            if (HasLink)
            {
                decorations.Add(new System.Windows.TextDecoration
                {
                    Location = System.Windows.TextDecorationLocation.Underline,
                });
            }
            return decorations;
        }
    }

    public Brush DueBrush
    {
        get
        {
            if (DueAt is null) return new SolidColorBrush(Color.FromRgb(0x6C, 0x73, 0x78));
            var days = (DueAt.Value.ToLocalTime().Date - DateTime.Today).Days;
            return days < 7
                ? new SolidColorBrush(Color.FromRgb(0xC8, 0x4A, 0x3D))
                : new SolidColorBrush(Color.FromRgb(0x6C, 0x73, 0x78));
        }
    }

    public FontWeight StateGlyphFontWeight =>
        State is TaskState.Action or TaskState.Next ? FontWeights.Bold : FontWeights.SemiBold;

    public FontStyle HeadingFontStyle =>
        HeadingId is null ? FontStyles.Italic : FontStyles.Normal;

    public Brush ReminderBrush => ReminderOverdue
        ? new SolidColorBrush(Color.FromRgb(0xC8, 0x4A, 0x3D))
        : new SolidColorBrush(Color.FromRgb(0x2F, 0x7D, 0x8C));

    public Brush StateBrush => State switch
    {
        TaskState.Action => new SolidColorBrush(Color.FromRgb(0x2F, 0x7D, 0x8C)),
        TaskState.Next => new SolidColorBrush(Color.FromRgb(0xC8, 0x4A, 0x3D)),
        TaskState.OnHold => new SolidColorBrush(Color.FromRgb(0x4E, 0x78, 0xB8)),
        TaskState.Waiting => new SolidColorBrush(Color.FromRgb(0xB9, 0x60, 0x26)),
        TaskState.Someday => new SolidColorBrush(Color.FromRgb(0xB7, 0xB9, 0xB7)),
        TaskState.Done => new SolidColorBrush(Color.FromRgb(0x4D, 0x9B, 0x64)),
        TaskState.Archived => new SolidColorBrush(Color.FromRgb(0x6C, 0x73, 0x78)),
        _ => Brushes.Gray,
    };

    public string? RowKeyOverride { get; init; }
    public string RowKey => RowKeyOverride ?? (IsNewHeading ? "h:new" : IsHeading ? $"h:{HeadingId?.ToString() ?? "none"}" : $"t:{TaskId}");

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

    internal static string ToDateInputLocal(DateTime utc) => utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
}

public sealed class InsertionLineAdorner : Adorner
{
    private readonly bool _after;
    private readonly Pen _pen = new(new SolidColorBrush(Color.FromRgb(0x2F, 0x7D, 0x8C)), 2);

    public InsertionLineAdorner(UIElement adornedElement, bool after)
        : base(adornedElement)
    {
        _after = after;
        IsHitTestVisible = false;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        var y = _after ? AdornedElement.RenderSize.Height : 0;
        drawingContext.DrawLine(_pen, new Point(4, y), new Point(Math.Max(4, AdornedElement.RenderSize.Width - 4), y));
        drawingContext.DrawEllipse(_pen.Brush, null, new Point(4, y), 3, 3);
    }
}

public partial class MainWindow : Window
{
    private const double DefaultWindowWidth = 420;
    private const double DefaultWindowHeight = 840;

    private readonly Database _db;
    private readonly ReminderEngine _reminders;
    private readonly IClock _clock;
    private readonly EntityFactory _entities;
    private readonly Action _quickAdd;

    private readonly HashSet<Guid> _expandedTaskIds = new();
    private readonly HashSet<Guid> _settingsExpandedTaskIds = new();
    private List<TaskRowVm> _rows = new();
    private List<DomainPage> _pages = new();
    private List<TaskItem> _taskSnapshot = new();
    private List<Heading> _headingSnapshot = new();
    private Guid _activePageId;
    private string _filterMode = "All";
    private DateFilterBucket _dateBucket = DateFilterBucket.All;
    private string _searchText = "";
    private bool _showEmptyHeadings = true;
    private bool _includeDone;
    private bool _untaggedOnly;
    private bool _inboxOnly;
    private bool _forecastMode;
    private bool _agendaMode;
    private bool _showAgendaTab;
    private readonly HashSet<string> _selectedTags = new(StringComparer.OrdinalIgnoreCase);
    private Guid? _focusedHeadingId;
    private Guid? _editingTaskId;
    private Guid? _editingNotesTaskId;
    private readonly Dictionary<Guid, string> _notesDrafts = new();
    private Guid? _editingStartAtTaskId;
    private Guid? _editingDueAtTaskId;
    private Guid? _creatingTaskId;
    private string _pendingTaskTitle = "";
    private Guid? _newTaskHeadingId;
    private double _newTaskSortOrder;
    private bool _creatingHeading;
    private string _pendingHeadingTitle = "";
    private Point _dragStart;
    private DragPayload? _dragSource;
    private InsertionLineAdorner? _insertionAdorner;
    private AdornerLayer? _insertionLayer;
    private FrameworkElement? _insertionElement;
    private bool _insertionAfter;
    private bool _isQuitting;
    private readonly Dictionary<Guid, DateTime> _recentlyCompletedAt = new();
    private Guid? _renamingPageId;
    private bool _creatingNewTag;
    private bool _suppressSelectionCollapse;
    private Guid _pageHoverTargetId;
    private DateTime _pageHoverStartedAtUtc;
    private Point _tagDragStart;

    private sealed record UndoRecord(
        List<TaskItem> DeletedTasks,
        List<Heading> DeletedHeadings,
        List<(Guid TaskId, Guid? OriginalHeadingId)> OrphanedTasks);

    private UndoRecord? _pendingUndo;
    private DispatcherTimer? _undoTimer;

    private sealed record DragPayload(string Kind, Guid Id, string TagName = "")
    {
        public override string ToString() => Kind == "tag" ? $"tag:{TagName}" : $"{Kind}:{Id}";

        public static bool TryParse(object? raw, out DragPayload payload)
        {
            payload = new DragPayload("", Guid.Empty);
            var text = raw as string;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var parts = text.Split(':', 2);
            if (parts.Length != 2) return false;
            if (parts[0] == "tag" && !string.IsNullOrWhiteSpace(parts[1]))
            {
                payload = new DragPayload("tag", Guid.Empty, parts[1]);
                return true;
            }
            if (!Guid.TryParse(parts[1], out var id)) return false;
            if (parts[0] is not ("task" or "heading" or "inbox")) return false;
            payload = new DragPayload(parts[0], id);
            return true;
        }
    }

    private static DataObject CreateDragDataObject(string payload)
    {
        var data = new DataObject();
        data.SetData(DataFormats.StringFormat, payload);
        data.SetData(DataFormats.UnicodeText, payload);
        data.SetData(DataFormats.Text, payload);
        return data;
    }

    private static bool TryGetDragPayload(IDataObject? data, out DragPayload payload)
    {
        payload = new DragPayload("", Guid.Empty);
        if (data is null) return false;

        object? raw =
            data.GetData(DataFormats.StringFormat)
            ?? data.GetData(DataFormats.UnicodeText)
            ?? data.GetData(DataFormats.Text);

        if (raw is null && data.GetDataPresent(typeof(string)))
            raw = data.GetData(typeof(string));

        return DragPayload.TryParse(raw, out payload);
    }

    public MainWindow(Database db, ReminderEngine reminders, IClock clock, EntityFactory entities, Action quickAdd)
    {
        _db = db;
        _reminders = reminders;
        _clock = clock;
        _entities = entities;
        _quickAdd = quickAdd;
        _activePageId = _db.GetActivePageId();
        InitializeComponent();
        RestoreWindowPlacement();

        var filterItems = new List<string> { "All", "Actions + Next", "Only Next", "All except Done", "Show All", "Only On Hold", "Only Waiting For", "Only Someday/Maybe", "Only Completed" };
        filterItems.AddRange(Perspective.BuiltIns.Select(p => p.Name));
        FilterCombo.ItemsSource = filterItems;
        FilterCombo.SelectedIndex = 0;

        _showAgendaTab = _db.GetSetting("show_agenda_tab") == "1";

        Loaded += (_, _) =>
        {
            HideSearchBar(clear: true);
            Refresh();
            TaskList.Focus();
            Dispatcher.BeginInvoke(UpdateTabScrollButtons, DispatcherPriority.ContextIdle);
        };
        Closing += OnClosing;
        PreviewKeyDown += OnPreviewKeyDown;
        PreviewKeyUp += OnPreviewKeyUp;

        _reminders.StateChanged += () => Dispatcher.BeginInvoke(Refresh);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        SaveCurrentPageViewState();
        SaveWindowPlacement();
        if (!_isQuitting)
        {
            e.Cancel = true;
            Hide();
        }
    }

    public void ToggleVisibility()
    {
        if (IsVisible) HideToTray();
        else ShowAndActivate();
    }

    public void ShowAndActivate()
    {
        Show();
        Activate();
        TaskList.Focus();
        Refresh();
    }

    public void HideToTray()
    {
        SaveCurrentPageViewState();
        SaveWindowPlacement();
        Hide();
    }

    public void ShowForInlineTask()
    {
        if (!IsVisible)
        {
            Show();
        }
        Activate();
        BeginCreateTaskNearSelection(after: true);
    }

    public void PrepareForShutdown()
    {
        _isQuitting = true;
        SaveCurrentPageViewState();
        SaveWindowPlacement();
    }

    public void Refresh()
    {
        var selectedKey = Selected?.RowKey;
        _db.ArchiveCompletedOlderThan(TimeSpan.FromDays(7));
        RefreshPages();
        var reminders = _db.GetActiveReminders().ToDictionary(r => r.TaskId, r => r);
        var now = _clock.UtcNow;

        if (_agendaMode)
        {
            RefreshAgenda(reminders, now, selectedKey);
            return;
        }

        _taskSnapshot = _db.GetTasks(includeArchived: true, pageId: _activePageId);
        _headingSnapshot = _db.GetHeadings(_activePageId).OrderBy(h => h.SortOrder).ThenBy(h => h.Title).ToList();
        var tasks = (_filterMode == "Archived"
                ? _taskSnapshot
                : _taskSnapshot.Where(t => t.ArchivedAt is null))
            .ToList();
        var headings = _headingSnapshot;

        var filtered = ApplyCurrentFilters(tasks, reminders, now).ToList();
        if (_creatingTaskId is { } newTaskId && !_forecastMode)
        {
            filtered.Add(new TaskItem
            {
                Id = newTaskId,
                PageId = _activePageId,
                HeadingId = _newTaskHeadingId,
                Title = _pendingTaskTitle,
                State = TaskState.Action,
                SortOrder = _newTaskSortOrder,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        _rows = BuildRows(headings, filtered, reminders, now);
        var selectedRow = selectedKey is null ? null : _rows.FirstOrDefault(r => r.RowKey == selectedKey);
        if (CollapseExpandedTasksExcept(selectedRow is { IsTask: true } ? selectedRow.TaskId : null))
        {
            _rows = BuildRows(headings, filtered, reminders, now);
            selectedRow = selectedKey is null ? null : _rows.FirstOrDefault(r => r.RowKey == selectedKey);
        }

        _suppressSelectionCollapse = true;
        try
        {
            TaskList.ItemsSource = _rows;
            TaskList.SelectedItem = selectedRow;
        }
        finally
        {
            _suppressSelectionCollapse = false;
        }
        RenderTagBar(tasks);

    }

    private void RefreshAgenda(Dictionary<Guid, Reminder> reminders, DateTime now, string? selectedKey)
    {
        _taskSnapshot = _db.GetTasks(includeArchived: _filterMode == "Archived");
        _headingSnapshot = _db.GetHeadings().OrderBy(h => h.PageId).ThenBy(h => h.SortOrder).ThenBy(h => h.Title).ToList();

        var tasks = (_filterMode == "Archived"
                ? _taskSnapshot
                : _taskSnapshot.Where(t => t.ArchivedAt is null))
            .ToList();

        var filtered = TaskFilter.Apply(tasks, reminders, new ComposedFilterCriteria(
            PageId: null,
            StateMode: _filterMode,
            IncludeDone: _includeDone,
            DateBucket: _dateBucket,
            SearchText: _searchText,
            TagNames: _selectedTags,
            UntaggedOnly: _untaggedOnly,
            FocusedHeadingId: null,
            NowUtc: now)).ToList();

        _rows = BuildAgendaRows(filtered, reminders, now);
        var selectedRow = selectedKey is null ? null : _rows.FirstOrDefault(r => r.RowKey == selectedKey);
        if (CollapseExpandedTasksExcept(selectedRow is { IsTask: true } ? selectedRow.TaskId : null))
        {
            _rows = BuildAgendaRows(filtered, reminders, now);
            selectedRow = selectedKey is null ? null : _rows.FirstOrDefault(r => r.RowKey == selectedKey);
        }

        _suppressSelectionCollapse = true;
        try
        {
            TaskList.ItemsSource = _rows;
            TaskList.SelectedItem = selectedRow;
        }
        finally
        {
            _suppressSelectionCollapse = false;
        }
        RenderAgendaTagBar(tasks);
    }

    private void RefreshPages()
    {
        _pages = _db.GetPages();
        if (_pages.All(p => p.Id != _activePageId))
        {
            _activePageId = _db.GetActivePageId();
        }

        var summaries = _db.GetPageTaskSummaries();

        PageTabs.Children.Clear();
        if (_showAgendaTab)
        {
            var isActive = _agendaMode;
            var agendaTab = new Button
            {
                Content = new TextBlock { Text = "Agenda", VerticalAlignment = VerticalAlignment.Center, FontStyle = FontStyles.Italic },
                Padding = new Thickness(10, 3, 10, 3),
                Margin = new Thickness(0, 0, 4, 0),
                FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal,
                BorderThickness = new Thickness(1),
                BorderBrush = isActive ? ThemeBrush("AppAccentSoftBrush") : Brushes.Transparent,
                Background = isActive ? ThemeBrush("AppSelectedBrush") : Brushes.Transparent,
                Foreground = isActive ? ThemeBrush("AppAccentStrongBrush") : ThemeBrush("AppTextMutedBrush"),
            };
            WindowChrome.SetIsHitTestVisibleInChrome(agendaTab, true);
            agendaTab.Click += (_, _) => SetAgendaMode();
            PageTabs.Children.Add(agendaTab);
        }

        foreach (var page in _pages)
        {
            var isRenaming = _renamingPageId == page.Id;
            UIElement tabContent;
            if (isRenaming)
            {
                var editor = new TextBox
                {
                    Text = page.Name,
                    DataContext = page,
                    FontSize = 12,
                    MinWidth = 60,
                    Padding = new Thickness(6, 1, 6, 1),
                    BorderBrush = ThemeBrush("AppAccentBrush"),
                };
                WindowChrome.SetIsHitTestVisibleInChrome(editor, true);
                editor.KeyDown += PageNameEditor_KeyDown;
                editor.LostFocus += PageNameEditor_LostFocus;
                tabContent = editor;
                Dispatcher.BeginInvoke(() => { editor.Focus(); editor.SelectAll(); });
            }
            else
            {
                var isActive = !_agendaMode && page.Id == _activePageId;
                summaries.TryGetValue(page.Id, out var s);
                tabContent = new TextBlock
                {
                    Text = page.Name,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = s.HasUrgentDue
                        ? ThemeBrush("AppDangerBrush")
                        : isActive ? ThemeBrush("AppAccentStrongBrush") : ThemeBrush("AppTextBrush"),
                    FontStyle = !s.HasNextActions ? FontStyles.Italic : FontStyles.Normal,
                };
            }

            var active = !_agendaMode && page.Id == _activePageId;
            var button = new Button
            {
                Content = tabContent,
                DataContext = page,
                Padding = new Thickness(10, 3, 10, 3),
                Margin = new Thickness(0, 0, 4, 0),
                FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal,
                BorderThickness = new Thickness(1),
                BorderBrush = active ? ThemeBrush("AppAccentSoftBrush") : Brushes.Transparent,
                Background = active ? ThemeBrush("AppSelectedBrush") : Brushes.Transparent,
                AllowDrop = true,
            };
            WindowChrome.SetIsHitTestVisibleInChrome(button, true);
            button.Click += PageTab_Click;
            button.MouseDoubleClick += PageTab_DoubleClick;
            button.PreviewDragOver += PageTab_DragOver;
            button.PreviewDragLeave += PageTab_DragLeave;
            button.PreviewDrop += PageTab_Drop;
            button.ContextMenu = BuildPageContextMenu(page);
            PageTabs.Children.Add(button);
        }

        Dispatcher.BeginInvoke(UpdateTabScrollButtons, DispatcherPriority.ContextIdle);
    }

    private Brush ThemeBrush(string key) => (Brush)FindResource(key);

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
    {
        // Expire 5-minute grace window
        var expiry = DateTime.UtcNow.AddMinutes(-5);
        foreach (var key in _recentlyCompletedAt.Keys.Where(k => _recentlyCompletedAt[k] < expiry).ToList())
            _recentlyCompletedAt.Remove(key);

        var filtered = TaskFilter.Apply(tasks, reminders, new ComposedFilterCriteria(
            PageId: _activePageId,
            StateMode: _filterMode,
            IncludeDone: _includeDone,
            DateBucket: _forecastMode ? DateFilterBucket.All : _dateBucket,
            SearchText: _searchText,
            TagNames: _selectedTags,
            UntaggedOnly: _untaggedOnly,
            FocusedHeadingId: _focusedHeadingId,
            NowUtc: now,
            InboxOnly: _inboxOnly)).ToList();

        // Keep recently-completed tasks visible for 5 minutes even when Done is hidden
        if (_recentlyCompletedAt.Count > 0 && !_includeDone)
        {
            var filteredIds = filtered.Select(t => t.Id).ToHashSet();
            var extras = tasks.Where(t => _recentlyCompletedAt.ContainsKey(t.Id) && !filteredIds.Contains(t.Id));
            filtered.AddRange(extras);
        }

        return filtered;
    }

    private List<TaskRowVm> BuildRows(
        IReadOnlyList<Heading> headings,
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyDictionary<Guid, Reminder> reminders,
        DateTime now)
    {
        if (_forecastMode)
            return BuildForecastRows(tasks, reminders, now);

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

                rows.Add(CreateHeadingRow(heading.Id, heading.Title, headingTasks.Count, heading.Collapsed, pageId: _activePageId));
            if (!heading.Collapsed)
            {
                rows.AddRange(headingTasks.Select(t => CreateTaskRow(t, heading.Title, reminders, now)));
            }
        }

        if (_creatingHeading && !_focusedHeadingId.HasValue && !_forecastMode)
        {
            rows.Add(CreateHeadingRow(null, "", 0, collapsed: false, isNew: true, isEditing: true, pageId: _activePageId));
        }

        var noHeadingTasks = tasksByHeading.TryGetValue(HeadingKey(null), out var looseTasks)
            ? looseTasks
            : new List<TaskItem>();
        if (noHeadingTasks.Count > 0
            && (!_focusedHeadingId.HasValue || _focusedHeadingId.Value == Guid.Empty))
        {
            rows.Add(CreateHeadingRow(null, "Inbox", noHeadingTasks.Count, collapsed: false, pageId: _activePageId));
            rows.AddRange(noHeadingTasks.Select(t => CreateTaskRow(t, "Inbox", reminders, now)));
        }

        return rows;
    }

    private List<TaskRowVm> BuildAgendaRows(
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyDictionary<Guid, Reminder> reminders,
        DateTime now)
    {
        var rows = new List<TaskRowVm>();
        var tasksByPage = tasks
            .GroupBy(t => t.PageId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.SortOrder).ThenBy(t => t.Title).ToList());
        var headingsByPage = _headingSnapshot
            .GroupBy(h => h.PageId)
            .ToDictionary(g => g.Key, g => g.OrderBy(h => h.SortOrder).ThenBy(h => h.Title).ToList());

        foreach (var page in _pages)
        {
            tasksByPage.TryGetValue(page.Id, out var pageTasks);
            pageTasks ??= new List<TaskItem>();
            headingsByPage.TryGetValue(page.Id, out var pageHeadings);
            pageHeadings ??= new List<Heading>();

            var tasksByHeading = pageTasks
                .GroupBy(t => HeadingKey(t.HeadingId))
                .ToDictionary(g => g.Key, g => g.OrderBy(t => t.SortOrder).ThenBy(t => t.Title).ToList());

            foreach (var heading in pageHeadings)
            {
                tasksByHeading.TryGetValue(HeadingKey(heading.Id), out var headingTasks);
                headingTasks ??= new List<TaskItem>();
                if (headingTasks.Count == 0 && !_showEmptyHeadings) continue;

                var headingTitle = $"{heading.Title} [{page.Name}]";
                rows.Add(CreateHeadingRow(heading.Id, headingTitle, headingTasks.Count, heading.Collapsed, pageId: page.Id));
                if (!heading.Collapsed)
                {
                    rows.AddRange(headingTasks.Select(t => CreateTaskRow(t, headingTitle, reminders, now)));
                }
            }

            var inboxTasks = tasksByHeading.TryGetValue(HeadingKey(null), out var looseTasks)
                ? looseTasks
                : new List<TaskItem>();
            if (inboxTasks.Count > 0)
            {
                var inboxTitle = $"Inbox [{page.Name}]";
                rows.Add(CreateHeadingRow(null, inboxTitle, inboxTasks.Count, collapsed: false, pageId: page.Id, rowKeyOverride: $"h:inbox:{page.Id}"));
                rows.AddRange(inboxTasks.Select(t => CreateTaskRow(t, inboxTitle, reminders, now)));
            }
        }

        return rows;
    }

    private List<TaskRowVm> BuildForecastRows(
        IReadOnlyList<TaskItem> tasks,
        IReadOnlyDictionary<Guid, Reminder> reminders,
        DateTime now)
    {
        var rows = new List<TaskRowVm>();
        var today = now.ToLocalTime().Date;

        static DateFilterBucket GetBucket(TaskItem task, IReadOnlyDictionary<Guid, Reminder> rems, DateTime nowUtc, DateTime todayLocal)
        {
            var earliest = new[] {
                task.StartAt, task.DueAt,
                rems.TryGetValue(task.Id, out var rem) ? rem.NextFireAt : null
            }.Where(d => d.HasValue).Select(d => d!.Value).DefaultIfEmpty().Min();

            if (earliest == default) return DateFilterBucket.NoDate;
            if (earliest <= nowUtc) return DateFilterBucket.Overdue;
            var localDate = earliest.ToLocalTime().Date;
            if (localDate == todayLocal) return DateFilterBucket.Today;
            if (localDate == todayLocal.AddDays(1)) return DateFilterBucket.Tomorrow;
            if (localDate <= todayLocal.AddDays(7)) return DateFilterBucket.ThisWeek;
            return DateFilterBucket.Upcoming;
        }

        var groups = new[] {
            (DateFilterBucket.Overdue, "Overdue"),
            (DateFilterBucket.Today, "Today"),
            (DateFilterBucket.Tomorrow, "Tomorrow"),
            (DateFilterBucket.ThisWeek, "This Week"),
            (DateFilterBucket.Upcoming, "Later"),
            (DateFilterBucket.NoDate, "No Date"),
        };

        var buckets = tasks
            .Select(t => (Task: t, Bucket: GetBucket(t, reminders, now, today)))
            .GroupBy(x => x.Bucket)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Task)
                .OrderBy(t => t.DueAt ?? t.StartAt ?? DateTime.MaxValue)
                .ThenBy(t => t.Title)
                .ToList());

        foreach (var (bucket, label) in groups)
        {
            if (!buckets.TryGetValue(bucket, out var group) || group.Count == 0) continue;
            rows.Add(CreateHeadingRow(null, label, group.Count, collapsed: false));
            rows.AddRange(group.Select(t => CreateTaskRow(t, label, reminders, now)));
        }

        return rows;
    }

    private TaskRowVm CreateHeadingRow(Guid? headingId, string title, int count, bool collapsed, bool isNew = false, bool isEditing = false, Guid? pageId = null, string? rowKeyOverride = null)
        => new()
        {
            IsHeading = true,
            HeadingId = headingId,
            PageId = pageId,
            HeadingTitle = title,
            HeadingCount = count,
            HeadingCollapsed = collapsed,
            IsFocusedHeading = _focusedHeadingId.HasValue && _focusedHeadingId.Value == (headingId ?? Guid.Empty),
            IsNewHeading = isNew,
            IsEditingHeading = isEditing,
            EditHeadingTitle = isNew ? _pendingHeadingTitle : title,
            RowKeyOverride = rowKeyOverride,
        };

    private TaskRowVm CreateTaskRow(TaskItem task, string headingTitle, IReadOnlyDictionary<Guid, Reminder> reminders, DateTime now)
    {
        reminders.TryGetValue(task.Id, out var rem);
        return new TaskRowVm
        {
            IsHeading = false,
            HeadingId = task.HeadingId,
            PageId = task.PageId,
            HeadingTitle = headingTitle,
            TaskId = task.Id,
            Title = task.Title,
            EditTitle = task.Title,
            IsEditing = _editingTaskId == task.Id,
            State = task.State,
            IsNewTask = _creatingTaskId == task.Id,
            StartAt = task.StartAt,
            DueAt = task.DueAt,
            Recurrence = task.Recurrence,
            Link = task.Link,
            CompletedAt = task.CompletedAt,
            Notes = task.Notes,
            Tags = task.Tags,
            Priority = task.Priority,
            EffortHours = task.EffortHours,
            ReminderActive = rem is not null,
            ReminderNextFireAt = rem?.NextFireAt,
            ReminderAutoSnoozeMinutes = rem?.AutoSnoozeIntervalMinutes,
            ReminderAutoSnoozeEnabled = rem?.AutoSnoozeEnabled == true,
            ReminderOverdue = rem?.NextFireAt is { } next && next <= now,
            IsExpanded = _expandedTaskIds.Contains(task.Id),
            IsSettingsExpanded = _settingsExpandedTaskIds.Contains(task.Id),
            IsEditingNotes = _editingNotesTaskId == task.Id,
            EditNotes = _editingNotesTaskId == task.Id && _notesDrafts.TryGetValue(task.Id, out var draft)
                ? draft
                : task.Notes ?? "",
            IsEditingStart = _editingStartAtTaskId == task.Id,
            EditStart = task.StartAt.HasValue ? ToDateInputLocal(task.StartAt.Value) : "",
            IsEditingDue = _editingDueAtTaskId == task.Id,
            EditDue = task.DueAt.HasValue ? ToDateInputLocal(task.DueAt.Value) : "",
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
            HideSearchBar(clear: true);
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
        var selected = (string?)FilterCombo.SelectedItem;
        if (selected is null) return;
        var perspective = Perspective.BuiltIns.FirstOrDefault(p => p.Name == selected);
        if (perspective is not null)
        {
            SetPerspective(perspective);
        }
        else
        {
            _filterMode = selected;
            _includeDone = selected is "Show All" or "Only Completed" or "Archived";
            _forecastMode = false;
            _inboxOnly = false;
            Refresh();
        }
    }

    private void SetPerspective(Perspective p)
    {
        _agendaMode = false;
        _forecastMode = p.Kind == PerspectiveKind.Forecast;
        _inboxOnly = p.Kind == PerspectiveKind.Inbox;
        if (p.Criteria is { } c)
        {
            _filterMode = c.StateMode;
            _includeDone = c.IncludeDone;
        }
        Refresh();
    }

    private void ShowEmptyHeadingsCheck_Changed(object sender, RoutedEventArgs e)
    {
        _showEmptyHeadings = true;
        Refresh();
    }

    private void ShowDoneCheck_Changed(object sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void NewTask_Click(object sender, RoutedEventArgs e) => BeginCreateTaskNearSelection(after: true);

    private void NewTaskAbove_Click(object sender, RoutedEventArgs e) => BeginCreateTaskNearSelection(after: false);

    private void NewTaskBelow_Click(object sender, RoutedEventArgs e) => BeginCreateTaskNearSelection(after: true);

    private void NewHeading_Click(object sender, RoutedEventArgs e) => CreateHeading();

    private void QuickAdd_MenuClick(object sender, RoutedEventArgs e) => BeginCreateTaskNearSelection(after: true);

    private void Quit_Click(object sender, RoutedEventArgs e)
    {
        _isQuitting = true;
        SaveWindowPlacement();
        Application.Current.Shutdown();
    }

    private void TitleBarMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void TitleBarMaximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void TitleBarClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DeleteTask_Click(object sender, RoutedEventArgs e) => ExecuteDeleteSelected();

    private void ArchiveTask_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is not { IsTask: true } sel) return;
        var task = FindTask(sel.TaskId);
        if (task is null) return;
        task.State = TaskState.Archived;
        task.ArchivedAt = _clock.UtcNow;
        _db.SaveTask(task);
        Refresh();
    }

    private void EditTask_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { IsTask: true } sel)
            EditTask(sel.TaskId);
    }

    private void MoveHeadingUp_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { IsHeading: true, IsNewHeading: false } sel)
            MoveHeading(sel.HeadingId, -1);
    }

    private void MoveHeadingDown_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { IsHeading: true, IsNewHeading: false } sel)
            MoveHeading(sel.HeadingId, 1);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SettingsDialog(
            _showAgendaTab,
            _db.GetSetting("sync_enabled") == "1",
            _db.GetSetting("supabase_url"),
            _db.GetSetting("supabase_publishable_key"))
        { Owner = this };
        if (dlg.ShowDialog() == true)
        {
            _showAgendaTab = dlg.ShowAgendaTab;
            _db.SaveSetting("show_agenda_tab", _showAgendaTab ? "1" : "0");
            _db.SaveSetting("sync_enabled", dlg.SyncEnabled ? "1" : "0");
            _db.SaveSetting("supabase_url", string.IsNullOrWhiteSpace(dlg.SupabaseUrl) ? null : dlg.SupabaseUrl.Trim());
            _db.SaveSetting("supabase_publishable_key", string.IsNullOrWhiteSpace(dlg.SupabasePublishableKey) ? null : dlg.SupabasePublishableKey.Trim());
            if (!_showAgendaTab && _agendaMode) { _agendaMode = false; }
            Refresh();
        }
    }

    private void Backup_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show(this, "Backup not yet implemented.", "Tasks");

    private void ToggleReminders_Click(object sender, RoutedEventArgs e)
    {
        _reminders.TogglePause();
        Refresh();
    }

    private void About_Click(object sender, RoutedEventArgs e)
        => MessageBox.Show(this, "WindowsTrayTasks · v1.0", "About");

    private void RenderTagBar(IReadOnlyList<TaskItem> pageTasks)
    {
        TagBar.Children.Clear();

        var untaggedCount = pageTasks.Count(t => t.Tags.Count == 0 && t.ArchivedAt is null);
        TagBar.Children.Add(CreateTagButton("Untagged", $"Untagged ({untaggedCount})", _untaggedOnly));
        TagBar.Children.Add(CreateTagButton("All", "All", !_untaggedOnly && _selectedTags.Count == 0));

        var counts = _db.GetTagTaskCounts(_activePageId);
        foreach (var tag in _db.GetTags(_activePageId))
        {
            counts.TryGetValue(tag.Id, out var count);
            TagBar.Children.Add(CreateTagButton(tag.Name, $"@{tag.DisplayName} ({count})", _selectedTags.Contains(tag.Name), tag.DisplayName));
        }

        if (_creatingNewTag)
        {
            var editor = new TextBox
            {
                Width = 90,
                FontSize = 11,
                Padding = new Thickness(4, 1, 4, 1),
                Margin = new Thickness(0, 0, 4, 2),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x8D, 0xB8, 0xE8)),
                ToolTip = "@tag name — Enter to create, Esc to cancel",
            };
            editor.KeyDown += NewTagEditor_KeyDown;
            editor.LostFocus += NewTagEditor_LostFocus;
            TagBar.Children.Add(editor);
        }
    }

    private void RenderAgendaTagBar(IReadOnlyList<TaskItem> tasks)
    {
        TagBar.Children.Clear();

        var tagTasks = (_filterMode == "Archived"
                ? tasks.Where(t => t.ArchivedAt is not null || t.State == TaskState.Archived)
                : tasks.Where(t => t.ArchivedAt is null && t.State != TaskState.Archived))
            .ToList();
        var untaggedCount = tagTasks.Count(t => t.Tags.Count == 0);
        TagBar.Children.Add(CreateTagButton("Untagged", $"Untagged ({untaggedCount})", _untaggedOnly));
        TagBar.Children.Add(CreateTagButton("All", "All", !_untaggedOnly && _selectedTags.Count == 0));

        var tags = tagTasks
            .SelectMany(t => t.Tags)
            .GroupBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => new { Name = g.Key, DisplayName = g.First().DisplayName, Count = g.Count() })
            .OrderBy(t => t.DisplayName, StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags)
        {
            TagBar.Children.Add(CreateTagButton(tag.Name, $"@{tag.DisplayName} ({tag.Count})", _selectedTags.Contains(tag.Name)));
        }
    }

    private Button CreateTagButton(string key, string text, bool selected, string? dragTagDisplayName = null)
    {
        var border = new Border
        {
            CornerRadius = new CornerRadius(12),
            Background = selected
                ? ThemeBrush("AppAccentBrush")
                : ThemeBrush("AppAccentHoverBrush"),
            BorderBrush = selected ? ThemeBrush("AppAccentStrongBrush") : ThemeBrush("AppAccentSoftBrush"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(8, 2, 8, 2),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 11,
                Foreground = selected
                    ? Brushes.White
                    : ThemeBrush("AppAccentStrongBrush"),
            },
        };
        var button = new Button
        {
            Content = border,
            DataContext = key,
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 5, 2),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = System.Windows.Input.Cursors.Hand,
        };
        button.Click += TagButton_Click;
        if (dragTagDisplayName is not null)
        {
            button.Tag = dragTagDisplayName;
            button.AllowDrop = true;
            button.DragOver += TagButton_DragOver;
            button.Drop += TagButton_Drop;
            button.PreviewMouseLeftButtonDown += (_, e) => _tagDragStart = e.GetPosition(null);
            button.MouseMove += (_, e) =>
            {
                if (e.LeftButton != MouseButtonState.Pressed) return;
                var pos = e.GetPosition(null);
                if (Math.Abs(pos.X - _tagDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(pos.Y - _tagDragStart.Y) < SystemParameters.MinimumVerticalDragDistance) return;
                DragDrop.DoDragDrop(button, CreateDragDataObject($"tag:{dragTagDisplayName}"), DragDropEffects.Copy);
            };
        }
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

    private void TagButton_DragOver(object sender, DragEventArgs e)
    {
        if (!TryGetDropTagName(sender, out _)
            || !TryGetDragPayload(e.Data, out var payload)
            || payload.Kind != "task")
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void TagButton_Drop(object sender, DragEventArgs e)
    {
        if (TryGetDropTagName(sender, out var tagName)
            && TryGetDragPayload(e.Data, out var payload)
            && payload.Kind == "task")
        {
            AssignTagToTask(payload.Id, tagName);
        }

        e.Handled = true;
    }

    private static bool TryGetDropTagName(object sender, out string tagName)
    {
        tagName = "";
        if (sender is not FrameworkElement { Tag: string raw } || string.IsNullOrWhiteSpace(raw))
            return false;

        tagName = raw.Trim().TrimStart('@');
        return tagName.Length > 0;
    }

    private void AssignTagToTask(Guid taskId, string tagName)
    {
        var task = FindTask(taskId);
        if (task is null) return;

        var updatedTitle = TaskTitleTags.AddTagToken(task.Title, tagName);
        if (updatedTitle == task.Title) return;

        task.Title = updatedTitle;
        _db.SaveTask(task);
        Refresh();
    }

    private void PageTab_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DomainPage page) return;
        SetActivePage(page);
    }

    private void PageTabsScrollLeft_Click(object sender, RoutedEventArgs e)
    {
        PageTabsScroll.ScrollToHorizontalOffset(Math.Max(0, PageTabsScroll.HorizontalOffset - 120));
        UpdateTabScrollButtons();
        e.Handled = true;
    }

    private void PageTabsScrollRight_Click(object sender, RoutedEventArgs e)
    {
        PageTabsScroll.ScrollToHorizontalOffset(PageTabsScroll.HorizontalOffset + 120);
        UpdateTabScrollButtons();
        e.Handled = true;
    }

    private void HeaderTabsHost_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateTabScrollButtons();

    private void PageTabs_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdateTabScrollButtons();

    private void PageTabsScroll_ScrollChanged(object sender, ScrollChangedEventArgs e)
        => UpdateTabScrollButtons();

    private void UpdateTabScrollButtons()
    {
        if (!IsLoaded) return;

        var available = HeaderTabsHost.ActualWidth;
        var contentWidth = PageTabs.ActualWidth;
        if (available <= 0 || contentWidth <= 0) return;

        var buttonSpace = PageTabsScrollLeftButton.ActualWidth
                          + PageTabsScrollRightButton.ActualWidth
                          + PageTabsScrollLeftButton.Margin.Left
                          + PageTabsScrollLeftButton.Margin.Right
                          + PageTabsScrollRightButton.Margin.Left
                          + PageTabsScrollRightButton.Margin.Right;
        if (buttonSpace <= 0) buttonSpace = 42;

        var allTabsFit = contentWidth <= available + 0.5;
        PageTabsScrollLeftButton.Visibility = allTabsFit ? Visibility.Collapsed : Visibility.Visible;
        PageTabsScrollRightButton.Visibility = allTabsFit ? Visibility.Collapsed : Visibility.Visible;

        PageTabsScroll.Width = allTabsFit
            ? contentWidth
            : Math.Max(40, available - buttonSpace);

        if (allTabsFit)
        {
            PageTabsScroll.ScrollToHorizontalOffset(0);
            PageTabsScrollLeftButton.IsEnabled = false;
            PageTabsScrollRightButton.IsEnabled = false;
            return;
        }

        PageTabsScrollLeftButton.IsEnabled = PageTabsScroll.HorizontalOffset > 0.5;
        PageTabsScrollRightButton.IsEnabled =
            PageTabsScroll.HorizontalOffset < PageTabsScroll.ScrollableWidth - 0.5;
    }

    private void PageTab_DragOver(object sender, DragEventArgs e)
    {
        if (!TryGetPageDropTarget(sender, e, out var page)
            || !TryGetDragPayload(e.Data, out var payload)
            || payload.Kind is not ("task" or "heading" or "inbox"))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;

        if (page.Id == _activePageId)
        {
            StopPageHoverTimer();
            return;
        }

        var now = _clock.UtcNow;
        if (page.Id != _pageHoverTargetId)
        {
            _pageHoverTargetId = page.Id;
            _pageHoverStartedAtUtc = now;
            return;
        }

        if (now - _pageHoverStartedAtUtc >= TimeSpan.FromMilliseconds(500))
        {
            StopPageHoverTimer();
            SwitchPageDuringDrag(page);
        }
    }

    private void PageTab_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        var position = e.GetPosition(element);
        var stillInside = position.X >= 0
            && position.Y >= 0
            && position.X <= element.ActualWidth
            && position.Y <= element.ActualHeight;
        if (!stillInside)
        {
            StopPageHoverTimer();
        }
    }

    private bool TryGetPageDropTarget(object sender, DragEventArgs e, out DomainPage page)
    {
        page = null!;

        if (sender is FrameworkElement { DataContext: DomainPage senderPage })
        {
            page = senderPage;
            return true;
        }

        var source = e.OriginalSource as DependencyObject;
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: DomainPage elementPage })
            {
                page = elementPage;
                return true;
            }

            if (source is FrameworkContentElement { DataContext: DomainPage contentPage })
            {
                page = contentPage;
                return true;
            }

            source = GetParentObject(source);
        }

        return false;
    }

    private void StopPageHoverTimer()
    {
        _pageHoverTargetId = Guid.Empty;
        _pageHoverStartedAtUtc = default;
    }

    private void SwitchPageDuringDrag(DomainPage page)
    {
        SaveCurrentPageViewState();
        _agendaMode = false;
        _activePageId = page.Id;
        RestorePageViewState(page);
        Refresh();
    }

    private void PageTab_Drop(object sender, DragEventArgs e)
    {
        StopPageHoverTimer();
        if (!TryGetPageDropTarget(sender, e, out var page)) return;
        if (!TryGetDragPayload(e.Data, out var payload)) return;

        ClearInsertionLine();
        if (payload.Kind == "task")
        {
            MoveTaskToPage(payload.Id, page.Id);
        }
        else if (payload.Kind == "heading" && !_agendaMode)
        {
            MoveHeadingToPage(payload.Id, page.Id);
        }
        else if (payload.Kind == "inbox")
        {
            MoveInboxToPage(payload.Id, page.Id);
        }

        if (page.Id != _activePageId) SwitchPageDuringDrag(page);
        e.Handled = true;
    }

    private void SetActivePage(DomainPage page)
    {
        _agendaMode = false;
        SaveCurrentPageViewState();
        _activePageId = page.Id;
        _db.SaveActivePageId(_activePageId);
        _selectedTags.Clear();
        _untaggedOnly = false;
        RestorePageViewState(page);
        Refresh();
    }

    private void SetAgendaMode()
    {
        SaveCurrentPageViewState();
        _agendaMode = true;
        _forecastMode = false;
        _inboxOnly = false;
        _focusedHeadingId = null;
        _filterMode = "Show All";
        _dateBucket = DateFilterBucket.All;
        _includeDone = true;
        _selectedTags.Clear();
        _untaggedOnly = false;

        FilterCombo.SelectionChanged -= FilterCombo_SelectionChanged;
        FilterCombo.SelectedItem = _filterMode;
        FilterCombo.SelectionChanged += FilterCombo_SelectionChanged;

        Refresh();
    }

    private void RenamePage_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DomainPage page) return;
        _renamingPageId = page.Id;
        Refresh();
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
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        if (IsInlineEditorFocused()) return;
        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;

        if (e.Key == Key.Escape)
        {
            // Priority 1: close any open inline editor — Esc accepts (commits) edits
            if (_editingTaskId.HasValue || _creatingTaskId.HasValue)
            {
                if (_editingTaskId == _creatingTaskId) CancelCreateTask();
                else
                {
                    var editRow = _rows.FirstOrDefault(r => r.TaskId == _editingTaskId);
                    if (editRow is not null) CommitInlineEdit(editRow);
                    else CancelInlineEdit();
                }
                e.Handled = true;
                return;
            }
            if (_editingNotesTaskId.HasValue)
            {
                var notesRow = _rows.FirstOrDefault(r => r.TaskId == _editingNotesTaskId);
                if (notesRow is not null) CommitNotesEdit(notesRow);
                else CancelNotesEdit();
                e.Handled = true;
                return;
            }
            if (_editingStartAtTaskId.HasValue) { CancelStartEdit(); e.Handled = true; return; }
            if (_editingDueAtTaskId.HasValue) { CancelDueEdit(); e.Handled = true; return; }
            if (_renamingPageId.HasValue) { _renamingPageId = null; Refresh(); e.Handled = true; return; }
            if (_creatingHeading) { CancelHeadingEdit(); e.Handled = true; return; }

            // Priority 2: close and reset search
            if (SearchBar.Visibility == Visibility.Visible || !string.IsNullOrEmpty(SearchBox.Text))
            {
                HideSearchBar(clear: true);
                e.Handled = true;
                return;
            }

            // Priority 3: collapse expanded task rows and their horizontal settings rail
            if (CollapseAllExpandedTasks())
            {
                Refresh();
                e.Handled = true;
                return;
            }

            e.Handled = true;
            return;
        }

        if (e.Key == Key.Space && !SearchBox.IsKeyboardFocusWithin && Selected is { } selected)
        {
            if (selected.IsHeading && !selected.IsNewHeading)
            {
                ToggleHeadingFocus(selected.HeadingId);
                e.Handled = true;
                return;
            }

            if (selected.IsTask)
            {
                ToggleExpanded(selected.TaskId);
                e.Handled = true;
                return;
            }
        }

        if (ctrl && e.Key == Key.F)
        {
            ToggleSearchBar();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Oem2 && !ctrl)
        {
            ShowSearchBar();
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.N)
        {
            BeginCreateTaskNearSelection(after: true);
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

        // Ctrl+1–8: set filter view (only when Alt is not held — Alt+Ctrl+N is page jump)
        if (ctrl && !alt)
        {
            var filterIndex = e.Key switch
            {
                Key.D1 or Key.NumPad1 => 1,
                Key.D2 or Key.NumPad2 => 2,
                Key.D3 or Key.NumPad3 => 3,
                Key.D4 or Key.NumPad4 => 4,
                Key.D5 or Key.NumPad5 => 5,
                Key.D6 or Key.NumPad6 => 6,
                Key.D7 or Key.NumPad7 => 7,
                Key.D8 or Key.NumPad8 => 8,
                _ => 0,
            };
            if (filterIndex > 0)
            {
                SetFilterView(filterIndex);
                e.Handled = true;
                return;
            }
        }
        if (ctrl && e.Key == Key.Tab)
        {
            SwitchPage((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift ? -1 : 1);
            e.Handled = true;
            return;
        }

        if (!ctrl && alt && !shift && (e.Key == Key.Left || e.Key == Key.Right))
        {
            SwitchPageIncludingAgenda(e.Key == Key.Left ? -1 : 1);
            e.Handled = true;
            return;
        }

        if (ctrl && e.Key == Key.W)
        {
            HideToTray();
            e.Handled = true;
            return;
        }

        if (ctrl && alt)
        {
            var number = NumberFromKey(e.Key == Key.System ? e.SystemKey : e.Key);
            if (number is >= 1 and <= 9)
            {
                JumpToPage(number.Value - 1);
                e.Handled = true;
            }
        }

        // Shift+Left/Right state cycle — must be in PreviewKeyDown so the
        // ListBox (SelectionMode=Extended) never sees it and triggers range-select.
        var shift2 = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        if (!ctrl && shift2 && !alt && Selected is { IsTask: true } sel2
            && (e.Key == Key.Left || e.Key == Key.Right))
        {
            var next = CycleStateForward(sel2.State, e.Key == Key.Right ? 1 : -1);
            SetState(sel2.TaskId, next);
            e.Handled = true;
        }
    }

    private void OnPreviewKeyUp(object sender, KeyEventArgs e)
    {
    }

    private TaskRowVm? Selected => TaskList.SelectedItem as TaskRowVm;

    private IReadOnlyList<TaskItem> CurrentTasks()
    {
        if (_taskSnapshot.Count == 0)
        {
            _taskSnapshot = _db.GetTasks(includeArchived: true, pageId: _activePageId);
        }
        return _taskSnapshot;
    }

    private IReadOnlyList<Heading> CurrentHeadings()
    {
        if (_headingSnapshot.Count == 0)
        {
            _headingSnapshot = _db.GetHeadings(_activePageId)
                .OrderBy(h => h.SortOrder)
                .ThenBy(h => h.Title)
                .ToList();
        }
        return _headingSnapshot;
    }

    private TaskItem? FindTask(Guid taskId)
        => CurrentTasks().FirstOrDefault(t => t.Id == taskId)
           ?? _db.GetTasks(includeArchived: true).FirstOrDefault(t => t.Id == taskId);

    private Heading? FindHeading(Guid headingId)
        => CurrentHeadings().FirstOrDefault(h => h.Id == headingId)
           ?? _db.GetHeadings().FirstOrDefault(h => h.Id == headingId);

    private void TaskList_KeyDown(object sender, KeyEventArgs e)
    {
        if (IsInlineEditorFocused()) return;
        var sel = Selected;
        if (sel is null) return;

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

        if (!ctrl && !shift && !alt && key is Key.Up or Key.Down)
        {
            MoveSelectionBy(key == Key.Up ? -1 : 1);
            e.Handled = true;
            return;
        }

        if (_agendaMode
            && ((ctrl && shift && (key is Key.Up or Key.Down or Key.Left or Key.Right))
                || (alt && !ctrl && !shift && (key is Key.Up or Key.Down))))
        {
            e.Handled = true;
            return;
        }

        if (ctrl && shift)
        {
            if (sel.IsTask && (key == Key.Up || key == Key.Down))
            {
                MoveTaskWithinHeading(sel.TaskId, key == Key.Up ? -1 : 1);
                e.Handled = true;
                return;
            }

            if (sel.IsTask && (key == Key.Left || key == Key.Right))
            {
                MoveTaskToAdjacentHeading(sel.TaskId, key == Key.Left ? -1 : 1);
                e.Handled = true;
                return;
            }

            if (sel.IsHeading && !sel.IsNewHeading && (key == Key.Up || key == Key.Down))
            {
                MoveHeading(sel.HeadingId, key == Key.Up ? -1 : 1);
                e.Handled = true;
                return;
            }
        }

        if (alt && !ctrl && !shift && (key == Key.Up || key == Key.Down))
        {
            if (sel.IsTask)
            {
                MoveTaskWithinHeading(sel.TaskId, key == Key.Up ? -1 : 1);
            }
            else if (sel.IsHeading && !sel.IsNewHeading)
            {
                MoveHeading(sel.HeadingId, key == Key.Up ? -1 : 1);
            }
            e.Handled = true;
            return;
        }

        if (key == Key.Insert)
        {
            BeginCreateTaskNearSelection(after: true);
            e.Handled = true;
            return;
        }

        if (ctrl && key == Key.Enter && sel.IsTask)
        {
            if (sel.IsExpanded)
                BeginNotesEdit(sel.TaskId);
            else
                EditTask(sel.TaskId);
            e.Handled = true;
            return;
        }

        if ((key == Key.Enter || key == Key.F2) && sel.IsTask
            && _editingTaskId != sel.TaskId && _editingNotesTaskId != sel.TaskId)
        {
            BeginInlineEdit(sel.TaskId);
            e.Handled = true;
            return;
        }

        if ((key == Key.Enter || key == Key.F2) && sel.IsNewHeading)
        {
            BeginCreateHeading();
            e.Handled = true;
            return;
        }

        if (key == Key.Delete)
        {
            ExecuteDeleteSelected();
            e.Handled = true;
            return;
        }

        if (key == Key.Space)
        {
            if (sel.IsHeading && !sel.IsNewHeading)
            {
                ToggleHeadingFocus(sel.HeadingId);
            }
            else if (sel.IsTask)
            {
                ToggleExpanded(sel.TaskId);
            }
            e.Handled = true;
            return;
        }

        if (sel.IsTask && key >= Key.D1 && key <= Key.D6)
        {
            SetState(sel.TaskId, (TaskState)(key - Key.D1 + 1));
            e.Handled = true;
            return;
        }
        if (sel.IsTask && key >= Key.NumPad1 && key <= Key.NumPad6)
        {
            SetState(sel.TaskId, (TaskState)(key - Key.NumPad1 + 1));
            e.Handled = true;
            return;
        }

        if (sel.IsTask && ctrl && key == Key.D)
        {
            EditTask(sel.TaskId, focusReminder: true);
            e.Handled = true;
        }
    }

    private void ToggleSearchBar()
    {
        if (SearchBar.Visibility == Visibility.Visible)
        {
            HideSearchBar(clear: true);
        }
        else
        {
            ShowSearchBar();
        }
    }

    private void ShowSearchBar()
    {
        SearchBar.Visibility = Visibility.Visible;
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void HideSearchBar(bool clear)
    {
        if (clear && (!string.IsNullOrEmpty(_searchText) || !string.IsNullOrEmpty(SearchBox.Text)))
        {
            _searchText = "";
            SearchBox.Text = "";
            Refresh();
        }

        SearchBar.Visibility = Visibility.Collapsed;
        if (SearchBox.IsKeyboardFocusWithin)
            TaskList.Focus();
    }

    private void TaskList_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (IsInlineEditorFocused()) return;

        var ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        var alt = (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (!ctrl && !shift && !alt && key is Key.Up or Key.Down)
        {
            MoveSelectionBy(key == Key.Up ? -1 : 1);
            e.Handled = true;
        }
    }

    private void TaskList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressSelectionCollapse) return;

        var selectedTaskId = Selected is { IsTask: true } selected ? selected.TaskId : (Guid?)null;
        if (!CollapseExpandedTasksExcept(selectedTaskId)) return;
        Refresh();
    }

    private void TaskList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (IsOriginalSourceWithin<MarkdownTextBlock>(e.OriginalSource as DependencyObject)) return;
        if (Selected is { IsTask: true } sel) BeginInlineEdit(sel.TaskId);
    }

    private void TaskList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(TaskList);
        var row = RowFromOriginalSource(e.OriginalSource as DependencyObject);
        _dragSource = row switch
        {
            { IsTask: true, IsNewTask: false } => new DragPayload("task", row.TaskId),
            { IsHeading: true, IsNewHeading: false, HeadingId: { } headingId } => new DragPayload("heading", headingId),
            { IsHeading: true, IsNewHeading: false, HeadingId: null } when row.HeadingTitle.StartsWith("Inbox", StringComparison.OrdinalIgnoreCase)
                => new DragPayload("inbox", row.PageId ?? _activePageId),
            _ => null,
        };
    }

    private void TaskList_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = RowFromOriginalSource(e.OriginalSource as DependencyObject);
        if (row is not null)
        {
            TaskList.SelectedItem = row;
        }
        else
        {
            TaskList.SelectedItem = null;
        }
    }

    private void TaskList_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragSource is not { } payload) return;
        var current = e.GetPosition(TaskList);
        if (Math.Abs(current.X - _dragStart.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _dragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        try
        {
            var effects = payload.Kind == "task"
                ? DragDropEffects.Move | DragDropEffects.Copy
                : DragDropEffects.Move;
            DragDrop.DoDragDrop(TaskList, CreateDragDataObject(payload.ToString()), effects);
        }
        finally
        {
            _dragSource = null;
            ClearInsertionLine();
        }
    }

    private void TaskList_DragOver(object sender, DragEventArgs e)
    {
        if (!TryGetDragPayload(e.Data, out var payload))
        {
            e.Effects = DragDropEffects.None;
            ClearInsertionLine();
            e.Handled = true;
            return;
        }

        if (payload.Kind == "tag")
        {
            ClearInsertionLine();
            var tagTarget = RowFromOriginalSource(e.OriginalSource as DependencyObject);
            e.Effects = tagTarget is { IsTask: true } ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
            return;
        }

        var target = RowFromOriginalSource(e.OriginalSource as DependencyObject);
        if (target is null)
        {
            // Allow drop on empty page for heading/inbox/task payloads
            if (_rows.Count == 0 && payload.Kind is "heading" or "inbox" or "task")
            {
                var layer = AdornerLayer.GetAdornerLayer(TaskList);
                if (layer is not null) ShowInsertionLine(TaskList, false);
                e.Effects = DragDropEffects.Move;
            }
            else
            {
                e.Effects = DragDropEffects.None;
                ClearInsertionLine();
            }
            e.Handled = true;
            return;
        }

        if (IsSelfDrop(payload, target))
        {
            e.Effects = DragDropEffects.None;
            ClearInsertionLine();
            e.Handled = true;
            return;
        }

        var container = TaskList.ItemContainerGenerator.ContainerFromItem(target) as FrameworkElement;
        if (container is not null)
        {
            ShowInsertionLine(container, ShouldInsertAfter(payload, target, e.GetPosition(container), container.ActualHeight));
        }

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void TaskList_DragLeave(object sender, DragEventArgs e)
    {
        if (!TaskList.IsMouseOver)
        {
            ClearInsertionLine();
        }
    }

    private void TaskList_Drop(object sender, DragEventArgs e)
    {
        ClearInsertionLine();
        if (!TryGetDragPayload(e.Data, out var payload)) return;

        if (payload.Kind == "tag")
        {
            var tagTarget = RowFromOriginalSource(e.OriginalSource as DependencyObject);
            if (tagTarget is not { IsTask: true }) return;
            AssignTagToTask(tagTarget.TaskId, payload.TagName);
            return;
        }

        var target = RowFromOriginalSource(e.OriginalSource as DependencyObject);
        if (target is null)
        {
            // Drop on empty page
            if (payload.Kind == "heading") MoveHeadingToPage(payload.Id, _activePageId);
            else if (payload.Kind == "inbox") MoveInboxToPage(payload.Id, _activePageId);
            else if (payload.Kind == "task") MoveTaskToPage(payload.Id, _activePageId);
            return;
        }
        if (IsSelfDrop(payload, target)) return;

        var container = TaskList.ItemContainerGenerator.ContainerFromItem(target) as FrameworkElement;
        var after = container is not null && ShouldInsertAfter(payload, target, e.GetPosition(container), container.ActualHeight);

        if (payload.Kind == "heading")
        {
            MoveHeadingNearListTarget(payload.Id, target, after);
            return;
        }

        if (payload.Kind == "inbox")
        {
            MoveInboxToPage(payload.Id, target.PageId ?? _activePageId);
            return;
        }

        if (target.IsHeading)
        {
            MoveTaskToHeadingEnd(payload.Id, target.HeadingId, target.PageId);
        }
        else
        {
            if (after)
                MoveTaskAfterTask(payload.Id, target.TaskId);
            else
                MoveTaskBeforeTask(payload.Id, target.TaskId);
        }
    }

    private static bool IsSelfDrop(DragPayload payload, TaskRowVm target)
        => payload.Kind switch
        {
            "task" => target.IsTask && target.TaskId == payload.Id,
            "heading" => target.IsHeading && target.HeadingId == payload.Id,
            "inbox" => target.IsHeading && target.HeadingId is null && target.PageId == payload.Id,
            _ => false,
        };

    private static bool ShouldInsertAfter(DragPayload payload, TaskRowVm target, Point positionInContainer, double targetHeight)
        => payload.Kind == "task" && target.IsHeading || positionInContainer.Y > targetHeight / 2;

    private void ShowInsertionLine(FrameworkElement target, bool after)
    {
        if (_insertionElement == target && _insertionAfter == after) return;
        ClearInsertionLine();
        var layer = AdornerLayer.GetAdornerLayer(target);
        if (layer is null) return;

        _insertionElement = target;
        _insertionLayer = layer;
        _insertionAfter = after;
        _insertionAdorner = new InsertionLineAdorner(target, after);
        layer.Add(_insertionAdorner);
    }

    private void ClearInsertionLine()
    {
        if (_insertionAdorner is not null && _insertionLayer is not null)
        {
            _insertionLayer.Remove(_insertionAdorner);
        }

        _insertionAdorner = null;
        _insertionLayer = null;
        _insertionElement = null;
    }

    private void HeadingCollapse_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm { IsHeading: true, HeadingId: { } headingId }) return;
        var heading = CurrentHeadings().FirstOrDefault(h => h.Id == headingId);
        if (heading is null) return;
        heading.Collapsed = !heading.Collapsed;
        _db.SaveHeading(heading);
        Refresh();
    }

    private void StateButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm { IsTask: true } row) return;
        if (row.State == TaskState.Action) SetState(row.TaskId, TaskState.Next);
        else if (row.State == TaskState.Next) SetState(row.TaskId, TaskState.Action);
    }

    private void StateButton_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm { IsTask: true } row) return;
        var target = row.State == TaskState.Done ? TaskState.Action : TaskState.Done;
        SetState(row.TaskId, target);
        e.Handled = true;
    }

    private void StateMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { CommandParameter: TaskRowVm { IsTask: true } row, Tag: string stateName }) return;
        if (!Enum.TryParse<TaskState>(stateName, out var state)) return;
        SetState(row.TaskId, state);
    }

    private void ExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm { IsTask: true } row)
        {
            ToggleExpanded(row.TaskId);
            e.Handled = true;
        }
    }

    private void InlineTitleBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (e.Key == Key.Enter)
        {
            CommitInlineEdit(row);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CommitInlineEdit(row);
            e.Handled = true;
        }
    }

    private void TitleTextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is TextBox box)
        {
            box.SelectAll();
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

    private void HeadingTitleBox_KeyDown(object sender, KeyEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (e.Key == Key.Enter)
        {
            CommitHeadingEdit(row);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelHeadingEdit();
            e.Handled = true;
        }
    }

    private void HeadingTitleBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm { IsEditingHeading: true } row)
        {
            CommitHeadingEdit(row);
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
            box.CaretIndex = box.Text.Length;
            box.ScrollToEnd();
        });
    }

    private void CommitInlineEdit(TaskRowVm row)
    {
        if (_editingTaskId != row.TaskId) return;
        var title = row.EditTitle.Trim();
        if (row.IsNewTask)
        {
            if (title.Length == 0)
            {
                CancelCreateTask();
                return;
            }

            var newTask = _entities.CreateTask(_activePageId, title, _newTaskHeadingId, TaskState.Action, _newTaskSortOrder);
            _db.SaveTask(newTask);
            _creatingTaskId = null;
            _pendingTaskTitle = "";
            _editingTaskId = null;
            Refresh();
            TaskList.SelectedItem = _rows.FirstOrDefault(r => r.TaskId == newTask.Id);
            return;
        }

        if (title.Length == 0)
        {
            BeginInlineEdit(row.TaskId);
            return;
        }

        var task = FindTask(row.TaskId);
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
        if (_creatingTaskId == _editingTaskId)
        {
            CancelCreateTask();
            return;
        }

        _editingTaskId = null;
        Refresh();
    }

    private void BeginCreateTaskNearSelection(bool after)
    {
        var anchor = Selected;
        Guid? headingId;
        double sortOrder;

        if (anchor is { IsTask: true, IsNewTask: false })
        {
            var tasks = CurrentTasks()
                .Where(t => t.ArchivedAt is null)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Title)
                .ToList();
            var task = tasks.FirstOrDefault(t => t.Id == anchor.TaskId);
            if (task is null)
            {
                BeginCreateTaskAtEnd(ResolveDefaultNewTaskHeadingId());
                return;
            }

            var siblings = tasks.Where(t => t.HeadingId == task.HeadingId).ToList();
            var index = siblings.FindIndex(t => t.Id == task.Id);
            headingId = task.HeadingId;
            if (after)
            {
                var next = index >= 0 && index + 1 < siblings.Count ? siblings[index + 1].SortOrder : (double?)null;
                sortOrder = SortOrderMath.Between(task.SortOrder, next);
            }
            else
            {
                var previous = index > 0 ? siblings[index - 1].SortOrder : (double?)null;
                sortOrder = SortOrderMath.Between(previous, task.SortOrder);
            }
        }
        else if (anchor is { IsHeading: true, IsNewHeading: false })
        {
            headingId = anchor.HeadingId;
            sortOrder = SortOrderAtEnd(headingId);
        }
        else
        {
            headingId = ResolveDefaultNewTaskHeadingId();
            sortOrder = SortOrderAtEnd(headingId);
        }

        BeginCreateTask(headingId, sortOrder);
    }

    private void BeginCreateTaskAtEnd(Guid? headingId) => BeginCreateTask(headingId, SortOrderAtEnd(headingId));

    private Guid? ResolveDefaultNewTaskHeadingId()
    {
        if (_focusedHeadingId is { } focused && focused != Guid.Empty) return focused;
        return null;
    }

    private double SortOrderAtEnd(Guid? headingId)
    {
        var siblings = CurrentTasks()
            .Where(t => t.ArchivedAt is null && t.HeadingId == headingId)
            .OrderBy(t => t.SortOrder)
            .ToList();
        return SortOrderMath.Between(siblings.LastOrDefault()?.SortOrder, null);
    }

    private void BeginCreateTask(Guid? headingId, double sortOrder)
    {
        _creatingTaskId = Guid.NewGuid();
        _pendingTaskTitle = "";
        _newTaskHeadingId = headingId;
        _newTaskSortOrder = sortOrder;
        _editingTaskId = _creatingTaskId;
        if (headingId is { } realHeadingId)
        {
            var heading = CurrentHeadings().FirstOrDefault(h => h.Id == realHeadingId);
            if (heading is { Collapsed: true })
            {
                heading.Collapsed = false;
                _db.SaveHeading(heading);
            }
        }

        Refresh();
        var row = _rows.FirstOrDefault(r => r.TaskId == _creatingTaskId);
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

    private void CancelCreateTask()
    {
        _creatingTaskId = null;
        _pendingTaskTitle = "";
        _editingTaskId = null;
        Refresh();
    }

    private void BeginNotesEdit(Guid taskId)
    {
        _expandedTaskIds.Add(taskId);
        _editingNotesTaskId = taskId;
        _notesDrafts[taskId] = FindTask(taskId)?.Notes ?? "";
        Refresh();
        var row = _rows.FirstOrDefault(r => r.TaskId == taskId);
        if (row is null) return;
        TaskList.SelectedItem = row;
        TaskList.ScrollIntoView(row);
        Dispatcher.BeginInvoke(() =>
        {
            var container = TaskList.ItemContainerGenerator.ContainerFromItem(row) as DependencyObject;
            var box = FindVisualChildByName<TextBox>(container, "NotesEditor");
            if (box is null) return;
            box.Focus();
            box.CaretIndex = box.Text.Length;
            box.ScrollToEnd();
        });
    }

    private void CommitNotesEdit(TaskRowVm row)
    {
        if (_editingNotesTaskId != row.TaskId) return;
        var task = FindTask(row.TaskId);
        if (task is not null)
        {
            var raw = _notesDrafts.TryGetValue(row.TaskId, out var draft) ? draft : row.EditNotes;
            var notes = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
            if (task.Notes != notes)
            {
                task.Notes = notes;
                _db.SaveTask(task);
            }
        }
        _notesDrafts.Remove(row.TaskId);
        _editingNotesTaskId = null;
        Refresh();
    }

    private void CancelNotesEdit()
    {
        if (_editingNotesTaskId is { } taskId) _notesDrafts.Remove(taskId);
        _editingNotesTaskId = null;
        Refresh();
    }

    private void BeginStartEdit(Guid taskId)
    {
        _expandedTaskIds.Add(taskId);
        _editingStartAtTaskId = taskId;
        _editingDueAtTaskId = null;
        Refresh();
        FocusDatePicker(taskId, "StartDatePicker");
    }

    private void CommitStartEdit(TaskRowVm row)
    {
        if (_editingStartAtTaskId != row.TaskId) return;
        var task = FindTask(row.TaskId);
        if (task is not null)
        {
            var text = row.EditStart.Trim();
            if (string.IsNullOrEmpty(text))
                task.StartAt = null;
            else
            {
                var parsed = DateInputParser.Parse(text, _clock.UtcNow);
                if (parsed is null) { FocusDatePicker(row.TaskId, "StartDatePicker"); return; }
                task.StartAt = parsed;
            }
            _db.SaveTask(task);
        }
        _editingStartAtTaskId = null;
        Refresh();
    }

    private void CancelStartEdit()
    {
        _editingStartAtTaskId = null;
        Refresh();
    }

    private void BeginDueEdit(Guid taskId)
    {
        _expandedTaskIds.Add(taskId);
        _editingDueAtTaskId = taskId;
        _editingStartAtTaskId = null;
        Refresh();
        FocusDatePicker(taskId, "DueDatePicker");
    }

    private void CommitDueEdit(TaskRowVm row)
    {
        if (_editingDueAtTaskId != row.TaskId) return;
        var task = FindTask(row.TaskId);
        if (task is not null)
        {
            var text = row.EditDue.Trim();
            if (string.IsNullOrEmpty(text))
                task.DueAt = null;
            else
            {
                var parsed = DateInputParser.Parse(text, _clock.UtcNow);
                if (parsed is null) { FocusDatePicker(row.TaskId, "DueDatePicker"); return; }
                task.DueAt = parsed;
            }
            SyncReminderToDue(task);
            _db.SaveTask(task);
        }
        _editingDueAtTaskId = null;
        Refresh();
    }

    private void CancelDueEdit()
    {
        _editingDueAtTaskId = null;
        Refresh();
    }

    private void FocusDatePicker(Guid taskId, string pickerName)
    {
        var row = _rows.FirstOrDefault(r => r.TaskId == taskId);
        if (row is null) return;
        TaskList.SelectedItem = row;
        Dispatcher.BeginInvoke(() =>
        {
            var container = TaskList.ItemContainerGenerator.ContainerFromItem(row) as DependencyObject;
            var picker = FindVisualChildByName<DatePicker>(container, pickerName);
            if (picker is null) return;
            picker.Focus();
            picker.IsDropDownOpen = true;
        });
    }

    private void StartChip_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm row)
            BeginStartEdit(row.TaskId);
    }

    private void ClearStart_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        var task = FindTask(row.TaskId);
        if (task is not null) { task.StartAt = null; _db.SaveTask(task); Refresh(); }
    }

    private void StartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (_editingStartAtTaskId != row.TaskId) return;
        if ((sender as DatePicker)?.SelectedDate is not { } selected) return;
        var task = FindTask(row.TaskId);
        if (task is null) return;
        var existing = task.StartAt?.ToLocalTime();
        var local = selected.Date.Add(existing?.TimeOfDay ?? TimeSpan.Zero);
        task.StartAt = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
        _db.SaveTask(task);
        _editingStartAtTaskId = null;
        Refresh();
    }

    private void StartDatePicker_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelStartEdit();
            e.Handled = true;
        }
    }

    private void StartEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (e.Key == Key.Escape) { CancelStartEdit(); e.Handled = true; }
        else if (e.Key == Key.Enter) { CommitStartEdit(row); e.Handled = true; }
    }

    private void StartEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm row && _editingStartAtTaskId == row.TaskId)
            CommitStartEdit(row);
    }

    private void DueChip_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm row)
            BeginDueEdit(row.TaskId);
    }

    private void NotesText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm row)
        {
            TaskList.SelectedItem = row;
            BeginNotesEdit(row.TaskId);
            e.Handled = true;
        }
    }

    private void TitleText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm { IsTask: true } row)
        {
            return;
        }

        TaskList.SelectedItem = row;
        if (!string.IsNullOrWhiteSpace(row.Link)
            && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            OpenTaskLink(row.Link);
        }
        else
        {
            BeginInlineEdit(row.TaskId);
        }
        e.Handled = true;
    }

    private void TaskLink_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row || string.IsNullOrWhiteSpace(row.Link))
            return;

        OpenTaskLink(row.Link);
    }

    private void OpenTaskLink(string link)
    {
        try
        {
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }
        catch
        {
            MessageBox.Show(this, "The link could not be opened.", "Tasks", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void ClearDue_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        var task = FindTask(row.TaskId);
        if (task is not null) { task.DueAt = null; SyncReminderToDue(task); _db.SaveTask(task); Refresh(); }
    }

    private void PriorityCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        if (combo.DataContext is not TaskRowVm row) return;
        combo.SelectionChanged -= PriorityCombo_SelectionChanged;
        combo.SelectedIndex = row.Priority switch
        {
            TaskPriority.Low => 1,
            TaskPriority.Medium => 2,
            TaskPriority.High => 3,
            _ => 0,
        };
        combo.SelectionChanged += PriorityCombo_SelectionChanged;
    }

    private void PriorityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo) return;
        if (combo.DataContext is not TaskRowVm row) return;
        var task = FindTask(row.TaskId);
        if (task is null) return;
        task.Priority = combo.SelectedIndex switch
        {
            1 => TaskPriority.Low,
            2 => TaskPriority.Medium,
            3 => TaskPriority.High,
            _ => null,
        };
        _db.SaveTask(task);
        Refresh();
    }

    private void EffortBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box) return;
        if (box.DataContext is not TaskRowVm row) return;
        SaveEffort(row.TaskId, box.Text);
    }

    private void EffortBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            if (sender is TextBox box && box.DataContext is TaskRowVm row)
                SaveEffort(row.TaskId, box.Text);
            e.Handled = true;
        }
    }

    private void SaveEffort(Guid taskId, string text)
    {
        var task = FindTask(taskId);
        if (task is null) return;
        var effort = int.TryParse(text.Trim(), out var h) && h >= 0 ? (int?)h : null;
        if (task.EffortHours == effort) return;
        task.EffortHours = effort;
        _db.SaveTask(task);
        Refresh();
    }

    private void LinkBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box) return;
        if (box.DataContext is not TaskRowVm row) return;
        SaveLink(row.TaskId, box.Text);
    }

    private void LinkBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Escape)
        {
            if (sender is TextBox box && box.DataContext is TaskRowVm row)
                SaveLink(row.TaskId, box.Text);
            e.Handled = true;
        }
    }

    private void SaveLink(Guid taskId, string text)
    {
        var task = FindTask(taskId);
        if (task is null) return;
        var link = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (task.Link == link) return;
        task.Link = link;
        _db.SaveTask(task);
        Refresh();
    }

    private void RecurrenceCombo_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.DataContext is not TaskRowVm row) return;
        combo.SelectionChanged -= RecurrenceCombo_SelectionChanged;
        combo.SelectedIndex = TaskRecurrence.Normalize(row.Recurrence) switch
        {
            "daily" => 1,
            "weekly" => 2,
            "biweekly" => 3,
            "monthly" => 4,
            "quarterly" => 5,
            "yearly" => 6,
            _ => 0,
        };
        combo.SelectionChanged += RecurrenceCombo_SelectionChanged;
    }

    private void RecurrenceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox combo || combo.DataContext is not TaskRowVm row) return;
        if (!combo.IsKeyboardFocusWithin && !combo.IsDropDownOpen) return;
        var task = FindTask(row.TaskId);
        if (task is null) return;
        var recurrence = combo.SelectedIndex switch
        {
            1 => "daily",
            2 => "weekly",
            3 => "biweekly",
            4 => "monthly",
            5 => "quarterly",
            6 => "yearly",
            _ => null,
        };
        if (task.Recurrence == recurrence) return;
        task.Recurrence = recurrence;
        _db.SaveTask(task);
        Refresh();
    }

    private void ReminderEnabledCheck_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox check || check.DataContext is not TaskRowVm row) return;
        check.Checked -= ReminderEnabledCheck_Changed;
        check.Unchecked -= ReminderEnabledCheck_Changed;
        check.IsEnabled = row.DueAt.HasValue;
        check.IsChecked = row.ReminderActive;
        check.Checked += ReminderEnabledCheck_Changed;
        check.Unchecked += ReminderEnabledCheck_Changed;
    }

    private void ReminderEnabledCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox check || check.DataContext is not TaskRowVm row) return;
        SaveReminderFromDue(row.TaskId, check.IsChecked == true, row.ReminderAutoSnoozeEnabled);
    }

    private void ReminderNagCheck_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox check || check.DataContext is not TaskRowVm row) return;
        check.Checked -= ReminderNagCheck_Changed;
        check.Unchecked -= ReminderNagCheck_Changed;
        check.IsEnabled = row.DueAt.HasValue;
        check.IsChecked = row.ReminderActive && row.ReminderAutoSnoozeEnabled;
        check.Checked += ReminderNagCheck_Changed;
        check.Unchecked += ReminderNagCheck_Changed;
    }

    private void ReminderNagCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox check || check.DataContext is not TaskRowVm row) return;
        SaveReminderFromDue(row.TaskId, row.ReminderActive || check.IsChecked == true, check.IsChecked == true);
    }

    private void ReminderSnooze5_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row || !row.ReminderActive) return;
        _reminders.Snooze(row.TaskId, 5);
        Refresh();
    }

    private void ReminderAck_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row || !row.ReminderActive) return;
        _reminders.Acknowledge(row.TaskId);
        Refresh();
    }

    private void SaveReminderFromDue(Guid taskId, bool enabled, bool continuousNag)
    {
        var task = FindTask(taskId);
        if (task is null) return;
        if (!enabled)
        {
            _db.DeleteRemindersForTask(taskId);
            Refresh();
            return;
        }

        if (task.DueAt is null)
        {
            Refresh();
            return;
        }

        var rem = _db.GetReminderForTask(taskId) ?? _entities.CreateReminder(taskId, task.DueAt.Value);
        rem.Enabled = true;
        rem.FireAt = task.DueAt.Value;
        rem.NextFireAt = task.DueAt.Value;
        rem.Status = ReminderStatus.Active;
        rem.AutoSnoozeEnabled = continuousNag;
        rem.AutoSnoozeIntervalMinutes = 5;
        _db.SaveReminder(rem);
        Refresh();
    }

    private void SyncReminderToDue(TaskItem task)
    {
        var rem = _db.GetReminderForTask(task.Id);
        if (rem is null) return;
        if (task.DueAt is null)
        {
            _db.DeleteRemindersForTask(task.Id);
            return;
        }

        rem.FireAt = task.DueAt.Value;
        rem.NextFireAt = task.DueAt.Value;
        rem.Status = ReminderStatus.Active;
        rem.Enabled = true;
        _db.SaveReminder(rem);
    }

    private void DueDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (_editingDueAtTaskId != row.TaskId) return;
        if ((sender as DatePicker)?.SelectedDate is not { } selected) return;
        var task = FindTask(row.TaskId);
        if (task is null) return;
        var existing = task.DueAt?.ToLocalTime();
        var local = selected.Date.Add(existing?.TimeOfDay ?? TimeSpan.Zero);
        task.DueAt = DateTime.SpecifyKind(local, DateTimeKind.Local).ToUniversalTime();
        SyncReminderToDue(task);
        _db.SaveTask(task);
        _editingDueAtTaskId = null;
        Refresh();
    }

    private void DueDatePicker_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            CancelDueEdit();
            e.Handled = true;
        }
    }

    private void DueEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (e.Key == Key.Escape) { CancelDueEdit(); e.Handled = true; }
        else if (e.Key == Key.Enter) { CommitDueEdit(row); e.Handled = true; }
    }

    private void DueEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm row && _editingDueAtTaskId == row.TaskId)
            CommitDueEdit(row);
    }

    private void NotesEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        if (e.Key == Key.Escape)
        {
            if ((sender as FrameworkElement)?.DataContext is TaskRowVm row2)
                CommitNotesEdit(row2);
            else
                CancelNotesEdit();
            e.Handled = true;
        }
        else if (e.Key == Key.System && e.SystemKey == Key.Return)
        {
            // Alt+Enter: add bullet to current line (if none), then new bullet line
            if (sender is not TextBox box) return;
            var caret = box.CaretIndex;
            var text = box.Text;
            var lineStart = text.LastIndexOf('\n', Math.Max(0, caret - 1)) + 1;
            var lineText = text.Substring(lineStart, caret - lineStart);
            var hasBullet = lineText.StartsWith("- ") || lineText.StartsWith("• ");
            string insert;
            if (!hasBullet)
            {
                // Add bullet to current line then newline with bullet
                box.Text = text.Substring(0, lineStart) + "- " + text.Substring(lineStart);
                var newCaret = lineStart + 2 + (caret - lineStart);
                insert = "\n- ";
                box.Text = box.Text.Substring(0, newCaret) + insert + box.Text.Substring(newCaret);
                box.CaretIndex = newCaret + insert.Length;
            }
            else
            {
                var prefix = lineText.StartsWith("- ") ? "- " : "• ";
                insert = "\n" + prefix;
                box.Text = text.Substring(0, caret) + insert + text.Substring(caret);
                box.CaretIndex = caret + insert.Length;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Shift) == 0)
        {
            CommitNotesEdit(row);
            e.Handled = true;
        }
    }

    private void NotesEditor_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox box && box.DataContext is TaskRowVm row && _editingNotesTaskId == row.TaskId)
        {
            _notesDrafts[row.TaskId] = box.Text;
        }
    }

    private void NotesEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm row && _editingNotesTaskId == row.TaskId)
        {
            CommitNotesEdit(row);
        }
    }

    private void ToggleExpanded(Guid taskId)
    {
        var selectedKey = $"t:{taskId}";
        if (!_expandedTaskIds.Add(taskId))
        {
            _expandedTaskIds.Remove(taskId);
            _settingsExpandedTaskIds.Remove(taskId);
        }
        Refresh();
        Dispatcher.BeginInvoke(() =>
        {
            var row = _rows.FirstOrDefault(r => r.RowKey == selectedKey);
            if (row is null) return;
            var index = _rows.IndexOf(row);
            TaskList.SelectedIndex = index;
            TaskList.ScrollIntoView(row);
            TaskList.UpdateLayout();
            Keyboard.Focus(TaskList);
        }, DispatcherPriority.ContextIdle);
    }

    private void SettingsExpandButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not TaskRowVm row) return;
        ToggleTaskSettings(row.TaskId);
        e.Handled = true;
    }

    private void ToggleTaskSettings(Guid taskId)
    {
        _expandedTaskIds.Add(taskId);
        if (!_settingsExpandedTaskIds.Add(taskId))
            _settingsExpandedTaskIds.Remove(taskId);

        Refresh();
        SelectTaskRow(taskId);
    }

    private void MoveSelectionBy(int delta)
    {
        if (_rows.Count == 0) return;

        var current = TaskList.SelectedItem as TaskRowVm;
        var index = current is null ? TaskList.SelectedIndex : _rows.IndexOf(current);
        if (index < 0) index = TaskList.SelectedIndex;
        if (index < 0) index = delta > 0 ? -1 : _rows.Count;

        var nextIndex = Math.Clamp(index + delta, 0, _rows.Count - 1);
        var next = _rows[nextIndex];
        TaskList.SelectedIndex = nextIndex;
        TaskList.SelectedItem = next;
        TaskList.ScrollIntoView(next);
        TaskList.Focus();
    }

    private bool CollapseExpandedTasksExcept(Guid? selectedTaskId)
    {
        if (_expandedTaskIds.Count == 0 && _settingsExpandedTaskIds.Count == 0) return false;

        var changed = _expandedTaskIds.RemoveWhere(id => selectedTaskId != id) > 0;
        changed |= _settingsExpandedTaskIds.RemoveWhere(id => selectedTaskId != id) > 0;

        if (_editingNotesTaskId is { } notesTaskId && selectedTaskId != notesTaskId)
        {
            _editingNotesTaskId = null;
            _notesDrafts.Remove(notesTaskId);
            changed = true;
        }

        if (_editingStartAtTaskId is { } startTaskId && selectedTaskId != startTaskId)
        {
            _editingStartAtTaskId = null;
            changed = true;
        }

        if (_editingDueAtTaskId is { } dueTaskId && selectedTaskId != dueTaskId)
        {
            _editingDueAtTaskId = null;
            changed = true;
        }

        return changed;
    }

    private bool CollapseAllExpandedTasks()
    {
        if (_expandedTaskIds.Count == 0
            && _settingsExpandedTaskIds.Count == 0
            && _editingNotesTaskId is null
            && _editingStartAtTaskId is null
            && _editingDueAtTaskId is null)
        {
            return false;
        }

        _expandedTaskIds.Clear();
        _settingsExpandedTaskIds.Clear();
        if (_editingNotesTaskId is { } notesTaskId)
        {
            _notesDrafts.Remove(notesTaskId);
            _editingNotesTaskId = null;
        }
        _editingStartAtTaskId = null;
        _editingDueAtTaskId = null;
        return true;
    }

    private void SelectTaskRow(Guid taskId)
    {
        Dispatcher.BeginInvoke(() =>
        {
            var row = _rows.FirstOrDefault(r => r.TaskId == taskId);
            if (row is null) return;
            TaskList.SelectedItem = row;
            TaskList.ScrollIntoView(row);
            Keyboard.Focus(TaskList);
        }, DispatcherPriority.ContextIdle);
    }

    private void ToggleHeadingFocus(Guid? headingId)
    {
        var key = headingId ?? Guid.Empty;
        _focusedHeadingId = _focusedHeadingId == key ? null : key;
        Refresh();
    }

    private void EditTask(Guid taskId, bool focusReminder = false)
    {
        var task = FindTask(taskId);
        if (task is null) return;
        var dlg = new TaskEditorWindow(_db, _clock, _entities, task, focusReminder) { Owner = this };
        if (dlg.ShowDialog() == true) Refresh();
    }

    private void SetState(Guid taskId, TaskState state)
    {
        var task = FindTask(taskId);
        if (task is null) return;
        var wasDone = task.State == TaskState.Done;
        var wasArchived = task.State == TaskState.Archived || task.ArchivedAt is not null;
        var existingReminder = _db.GetReminderForTask(taskId);
        task.State = state;
        if (state == TaskState.Done)
        {
            task.CompletedAt ??= _clock.UtcNow;
            _reminders.Acknowledge(taskId);
            _recentlyCompletedAt[taskId] = DateTime.UtcNow;
            CreateNextRecurringTask(task, existingReminder);
        }
        else if (state == TaskState.Archived)
        {
            task.ArchivedAt ??= _clock.UtcNow;
            _reminders.Acknowledge(taskId);
        }
        else if (wasDone)
        {
            task.CompletedAt = null;
            _recentlyCompletedAt.Remove(taskId);
        }
        if (state != TaskState.Archived && wasArchived)
        {
            task.ArchivedAt = null;
        }

        _db.SaveTask(task);
        Refresh();
    }

    private void CreateNextRecurringTask(TaskItem completedTask, Reminder? previousReminder)
    {
        var recurrence = TaskRecurrence.Normalize(completedTask.Recurrence);
        if (recurrence is null) return;

        var nextDue = TaskRecurrence.NextUtc(completedTask.DueAt ?? completedTask.CompletedAt ?? _clock.UtcNow, recurrence);
        var nextStart = completedTask.StartAt is { } start
            ? TaskRecurrence.NextUtc(start, recurrence)
            : null;

        var next = _entities.CreateTask(
            completedTask.PageId,
            completedTask.Title,
            completedTask.HeadingId,
            TaskState.Action,
            SortOrderAtEndInPage(completedTask.PageId, completedTask.HeadingId));
        next.Notes = completedTask.Notes;
        next.StartAt = nextStart;
        next.DueAt = nextDue;
        next.Recurrence = recurrence;
        next.Link = completedTask.Link;
        next.Priority = completedTask.Priority;
        next.EffortHours = completedTask.EffortHours;
        _db.SaveTask(next);

        if (previousReminder is not null && next.DueAt is { } due)
        {
            var rem = _entities.CreateReminder(next.Id, due);
            rem.AutoSnoozeEnabled = previousReminder.AutoSnoozeEnabled;
            rem.AutoSnoozeIntervalMinutes = previousReminder.AutoSnoozeIntervalMinutes;
            _db.SaveReminder(rem);
        }
    }

    private static TaskState CycleStateForward(TaskState state, int delta)
        => StateCycle.Move(state, delta);

    private void CreateHeading()
    {
        BeginCreateHeading();
    }

    private void BeginCreateHeading()
    {
        _creatingHeading = true;
        _pendingHeadingTitle = "";
        _focusedHeadingId = null;
        Refresh();
        var row = _rows.FirstOrDefault(r => r.IsNewHeading);
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

    private void CommitHeadingEdit(TaskRowVm row)
    {
        if (!_creatingHeading || !row.IsNewHeading) return;
        var title = row.EditHeadingTitle.Trim();
        if (title.Length == 0)
        {
            _pendingHeadingTitle = "";
            BeginCreateHeading();
            return;
        }

        _db.SaveHeading(_entities.CreateHeading(_activePageId, title));
        _creatingHeading = false;
        _pendingHeadingTitle = "";
        Refresh();
    }

    private void CancelHeadingEdit()
    {
        _creatingHeading = false;
        _pendingHeadingTitle = "";
        Refresh();
    }

    private void MoveTaskWithinHeading(Guid taskId, int delta)
    {
        var tasks = CurrentTasks()
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
        var task = FindTask(taskId);
        if (task is null) return;
        var headingIds = new List<Guid?> { null };
        headingIds.AddRange(CurrentHeadings().OrderBy(h => h.SortOrder).Select(h => (Guid?)h.Id));
        var index = headingIds.FindIndex(id => id == task.HeadingId);
        if (index < 0) index = 0;
        var targetIndex = index + delta;
        if (targetIndex < 0 || targetIndex >= headingIds.Count) return;

        var targetHeadingId = headingIds[targetIndex];
        task.HeadingId = targetHeadingId;
        var siblings = CurrentTasks()
            .Where(t => t.HeadingId == targetHeadingId && t.Id != task.Id)
            .OrderBy(t => t.SortOrder)
            .ToList();
        task.SortOrder = SortOrderMath.Between(siblings.LastOrDefault()?.SortOrder, null);
        _db.SaveTask(task);
        Refresh();
    }

    private void MoveTaskToPage(Guid taskId, Guid pageId)
    {
        var task = FindTask(taskId);
        if (task is null) return;
        task.PageId = pageId;
        task.HeadingId = null;
        task.SortOrder = SortOrderAtEndInPage(pageId, headingId: null);
        _db.SaveTask(task);
        Refresh();
    }

    private void MoveHeading(Guid? headingId, int delta)
    {
        if (!headingId.HasValue) return;
        var source = FindHeading(headingId.Value);
        if (source is null) return;
        var headings = _db.GetHeadings(source.PageId).OrderBy(h => h.SortOrder).ThenBy(h => h.Title).ToList();
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

    private void MoveHeadingNearHeading(Guid headingId, Guid targetHeadingId, bool after)
    {
        var heading = FindHeading(headingId);
        var target = FindHeading(targetHeadingId);
        if (heading is null || target is null) return;

        if (TaskBoardMoves.MoveHeadingNearHeading(
                heading,
                target,
                after,
                _db.GetHeadings(target.PageId),
                _db.GetTasks(includeArchived: true),
                out var movedTasks))
        {
            _db.SaveHeading(heading);
            SaveTasks(movedTasks);
            Refresh();
        }
    }

    private void MoveHeadingNearListTarget(Guid headingId, TaskRowVm target, bool after)
    {
        if (target.IsHeading)
        {
            if (target.HeadingId is { } targetHeadingId)
            {
                MoveHeadingNearHeading(headingId, targetHeadingId, after);
            }
            else
            {
                MoveHeadingToPage(headingId, target.PageId ?? _activePageId);
            }

            return;
        }

        var targetTask = FindTask(target.TaskId);
        if (targetTask?.HeadingId is { } containingHeadingId)
        {
            MoveHeadingNearHeading(headingId, containingHeadingId, after);
        }
        else
        {
            MoveHeadingToPage(headingId, target.PageId ?? _activePageId);
        }
    }

    private void MoveHeadingToPage(Guid headingId, Guid pageId)
    {
        var heading = FindHeading(headingId);
        if (heading is null) return;

        TaskBoardMoves.MoveHeadingToPage(
            heading,
            pageId,
            _db.GetHeadings(pageId),
            _db.GetTasks(includeArchived: true),
            out var movedTasks);
        _db.SaveHeading(heading);
        SaveTasks(movedTasks);

        Refresh();
    }

    private void SaveTasks(IEnumerable<TaskItem> tasks)
    {
        foreach (var task in tasks) _db.SaveTask(task);
    }

    private void MoveInboxToPage(Guid sourcePageId, Guid targetPageId)
    {
        var movedTasks = TaskBoardMoves.MoveInboxToPage(sourcePageId, targetPageId, _db.GetTasks(includeArchived: true));
        if (movedTasks.Count == 0) return;

        SaveTasks(movedTasks);
        Refresh();
    }

    private void MoveTaskToHeadingEnd(Guid taskId, Guid? headingId, Guid? explicitPageId = null)
    {
        var task = FindTask(taskId);
        if (task is null) return;

        var pageId = explicitPageId ?? _activePageId;
        if (headingId.HasValue)
        {
            var heading = FindHeading(headingId.Value);
            if (heading is null) return;
            pageId = heading.PageId;
        }

        var siblings = _db.GetTasks(includeArchived: true, pageId: pageId)
            .Where(t => t.HeadingId == headingId && t.Id != taskId)
            .OrderBy(t => t.SortOrder)
            .ToList();
        task.PageId = pageId;
        task.HeadingId = headingId;
        task.SortOrder = SortOrderMath.Between(siblings.LastOrDefault()?.SortOrder, null);
        _db.SaveTask(task);
        Refresh();
    }

    private void MoveTaskBeforeTask(Guid taskId, Guid targetTaskId)
    {
        var tasks = _db.GetTasks(includeArchived: true)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .ToList();
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        var target = tasks.FirstOrDefault(t => t.Id == targetTaskId);
        if (task is null || target is null) return;
        var siblings = tasks.Where(t => t.PageId == target.PageId && t.HeadingId == target.HeadingId && t.Id != taskId).ToList();
        var targetIndex = siblings.FindIndex(t => t.Id == targetTaskId);
        var previous = targetIndex > 0 ? siblings[targetIndex - 1].SortOrder : (double?)null;
        var next = target.SortOrder;
        task.PageId = target.PageId;
        task.HeadingId = target.HeadingId;
        task.SortOrder = SortOrderMath.Between(previous, next);
        _db.SaveTask(task);
        Refresh();
    }

    private void MoveTaskAfterTask(Guid taskId, Guid targetTaskId)
    {
        var tasks = _db.GetTasks(includeArchived: true)
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Title)
            .ToList();
        var task = tasks.FirstOrDefault(t => t.Id == taskId);
        var target = tasks.FirstOrDefault(t => t.Id == targetTaskId);
        if (task is null || target is null) return;
        var siblings = tasks.Where(t => t.PageId == target.PageId && t.HeadingId == target.HeadingId && t.Id != taskId).ToList();
        var targetIndex = siblings.FindIndex(t => t.Id == targetTaskId);
        var previous = target.SortOrder;
        var next = targetIndex >= 0 && targetIndex + 1 < siblings.Count ? siblings[targetIndex + 1].SortOrder : (double?)null;
        task.PageId = target.PageId;
        task.HeadingId = target.HeadingId;
        task.SortOrder = SortOrderMath.Between(previous, next);
        _db.SaveTask(task);
        Refresh();
    }

    private double SortOrderAtEndInPage(Guid pageId, Guid? headingId)
    {
        var tasks = _db.GetTasks(includeArchived: true, pageId: pageId)
            .Where(t => t.ArchivedAt is null && t.HeadingId == headingId)
            .OrderBy(t => t.SortOrder)
            .ToList();
        return SortOrderMath.Between(tasks.LastOrDefault()?.SortOrder, null);
    }

    private double SortOrderAtEndForPageHeadings(Guid pageId)
        => TaskBoardMoves.SortOrderAtEndForPageHeadings(pageId, _db.GetHeadings(pageId));

    private void SwitchPage(int delta)
    {
        if (_pages.Count == 0) RefreshPages();
        if (_pages.Count == 0) return;
        var index = _pages.FindIndex(p => p.Id == _activePageId);
        if (index < 0) index = 0;
        var next = (index + delta + _pages.Count) % _pages.Count;
        SetActivePage(_pages[next]);
    }

    private void SwitchPageIncludingAgenda(int delta)
    {
        if (_pages.Count == 0) RefreshPages();
        if (_pages.Count == 0 && !_showAgendaTab) return;

        var count = _pages.Count + (_showAgendaTab ? 1 : 0);
        if (count == 0) return;

        var index = _agendaMode
            ? 0
            : _pages.FindIndex(p => p.Id == _activePageId) + (_showAgendaTab ? 1 : 0);
        if (index < 0) index = _showAgendaTab ? 0 : 0;

        var next = (index + delta + count) % count;
        if (_showAgendaTab && next == 0)
        {
            SetAgendaMode();
            return;
        }

        var pageIndex = next - (_showAgendaTab ? 1 : 0);
        if (pageIndex >= 0 && pageIndex < _pages.Count)
            SetActivePage(_pages[pageIndex]);
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

    private static readonly string[] FilterViewByIndex =
    [
        "",                  // 0 — unused
        "Only Next",         // Ctrl+1
        "Actions + Next",    // Ctrl+2
        "All except Done",   // Ctrl+3
        "Show All",          // Ctrl+4
        "Only On Hold",      // Ctrl+5
        "Only Waiting For",  // Ctrl+6
        "Only Someday/Maybe",// Ctrl+7
        "Only Completed",    // Ctrl+8
    ];

    private void SetFilterView(int index)
    {
        if (index < 1 || index >= FilterViewByIndex.Length) return;
        var mode = FilterViewByIndex[index];
        FilterCombo.SelectedItem = mode;
        // FilterCombo_SelectionChanged will set _filterMode and call Refresh
    }

    private void SaveCurrentPageViewState()
    {
        var page = _db.GetPage(_activePageId);
        if (page is null) return;
        page.LastFilterView = _filterMode;
        page.LastFocusedHeadingId = _focusedHeadingId == Guid.Empty ? null : _focusedHeadingId;
        page.LastSearchText = "";
        _db.SavePage(page, enqueueSync: false);
    }

    private void RestoreWindowPlacement()
    {
        Width = ReadWindowDouble("window_width") ?? DefaultWindowWidth;
        Height = ReadWindowDouble("window_height") ?? DefaultWindowHeight;
        if (ReadWindowDouble("window_left") is { } left) Left = left;
        if (ReadWindowDouble("window_top") is { } top) Top = top;

        if (!IsWindowMostlyOnScreen())
        {
            Left = Math.Max(0, SystemParameters.WorkArea.Left + 40);
            Top = Math.Max(0, SystemParameters.WorkArea.Top + 40);
        }
    }

    private void SaveWindowPlacement()
    {
        if (WindowState == WindowState.Minimized) return;

        var bounds = WindowState == WindowState.Maximized ? RestoreBounds : new Rect(Left, Top, Width, Height);
        SaveWindowDouble("window_left", bounds.Left);
        SaveWindowDouble("window_top", bounds.Top);
        SaveWindowDouble("window_width", Math.Max(MinWidth, bounds.Width));
        SaveWindowDouble("window_height", Math.Max(MinHeight, bounds.Height));
    }

    private bool IsWindowMostlyOnScreen()
    {
        var rect = new Rect(Left, Top, Width, Height);
        return rect.Right > SystemParameters.VirtualScreenLeft
               && rect.Bottom > SystemParameters.VirtualScreenTop
               && rect.Left < SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
               && rect.Top < SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight;
    }

    private double? ReadWindowDouble(string key)
        => double.TryParse(_db.GetSetting(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : null;

    private void SaveWindowDouble(string key, double value)
        => _db.SaveSetting(key, value.ToString("R", CultureInfo.InvariantCulture));

    private void RestorePageViewState(DomainPage page)
    {
        _filterMode = page.LastFilterView ?? "All";
        _includeDone = _filterMode is "Show All" or "Only Completed" or "Archived";
        _forecastMode = false;
        _inboxOnly = false;
        _focusedHeadingId = page.LastFocusedHeadingId;
        _searchText = "";
        SearchBox.Text = _searchText;
        SearchBar.Visibility = Visibility.Collapsed;
        FilterCombo.SelectionChanged -= FilterCombo_SelectionChanged;
        FilterCombo.SelectedItem = _filterMode;
        FilterCombo.SelectionChanged += FilterCombo_SelectionChanged;
    }

    private static bool IsInlineEditorFocused()
        => Keyboard.FocusedElement is TextBox tb && tb.Name is not "SearchBox";

    private static string ToDateInputLocal(DateTime utc) => utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

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

    private static T? FindVisualChildByName<T>(DependencyObject? parent, string name) where T : FrameworkElement
    {
        if (parent is null) return null;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T fe && fe.Name == name) return fe;
            var nested = FindVisualChildByName<T>(child, name);
            if (nested is not null) return nested;
        }
        return null;
    }

    // ── Multi-select delete with undo ─────────────────────────────────────────
    private void ExecuteDeleteSelected()
    {
        if (IsInlineEditorFocused()) return;
        var allSelected = TaskList.SelectedItems.OfType<TaskRowVm>().ToList();
        if (allSelected.Count == 0) return;

        var deletedTasks = new List<TaskItem>();
        var deletedHeadings = new List<Heading>();
        var orphanedTasks = new List<(Guid, Guid?)>();
        var explicitlyDeletedTaskIds = new HashSet<Guid>();

        // Collect tasks to delete first
        foreach (var row in allSelected.Where(r => r.IsTask && !r.IsNewTask))
        {
            var task = FindTask(row.TaskId);
            if (task is null) continue;
            deletedTasks.Add(task);
            explicitlyDeletedTaskIds.Add(task.Id);
        }

        // Collect headings; orphan their tasks (unless already being deleted)
        foreach (var row in allSelected.Where(r => r.IsHeading && !r.IsNewHeading && r.HeadingId.HasValue))
        {
            var heading = FindHeading(row.HeadingId!.Value);
            if (heading is null) continue;
            deletedHeadings.Add(heading);
            var headingTasks = CurrentTasks()
                .Where(t => t.HeadingId == heading.Id && t.ArchivedAt is null
                            && !explicitlyDeletedTaskIds.Contains(t.Id))
                .ToList();
            foreach (var t in headingTasks)
            {
                orphanedTasks.Add((t.Id, t.HeadingId));
                t.HeadingId = null;
                _db.SaveTask(t);
            }
        }

        if (deletedTasks.Count == 0 && deletedHeadings.Count == 0) return;

        foreach (var task in deletedTasks)
        {
            _db.DeleteTask(task.Id);
            _db.DeleteRemindersForTask(task.Id);
        }
        foreach (var heading in deletedHeadings)
            _db.DeleteHeading(heading.Id);

        _pendingUndo = new UndoRecord(deletedTasks, deletedHeadings, orphanedTasks);

        var totalItems = deletedTasks.Count + deletedHeadings.Count;
        var label = totalItems == 1
            ? (deletedHeadings.Count == 1 ? "Heading deleted." : "Task deleted.")
            : $"{totalItems} items deleted.";
        ShowUndoBar(label);
        Refresh();
    }

    private void ShowUndoBar(string message)
    {
        UndoMessage.Text = message;
        UndoBar.Visibility = Visibility.Visible;

        _undoTimer?.Stop();
        _undoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _undoTimer.Tick += (_, _) => { _undoTimer.Stop(); HideUndoBar(); };
        _undoTimer.Start();
    }

    private void HideUndoBar()
    {
        UndoBar.Visibility = Visibility.Collapsed;
        _pendingUndo = null;
    }

    private void UndoButton_Click(object sender, RoutedEventArgs e)
    {
        _undoTimer?.Stop();
        if (_pendingUndo is not { } undo) { HideUndoBar(); return; }

        foreach (var task in undo.DeletedTasks)
        {
            task.DeletedAt = null;
            _db.SaveTask(task);
        }
        foreach (var heading in undo.DeletedHeadings)
        {
            heading.DeletedAt = null;
            _db.SaveHeading(heading);
        }
        foreach (var (taskId, originalHeadingId) in undo.OrphanedTasks)
        {
            var task = _db.GetTasks(includeArchived: true)
                .FirstOrDefault(t => t.Id == taskId);
            if (task is null) continue;
            task.HeadingId = originalHeadingId;
            _db.SaveTask(task);
        }

        HideUndoBar();
        Refresh();
    }

    // ── Sync placeholder ──────────────────────────────────────────────────────
    private void SyncButton_Click(object sender, RoutedEventArgs e)
    {
        var enabled = _db.GetSetting("sync_enabled") == "1";
        var url = _db.GetSetting("supabase_url");
        var key = _db.GetSetting("supabase_publishable_key");
        if (!enabled || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
        {
            MessageBox.Show(this,
                "Supabase sync is not configured yet. Open Settings to enable sync and add the project URL plus publishable key.",
                "Sync",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        MessageBox.Show(this,
            "Supabase settings are saved. Remote sync still needs the project schema, user sign-in/session wiring, and the outbox transport before data can be replicated safely.",
            "Sync",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ── Due-date double-click opens inline picker ─────────────────────────────
    private void DueText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if ((sender as FrameworkElement)?.DataContext is TaskRowVm { IsTask: true } row)
        {
            BeginDueEdit(row.TaskId);
            e.Handled = true;
        }
    }

    // ── Inline page rename ────────────────────────────────────────────────────
    private void PageTab_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DomainPage page) return;
        _renamingPageId = page.Id;
        Refresh();
        e.Handled = true;
    }

    private void PageNameEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not DomainPage page) return;
        if (e.Key == Key.Enter) { CommitPageRename(page, (TextBox)sender); e.Handled = true; }
        else if (e.Key == Key.Escape) { _renamingPageId = null; Refresh(); e.Handled = true; }
    }

    private void PageNameEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is DomainPage page)
            CommitPageRename(page, (TextBox)sender);
    }

    private void CommitPageRename(DomainPage page, TextBox box)
    {
        if (_renamingPageId != page.Id) return;
        var name = box.Text.Trim();
        _renamingPageId = null;
        if (string.IsNullOrEmpty(name)) { Refresh(); return; }
        if (name == page.Name) { Refresh(); return; }
        page.Name = name;
        try { _db.SavePage(page); }
        catch (Microsoft.Data.Sqlite.SqliteException)
        {
            MessageBox.Show(this, "A page with that name already exists.", "Pages", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        Refresh();
    }

    // ── New tag inline ────────────────────────────────────────────────────────
    private void NewTagMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _creatingNewTag = true;
        Refresh();
        Dispatcher.BeginInvoke(() =>
        {
            var box = TagBar.Children.OfType<TextBox>().FirstOrDefault();
            box?.Focus();
        });
    }

    private void NewTagEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        if (e.Key == Key.Enter) { CommitNewTag(box.Text); e.Handled = true; }
        else if (e.Key == Key.Escape) { _creatingNewTag = false; Refresh(); e.Handled = true; }
    }

    private void NewTagEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box) CommitNewTag(box.Text);
    }

    private void CommitNewTag(string text)
    {
        _creatingNewTag = false;
        var name = text.Trim().TrimStart('@');
        if (!string.IsNullOrEmpty(name))
            _db.AddTag(_activePageId, name);
        Refresh();
    }

    private static TaskRowVm? RowFromOriginalSource(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: TaskRowVm row }) return row;
            if (source is FrameworkContentElement { DataContext: TaskRowVm contentRow }) return contentRow;
            source = GetParentObject(source);
        }
        return null;
    }

    private static bool IsOriginalSourceWithin<T>(DependencyObject? source) where T : DependencyObject
    {
        while (source is not null)
        {
            if (source is T) return true;
            source = GetParentObject(source);
        }

        return false;
    }

    private static DependencyObject? GetParentObject(DependencyObject source)
    {
        if (source is Visual or System.Windows.Media.Media3D.Visual3D)
            return VisualTreeHelper.GetParent(source);

        if (source is ContentElement content)
        {
            var parent = ContentOperations.GetParent(content);
            if (parent is not null) return parent;

            if (content is FrameworkContentElement frameworkContent)
                return frameworkContent.Parent;
        }

        return null;
    }
}
