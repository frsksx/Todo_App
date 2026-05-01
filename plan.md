# Plan: Current State and Remaining Work

This file has been cleaned up after the Tudumo, Due, and OmniFocus parity pass. Implemented task checklists were removed so this document only tracks current status, deferred work, and notes that future changes should preserve.

## Current Status

| Phase | Status | Notes |
|---|---|---|
| A0 - Foundations | Done | Build and test projects, diagnostics, deterministic seams, parser/filter/reminder/database coverage. |
| A - Visual and interaction parity | Done | Dense task list, state glyphs, inline editing, row expansion, heading collapse/focus, filters. |
| Ap - Pages | Done | Page entity, tab strip, keyboard switching, per-page view state, quick-add page selection. |
| A2 - Tag system | Done | @-token extraction, page-scoped tags, task/tag links, bottom tag bar, composed filtering. |
| A3 - Filter rework | Done | Composed filter criteria, date filter dropdown, AND-composed predicates. |
| A4 - Reordering | Done | Keyboard moves, sort-order math, drag-and-drop, insertion indicator line. |
| A5 - Filter button and state model | Done | Six action states, filter-view hotkeys, inline date chips, state icon menu. |
| A6 - Due and OmniFocus workflow | Done | Quick presets, visible snooze state, reminder action commands, Inbox, Forecast, Review metadata, built-in perspectives. |
| B - Chrome polish | Done | Menu bar, Settings/Backup/About stubs, standard window sizing, window placement persistence. |
| C - Bug-fixes | Done | Hotkey schema, inline editor guard, inline heading/task creation, DB snapshot caching, SDD update, page-tab drops, heading-drag headings-only view. |

Last verified:

- `dotnet build Todo-App.sln`
- `dotnet test Todo-App.sln --logger "trx;LogFilePrefix=test-results" --logger "console;verbosity=normal" --results-directory tests/_results`
- Result: 42/42 tests passed.

## Remaining Product Work

### Deferred UI Polish

- Custom send-to-tray title-bar button.
- `New` compound dropdown if a toolbar split-button is still desired.
- Find-as-toggle polish; the current always-visible search remains.
- Dedicated Page Manager window for bulk page rename, reorder, and delete.
- Dedicated Tag Manager window for rename, color, merge, and delete.
- Global quick-add shortcut that works while the app is minimized. It opens a title-only capture window where each row creates one task, allowing multiple tasks at once. Enter saves and closes the window; Alt+Enter inserts a new row.

### Larger SDD Scope

- Full reminder-engine expansion beyond the v1 action-command path.
- Recurrence engine.
- MSIX packaging and Windows toast action activation.
- Outlook drag/drop and source-reference integration.
- Backup/restore implementation beyond current stubs.
- Settings implementation beyond current stubs.
- Standalone Due-style countdown timers, if they become a separate surface.
- Multi-language and theme work.

### Test and Quality Follow-up

- UI smoke tests with a Windows desktop session.
- Performance/load tests for the 20k-task target.
- Visual regression coverage.

## Notes for Future Agents

- Keep task creation inline in `MainWindow`; do not reintroduce a separate task-input popup.
- When creating a task or headline, immediately focus and select the title text input so typing can begin right away.
- State icon left-click cycles; right-click opens the explicit state menu.
- Do not delete finished tasks automatically. Completed tasks stay in the list when the active filter includes them, use the Done state with a green checkmark, and render their title text greyed out.
- Space on a selected task expands or retracts that task when the title text is not being edited. Space on a selected headline toggles focus mode: show only that headline and its subtasks, then press Space again to return to the complete page with all headlines and tasks.
- Tasks without a heading appear under a default `No Project` heading. This heading is visible only on pages that currently have at least one task without a heading.
- Do not show task counts after heading titles.
- Show the file menu only while the user is pressing or navigating with Alt.
- Remove the bottom status bar.
- Keep toolbar items in a single row, located above the page tabs.
- Start and end date fields open a calendar popup so the date can be selected directly.
- Do not show a time when only a date is set and no time was explicitly set.
- In addition to start and due dates, tasks support recurrence and a link field. The link field can point to external context such as a OneNote page.
- Users can set or clear a task's recurrence from the task UI.
- Task note text supports markdown.
- When a task is expanded, clicking the note text edits the notes inline.
- For tasks due within the next 2 weeks, show days remaining instead of the due date.
- Heading drags temporarily show headings only; task rows restore after drop or cancel.
- Dragging a task or heading onto a page tab moves it to that page.
- Window placement is saved in `AppSetting` keys.
- `rg` may be unavailable in this workspace; use PowerShell `Select-String` if needed.
