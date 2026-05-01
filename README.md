# Windows Tray Task Manager

A keyboard-first Windows tray task manager inspired by Due's persistent reminders and Tudumo's fast GTD-style capture. The app runs from the tray, stores data in SQLite, and keeps the main workflow dense: quick capture, page tabs, headings, tags, reminders, and keyboard-driven task state changes.

## Current Features

- Tray icon with state-colored badges for idle, scheduled, due-soon, overdue, paused, and error states.
- Global hotkeys: `Ctrl+Alt+Q` for quick add and `Ctrl+Alt+T` for show/hide.
- Quick-add window with reminder shorthand.
- Main task window with pages, headings, inbox rows, search, composed filters, date filters, and tag filters.
- Task editor for title, heading, state, start/due dates, reminders, recurrence, links, and markdown notes.
- Task states: Action, Next, On Hold, Waiting, Someday, and Done.
- Reminder engine with auto-snooze and missed-reminder coalescing on startup.
- Tray balloon notifications.
- SQLite persistence with WAL mode at `%LOCALAPPDATA%\WindowsTrayTasks\tasks.db`.
- Single-instance handling with a named mutex and named pipe.
- Drag/drop reordering for tasks and headings, including moving tasks, headings, and inbox tasks between page tabs.
- Drag/drop tag assignment by dropping a task on a tag or dropping a tag on a task.

## Deferred Scope

- MSIX packaging.
- Windows App SDK toast notifications and toast action buttons.
- Outlook drag/drop and source-reference integration.
- Full recurrence engine.
- Backup/restore implementation beyond current stubs.
- Settings implementation beyond current stubs.
- Focus Assist integration and Task Scheduler safety net.
- Per-heading reminder defaults.

## Prerequisites

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Run

```powershell
dotnet run --project src/App/WindowsTrayTasks.App.csproj
```

The app starts silently in the tray. Open the tray overflow if the icon is hidden, or press `Ctrl+Alt+T` to show the main window.

## Test

```powershell
dotnet build Todo-App.sln
dotnet test Todo-App.sln --logger "console;verbosity=normal"
```

The current unit suite covers parser/filter/reminder behavior, persistence, pages/tags, sort-order math, and the pure board-move/tag helpers used by drag/drop flows. WPF pointer-level drag/drop still needs manual smoke testing in a Windows desktop session.

## Try the Golden Path

1. Press `Ctrl+Alt+Q`.
2. Type a title. In the reminder field, type `+1m` and press Enter.
3. The window closes and the tray icon turns blue.
4. After about 1 minute, the tray icon turns red and a balloon appears.
5. Press `Ctrl+Alt+T` to open the main window.
6. Press `Space` on the task to mark it done.
7. Right-click the tray icon and choose Quit.

## Keyboard Reference

| Key | Action |
|---|---|
| `Ctrl+F` or `/` | Focus search |
| `Ctrl+N` | New task |
| `Ctrl+H` | New heading |
| `Enter`, `F2`, or double-click | Edit selected task |
| `Ctrl+D` | Edit selected task and focus reminder |
| `Space` on task | Toggle done |
| `Space` on heading | Toggle heading focus mode |
| `Ctrl+Left` / `Ctrl+Right` | Cycle selected task state |
| `1`-`6` | Set state |
| `Delete` | Delete task |
| `Esc` | Close active editor/search, then hide window |

## Drag And Drop

- Drag a task within a page to reorder it or move it into a heading/inbox section.
- Drag a heading vertically in the list to position it before or after another heading section.
- Drag a task, heading, or inbox heading onto another page tab to move it there.
- Hover a page tab during a drag to switch pages, then drop vertically in the newly active page.
- Drag a task onto a tag to assign that tag.
- Drag a tag onto a task to assign that tag.

## Reminder Time Shorthand

In any datetime field:

- `+5m`: 5 minutes from now
- `+2h`: 2 hours from now
- `+1d`: 1 day from now
- `2026-05-01 14:30`: explicit local time
- `2026-05-01`: explicit date at local midnight

## Data Location

```text
%LOCALAPPDATA%\WindowsTrayTasks\tasks.db
%LOCALAPPDATA%\WindowsTrayTasks\tasks.db-wal
%LOCALAPPDATA%\WindowsTrayTasks\tasks.db-shm
```

Delete the `.db*` files to reset local data.

## Tray Menu

- Quick Add: same as `Ctrl+Alt+Q`
- Open Tasks: same as `Ctrl+Alt+T`
- Show Overdue: opens the main window
- Snooze All (5 min): bumps every active reminder by 5 minutes
- Pause / Resume Reminders: global pause
- Quit: clean shutdown

## Project Layout

```text
src/App/
  App.xaml(.cs)                    startup, single-instance, wiring
  Hotkeys/GlobalHotkeyService.cs   Win32 RegisterHotKey
  Shell/SingleInstance.cs          mutex and named pipe
  Tray/TrayIconManager.cs          NotifyIcon and dynamic icon rendering
  Views/                           WPF windows

src/Domain/
  Models.cs                        task, heading, page, tag, reminder models
  TaskBoardMoves.cs                pure page/heading/inbox move rules
  TaskTitleTags.cs                 tag-token title updates
  SortOrderMath.cs                 sparse ordering helpers
  ReminderActions.cs               reminder command behavior

src/Infrastructure/
  Persistence/Database.cs          SQLite persistence

tests/
  Domain.Tests/
  Infrastructure.Tests/
  TestSupport/
```

See [sdd.md](sdd.md) and [plan.md](plan.md) for the broader design notes and current roadmap.
