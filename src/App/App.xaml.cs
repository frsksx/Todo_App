using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Hotkeys;
using WindowsTrayTasks.Infrastructure;
using WindowsTrayTasks.Infrastructure.Persistence;
using WindowsTrayTasks.Reminders;
using WindowsTrayTasks.Shell;
using WindowsTrayTasks.Tray;
using WindowsTrayTasks.Views;

namespace WindowsTrayTasks;

public partial class App : Application
{
    private SingleInstance? _singleInstance;
    private IClock? _clock;
    private IIdGenerator? _ids;
    private EntityFactory? _entities;
    private Database? _db;
    private ReminderEngine? _reminders;
    private TrayIconManager? _tray;
    private GlobalHotkeyService? _hotkeys;
    private MainWindow? _main;
    private QuickAddWindow? _quickAdd;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstance();
        if (!_singleInstance.IsFirstInstance)
        {
            // Forward intent (best-effort) to existing instance and exit.
            var arg = e.Args.FirstOrDefault() ?? "show";
            SingleInstance.SendMessage(arg);
            Shutdown();
            return;
        }
        _singleInstance.MessageReceived += msg => Dispatcher.BeginInvoke(() => HandleIpc(msg));
        _singleInstance.StartListener();

        DispatcherUnhandledException += (_, ex) =>
        {
            ex.Handled = true;
            MessageBox.Show($"Unexpected error: {ex.Exception.Message}\n\n{ex.Exception.StackTrace}", "Tray Tasks");
        };

        _clock = new SystemClock();
        _ids = new SystemIdGenerator();
        _entities = new EntityFactory(_clock, _ids);
        _db = new Database(_clock, ids: _ids);
        _reminders = new ReminderEngine(_db, _clock, Dispatcher);
        _tray = new TrayIconManager();
        _hotkeys = new GlobalHotkeyService();

        _main = new MainWindow(_db, _reminders, _clock, _entities, () => ShowQuickAdd());
        _quickAdd = new QuickAddWindow(_db, _clock, _entities, () =>
        {
            _main!.Refresh();
            UpdateTrayState();
        });

        WireTray();
        WireHotkeys();
        WireReminders();

        _reminders.Start();
        UpdateTrayState();
    }

    private void WireTray()
    {
        _tray!.OpenMainRequested += () => Dispatcher.BeginInvoke(ShowMain);
        _tray.QuickAddRequested += () => Dispatcher.BeginInvoke(ShowQuickAdd);
        _tray.OverdueRequested += () => Dispatcher.BeginInvoke(() => { ShowMain(); _main!.Title = "Tasks"; });
        _tray.SnoozeAllRequested += () => { _reminders!.SnoozeAll(5); UpdateTrayState(); _tray.ShowBalloon("Snoozed", "All reminders snoozed 5 minutes"); };
        _tray.PauseToggleRequested += () =>
        {
            _reminders!.TogglePause();
            UpdateTrayState();
            _tray.ShowBalloon(_reminders.IsPaused ? "Reminders paused" : "Reminders resumed", "");
        };
        _tray.QuitRequested += () => Dispatcher.BeginInvoke(QuitApp);
    }

    private void WireHotkeys()
    {
        var conflicts = new System.Collections.Generic.List<string>();
        if (!_hotkeys!.TryRegister(HotkeyModifiers.Control | HotkeyModifiers.Alt, Key.Q, ShowQuickAdd, out var err1))
            conflicts.Add(err1!);
        if (!_hotkeys.TryRegister(HotkeyModifiers.Control | HotkeyModifiers.Alt, Key.T, ShowMain, out var err2))
            conflicts.Add(err2!);

        if (conflicts.Count > 0)
        {
            _tray!.ShowBalloon("Hotkey conflict",
                "One or more global hotkeys could not be registered. Use the tray menu instead.\n" + string.Join("\n", conflicts));
        }
    }

    private void WireReminders()
    {
        _reminders!.RemindersFired += fired =>
        {
            UpdateTrayState();
            if (fired.Count == 1)
            {
                var task = _db!.GetTasks(includeArchived: false).FirstOrDefault(t => t.Id == fired[0].TaskId);
                _tray!.ShowBalloon("Reminder", task?.Title ?? "Reminder due");
            }
            else if (fired.Count > 1)
            {
                _tray!.ShowBalloon($"{fired.Count} reminders due", "Click the tray icon to review");
            }
        };
        _reminders.StateChanged += () => Dispatcher.BeginInvoke(UpdateTrayState);
    }

    private void UpdateTrayState()
    {
        var snap = _reminders!.Snapshot();
        TrayState state;
        string tip;
        if (_reminders.IsPaused) { state = TrayState.Paused; tip = "Tray Tasks · reminders paused"; }
        else if (snap.OverdueCount > 0) { state = TrayState.Overdue; tip = $"Tray Tasks · {snap.OverdueCount} overdue"; }
        else if (snap.NextFireAtUtc is { } nf && nf - _clock!.UtcNow < TimeSpan.FromMinutes(15))
        { state = TrayState.DueSoon; tip = $"Tray Tasks · next at {nf.ToLocalTime():HH:mm}"; }
        else if (snap.ActiveCount > 0)
        { state = TrayState.Scheduled; tip = $"Tray Tasks · {snap.ActiveCount} reminders"; }
        else { state = TrayState.Idle; tip = "Tray Tasks · no reminders"; }
        _tray!.SetState(state, tip);
    }

    private void ShowMain() { _main!.ToggleVisibility(); }
    private void ShowQuickAdd() { _quickAdd!.ShowQuickAdd(); }

    private void HandleIpc(string message)
    {
        switch (message)
        {
            case "quickadd": ShowQuickAdd(); break;
            default: ShowMain(); break;
        }
    }

    private void QuitApp()
    {
        _reminders?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _reminders?.Dispose();
        _hotkeys?.Dispose();
        _tray?.Dispose();
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
