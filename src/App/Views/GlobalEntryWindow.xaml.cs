using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;

namespace WindowsTrayTasks.Views;

public partial class GlobalEntryWindow : Window
{
    private readonly Database _db;
    private readonly IClock _clock;
    private readonly EntityFactory _entities;
    private readonly Action _onSaved;

    public GlobalEntryWindow(Database db, IClock clock, EntityFactory entities, Action onSaved)
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
        HintText.Text = "Save: Enter · Cancel: Esc · Dates: tomorrow 9, +2h, 2026-05-10";
        TitleBox.Focus();
    }

    public void ShowEntry()
    {
        TitleBox.Text = "";
        NotesBox.Text = "";
        StartBox.Text = "";
        DueBox.Text = "";
        HintText.Text = "Save: Enter · Cancel: Esc · Dates: tomorrow 9, +2h, 2026-05-10";
        if (!IsVisible) Show();
        Activate();
        TitleBox.Focus();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => HideEntry();

    private void Save_Click(object sender, RoutedEventArgs e) => TrySave();

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { HideEntry(); e.Handled = true; return; }
        base.OnKeyDown(e);
    }

    private void TrySave()
    {
        var title = TitleBox.Text.Trim();
        if (string.IsNullOrEmpty(title)) { TitleBox.Focus(); return; }

        DateTime? startUtc = null;
        if (!string.IsNullOrWhiteSpace(StartBox.Text))
        {
            startUtc = DateInputParser.Parse(StartBox.Text.Trim(), _clock.UtcNow);
            if (startUtc is null)
            {
                HintText.Text = "⚠ Start date not recognized.";
                StartBox.Focus();
                return;
            }
        }

        DateTime? dueUtc = null;
        if (!string.IsNullOrWhiteSpace(DueBox.Text))
        {
            dueUtc = DateInputParser.Parse(DueBox.Text.Trim(), _clock.UtcNow);
            if (dueUtc is null)
            {
                HintText.Text = "⚠ Due date not recognized.";
                DueBox.Focus();
                return;
            }
        }

        var pageId = GetOrCreateInboxPage();
        var task = _entities.CreateTask(pageId, title, headingId: null, TaskState.Action);
        task.Notes = string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim();
        task.StartAt = startUtc;
        task.DueAt = dueUtc;
        _db.SaveTask(task);

        _onSaved();
        HideEntry();
    }

    private Guid GetOrCreateInboxPage()
    {
        const string InboxName = "Inbox";
        var pages = _db.GetPages();
        var inbox = pages.FirstOrDefault(p =>
            p.Name.Equals(InboxName, StringComparison.OrdinalIgnoreCase));
        if (inbox is not null) return inbox.Id;

        var newPage = _entities.CreatePage(InboxName);
        _db.SavePage(newPage);
        return newPage.Id;
    }

    private void HideEntry() => Hide();
}
