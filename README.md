# Windows Tray Task Manager — v1 prototype

A keyboard-first Windows tray task manager inspired by Due (persistent reminders) and Tudumo (GTD speed). This is the **Phase 1 prototype** from [sdd.md](sdd.md) — enough surface area to use and stress-test the core flows.

## What's in v1

- ✅ Tray icon with state-colored badges (idle / scheduled / due-soon / overdue / paused / error)
- ✅ Global hotkeys (`Ctrl+Alt+Q` quick-add, `Ctrl+Alt+T` show/hide main)
- ✅ Quick-add window
- ✅ Main window with task list grouped by heading, search, filters
- ✅ Task editor (title, heading, state, start/due dates, reminder, auto-snooze, notes)
- ✅ 7 task states (Inbox/Next/Waiting/Scheduled/Someday/Done/Archived) with keys `1`–`7`
- ✅ Reminder engine with auto-snooze and missed-reminder coalescing on startup
- ✅ Tray balloon notifications (single + coalesced summary)
- ✅ SQLite persistence (WAL) at `%LOCALAPPDATA%\WindowsTrayTasks\tasks.db`
- ✅ Single-instance via named mutex + named pipe

## What's deferred from the SDD

- ❌ MSIX packaging (run unpackaged via `dotnet run`)
- ❌ Windows App SDK toast notifications (uses NotifyIcon balloon tip — works unpackaged; toast actions like Done/Snooze require MSIX)
- ❌ Outlook drag/drop, recurrence, backup/restore, DPAPI encryption, change log/HLC
- ❌ Today Review mode, Focus Assist integration, Task Scheduler safety net
- ❌ Per-heading reminder defaults

## Prerequisites

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Run it

```powershell
dotnet run --project src/App/WindowsTrayTasks.csproj
```

The app starts silently into the tray — **no main window appears at first**. Look for the round colored icon in the system tray (you may need to click the chevron to expand the overflow area; consider pinning the icon).

## Try the golden path

1. Press `Ctrl+Alt+Q` — quick-add window pops.
2. Type a title. In the reminder field, type `+1m` and press Enter.
3. The window closes. The tray icon turns blue (scheduled).
4. After ~1 minute, the tray icon turns red, a balloon appears, and the tooltip shows "1 overdue."
5. Press `Ctrl+Alt+T` — main window opens, task is highlighted with `⏰ overdue`.
6. Press `Space` to mark done. The reminder is acknowledged; tray returns to gray.
7. Right-click tray icon → Quit (or `Ctrl+Alt+T` again to hide).

## Keyboard reference (main window)

| Key | Action |
|---|---|
| `Ctrl+F` or `/` | Focus search |
| `Ctrl+N` | New task (opens quick-add) |
| `Ctrl+H` | New heading |
| `Enter` / `F2` / double-click | Edit selected task |
| `Ctrl+D` | Edit selected task, focus reminder |
| `Space` | Toggle done |
| `1`–`7` | Set state (Inbox / Next / Waiting / Scheduled / Someday / Done / Archived) |
| `Delete` | Delete task |
| `Esc` | Hide window (or clear search if search has focus) |

## Reminder time shorthand

In any datetime field:
- `+5m` — 5 minutes from now
- `+2h` — 2 hours from now
- `+1d` — 1 day from now
- `2026-05-01 14:30` — explicit local time
- `2026-05-01` — explicit date (00:00 local)

## Data location

```
%LOCALAPPDATA%\WindowsTrayTasks\tasks.db        (SQLite, WAL mode)
%LOCALAPPDATA%\WindowsTrayTasks\tasks.db-wal
%LOCALAPPDATA%\WindowsTrayTasks\tasks.db-shm
```

Delete the `.db*` files to reset.

## Tray menu

- **Quick Add** — same as `Ctrl+Alt+Q`
- **Open Tasks** — same as `Ctrl+Alt+T`
- **Show Overdue** — opens main window
- **Snooze All (5 min)** — bumps every active reminder by 5 minutes
- **Pause / Resume Reminders** — global pause; tray icon turns purple
- **Quit** — clean shutdown

## Known v1 limitations

- **Tray balloon, not Windows toast.** Done/Snooze buttons inside notifications require MSIX packaging + COM activator (SDD §3.3 / §10.2). Click the tray icon or use the main window to act on a reminder.
- **No recurrence yet.** The data model supports it (SDD §8.5) but the UI is deferred.
- **No backup yet.** Back up the `.db` files manually if you care about the data.
- **Hotkey conflicts** are reported in a balloon at startup; remediation requires editing the source for now (settings UI is post-Phase-1).
- **Hidden tray icon.** Windows hides new tray icons by default in the overflow area. Drag it onto the visible tray for testing convenience.

## Project layout

```
src/App/
  App.xaml(.cs)              startup, single-instance, wiring
  Domain/Models.cs           TaskItem, Heading, Reminder, enums
  Persistence/Database.cs    SQLite repos (hand-rolled)
  Reminders/ReminderEngine.cs
  Tray/TrayIconManager.cs    NotifyIcon + dynamic icon rendering
  Hotkeys/GlobalHotkeyService.cs   Win32 RegisterHotKey
  Shell/SingleInstance.cs    mutex + named pipe
  Views/
    MainWindow.xaml(.cs)
    QuickAddWindow.xaml(.cs)
    TaskEditorWindow.xaml(.cs)
    SimplePromptWindow.xaml(.cs)
```

See [sdd.md](sdd.md) for the full design and the roadmap of what comes next.
