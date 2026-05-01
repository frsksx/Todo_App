# Plan: Current State and Remaining Work

This file tracks current status, pending work, known bugs, deferred items, and notes future agents must preserve. Implemented task checklists were removed during the Tudumo / Due / OmniFocus parity pass.

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
| D - UX refinement (round 2) | Done | Visual polish, refined click semantics, toolbar cleanup, sync placeholder, bug fixes — see notes below for deferred drag/drop items. |

Last verified (2026-05-01, after deferred items):

- `dotnet build Todo-App.sln`
- `dotnet test Todo-App.sln --logger "trx;LogFilePrefix=test-results" --logger "console;verbosity=normal" --results-directory tests/_results`
- Result: 42/42 tests passed.

## Pending Work

No pending deferred items.

## Known Bugs

No known open bugs.

## Deferred and Larger Scope

### Larger SDD scope

- Full reminder-engine expansion beyond the v1 action-command path.
- Recurrence engine.
- MSIX packaging and Windows toast action activation.
- Outlook drag/drop and source-reference integration.
- Backup/restore implementation beyond current stubs.
- Settings implementation beyond current stubs.
- Standalone Due-style countdown timers, if they become a separate surface.
- Multi-language and theme work.

### Test and quality follow-up

- UI smoke tests with a Windows desktop session.
- Performance/load tests for the 20k-task target.
- Visual regression coverage.

## Notes for Future Agents

These describe behavior that has shipped and must be preserved. When a Pending Work item lands, update the matching note instead of leaving both.

- Keep task creation inline in `MainWindow`; do not reintroduce a separate task-input popup.
- When creating a task or headline, immediately focus and select the title text input so typing can begin right away.
- State icon left-click toggles Action ↔ Next; double-click toggles Action ↔ Complete; right-click opens the explicit state menu. Ctrl+Left/Right cycles the selected task through Action → Next → Done.
- Do not delete finished tasks automatically. Completed tasks stay in the list when the active filter includes them, use the Done state with a green checkmark, and render their title text greyed out with strikethrough. A freshly completed task stays visible for 5 minutes regardless of filter.
- Space on a selected task expands or retracts that task when the title text is not being edited. Space on a selected headline toggles focus mode: show only that headline and its subtasks; press Space again to return to the complete page.
- Tasks without a heading appear under an `Inbox` heading rendered in italic. This heading is visible only on pages that currently have at least one task without a heading.
- Do not show task counts after heading titles.
- Show the file menu only when the Alt key itself is pressed (not Alt+Tab or other Alt combos). Hide it on Alt release unless keyboard focus moved into the menu.
- Remove the bottom status bar.
- Keep toolbar items in a single row, located above the page tabs.
- Start and end date fields open a calendar popup so the date can be selected directly.
- Do not show a time when only a date is set and no time was explicitly set.
- In addition to start and due dates, tasks support recurrence and a link field. The link field can point to external context such as a OneNote page.
- Users can set or clear a task's recurrence from the task UI.
- Task note text supports markdown.
- When a task is expanded, clicking the note text edits the notes inline.
- For tasks due within the next 2 weeks, show days remaining instead of the due date (`in Nd`). Due-date text turns red when fewer than 7 days remain (including overdue tasks). Overdue tasks display `Nd overdue`.
- Escape first closes any open text box (title editor, note editor, date picker, page rename, heading editor); only when nothing is open does it minimize the window.
- In the note editor, Alt+Enter inserts the next bullet point (continuing the current line prefix `- ` or `• `).
- Tags render as flat text with no border frame. Selected tags have a light-blue background.
- Heading drags temporarily show headings only; task rows restore after drop or cancel.
- Dragging a task or heading onto a page tab moves it to that page.
- Window placement is saved in `AppSetting` keys.
- `rg` may be unavailable in this workspace; use PowerShell `Select-String` if needed.
