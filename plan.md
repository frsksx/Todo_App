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
| C - Bug-fixes | Done | Hotkey schema, inline editor guard, inline heading/task creation, DB snapshot caching, SDD update, page-tab drops, heading/tag drag hardening. |
| D - UX refinement (round 2) | Done | Visual polish, refined click semantics, toolbar cleanup, sync placeholder, drag/drop fixes, docs and test extension. |
| E - Interaction refinements | Done | Multi-select + DEL+undo, date-button display, bold expanded title, double-click select-all, fixed expand column, Alt+Enter bullets, Enter saves title, Alt+Up/Down moves, empty-page drop indicator, "ago" overdue, Shift+Arrow state cycle, grey Someday/Maybe titles, tags left of due date, Ctrl+Alt+A global entry. |
| F - UX polish round 3 | Done | Shift+Alt file menu + 1-min auto-hide, no duplicate due date in expanded, Esc accepts edits, Priority/Effort fields, Agenda tab (Settings toggle), Ctrl+W hides to tray, Shift+Arrow selection bug fixed, empty-page drop indicator for task drag. |
| G - Plan implementation pass | Done | Agenda overhaul, right-column priority/effort/link editor, linked-title open, row hover, Esc no-op when idle, Alt+Up/Down fix, cursor-at-end editors, expanded selection retention, batched tag loading. |
| H - Reliability, recurrence, and reminders | Done | Agenda tag bar/row tag visibility, expanded-row keyboard selection fix, draft-preserving notes editor, task recurrence, Due-style derived reminders, 7-day completed-task archival, compact expanded inspector, preferred state glyphs. |
| I - Task defaults and expanded layout follow-up | Done | Medium-priority task default, deterministic Up/Down row selection after expansion, horizontal task-settings sub-expansion inside expanded rows, double-click notes editing, post-completion text cleanup, title-bar page tabs via WindowChrome. |
| J - Header/status cleanup and selection reliability | Done | Ctrl+click title link targeting, expanded-row auto-collapse, PreviewKeyDown arrow navigation fix, calmer blue hover, status-bar filter/settings controls, header icon/tab scroll buttons, Alt+Left/Right page navigation. |
| K - Header/search micro-refinements | Done | Conditional tab scroll buttons, stable top-right notes-area gear, Ctrl+F hidden search toggle, search reset on hide/startup, tab-scroll buttons adjacent to the tab viewport. |
| L - Modern clean utility visual refresh | Done | Shared light theme resources, TickTick/Todoist-inspired clean utility shell, calmer rounded rows, refined tabs/status bar, polished expanded task card, and matching utility-window styling. |
| M - Sync groundwork | Done | Local SyncOutbox/SyncCursor tables, Supabase options/device id, sync setting persistence, write-side outbox enqueueing for pages/headings/tasks/tags/task-tags/reminders, reminder tombstones, and infrastructure tests. |

Last verified (2026-05-03, after M sync groundwork):

- `dotnet build Todo-App.sln`
- `dotnet test Todo-App.sln --logger "console;verbosity=minimal"`
- Result: 79/79 tests passed.

## Pending Work

- Prevent the reported item-disappearing behavior once the exact trigger is clarified.
- Complete live Supabase remote sync transport/auth/schema work. The app now stores Supabase URL, publishable key, sync-enabled settings, and a generated device id; local writes are queued in `SyncOutbox`; replication is still blocked on the remote schema, auth/session, and transport pieces in the detailed implementation plan below.

## Modern Visual Refresh Plan

### Product direction

- Keep the current no-sidebar layout. Pages stay in the custom title-bar tab strip; filters, tags, settings, and sync stay in the compact bottom status bar.
- Aim for "utility visible, minimal, clean": retain the app's power-user density and always-available controls, but make them quieter, more coherent, and less grey/toolbar-heavy.
- Use TickTick/Todoist as references, not clones: light canvas, restrained accents, calm rows, clear hierarchy, and compact task metadata.
- Polish the whole app, not only `MainWindow`: include `QuickAddWindow`, `GlobalEntryWindow`, `SettingsDialog`, `SimplePromptWindow`, and `TaskEditorWindow` so the design feels intentional everywhere.
- Take enough time to make the design cohesive. Prefer a slightly slower, well-composed pass over a quick color swap.
- Preserve all existing task behavior, keyboard shortcuts, drag/drop, recurrence/reminders, Agenda, link handling, expanded-row interactions, and tray behavior. This phase should be visual and structural only unless a tiny behavior change is required for styling.
- Start with one polished light theme. Dark mode and theme switching are deferred unless requested later.

### Visual language

- Background: warm off-white app canvas with white or near-white list surfaces.
- Accent: choose one primary accent family for the first pass, preferably a calm blue/teal for TickTick-like utility rather than Todoist red. Use it sparingly for selected rows, active tabs, focused inputs, and primary affordances.
- Text: use a modern native Windows font stack such as `Segoe UI Variable`, falling back to `Segoe UI`. Increase hierarchy through weight and opacity rather than large size jumps.
- Rows: soft separators or spacing instead of heavy borders; rounded hover/selected backgrounds; selected row remains obvious during hover.
- Headings: quieter section headers with semi-bold text, subtle background or pill, and less visual weight than today.
- Tags and metadata: small flat chips or muted inline text. Tags should stay useful but not shout.
- State/priority: compact colored glyphs/dots/pills. Keep current state glyph semantics but tune colors to the new palette.
- Expanded row: should feel like an inline card with a calm notes area, subtle border/radius, and a compact settings rail that remains utility-focused.
- Header tabs: rounded compact pills. Active tab gets soft filled background; inactive tabs stay quiet. Scroll buttons remain visible only when needed.
- Status bar: subtle footer treatment with small grouped controls; settings/filter/sync remain discoverable without looking like an old toolbar.

### Implementation phases

1. Add a shared visual token layer.
   - Add reusable resources for colors, brushes, typography sizes, corner radii, spacing, borders, focus rings, hover backgrounds, selected backgrounds, and disabled/muted text.
   - Prefer a dedicated resource dictionary such as `src/App/Resources/Theme.xaml` if convenient, merged from `App.xaml`. If keeping the first pass local, group resources at the top of `MainWindow.xaml` and plan to extract once stable.
   - Replace repeated inline colors in `MainWindow.xaml` with named resources before doing large style changes.

2. Refresh the shell.
   - Restyle the custom title bar to feel lighter and integrated with the main canvas.
   - Keep window controls visible and reliable.
   - Restyle app icon badge, page tabs, tab hover/active states, and tab scroll buttons.
   - Keep title-bar drag space intact.
   - Keep search hidden by default and visually align it with the new shell when toggled.

3. Refresh task and heading rows.
   - Introduce a modern `TaskRowStyle` with rounded hover/selected backgrounds and no harsh borders.
   - Tune row margins/padding so the app feels cleaner without losing dense utility.
   - Restyle task title, state glyph, tags, reminder text, due text, recurrence, and expand affordance.
   - Ensure selected row, focused row, hovered row, Done/Archived/Someday/Maybe styling, overdue text, and linked-title underlines remain distinguishable.

4. Refresh expanded task layout.
   - Give expanded content a subtle card/surface feel with gentle radius, soft border, and clear inner spacing.
   - Keep the settings gear anchored next to the notes text as specified elsewhere in this plan.
   - Restyle notes markdown, notes editor, text inputs, ComboBoxes, DatePickers, reminder controls, and the horizontal settings rail.
   - Preserve the compact horizontal utility layout and avoid vertical clutter.

5. Refresh bottom status bar.
   - Make settings, tags, filter/perspective dropdown, and sync look like one coherent footer.
   - Restyle tag chips and selected tag state.
   - Keep controls compact and discoverable.
   - Avoid moving controls into a sidebar or large top toolbar.

6. Apply supporting control styles.
   - Add consistent styles for icon buttons, quiet buttons, danger/close hover states, text boxes, ComboBoxes, DatePickers, CheckBoxes, context menus, and tooltips where practical.
   - Deliberately style `QuickAddWindow`, `GlobalEntryWindow`, `SettingsDialog`, `SimplePromptWindow`, and `TaskEditorWindow` so the supporting windows match the main app.
   - Keep styles scoped enough that utility windows are polished without accidentally breaking their compact workflows.

7. Polish and verify.
   - Run `dotnet build Todo-App.sln`.
   - Run `dotnet test Todo-App.sln --logger "console;verbosity=minimal"`.
   - Do a manual UI smoke test if possible: title-bar drag/window controls, tab overflow, search toggle, row selection/hover, expansion/settings rail, Agenda, tag filtering, context menus, quick add/global entry/settings dialogs.
   - Update this plan with the shipped visual decisions and any remaining visual debt.

### Acceptance criteria

- App still has no sidebar.
- App feels lighter, cleaner, and more modern while keeping utility controls visible.
- All app windows share the same visual language, including quick add, global entry, settings, prompt, and task editor surfaces.
- Header tabs, task rows, expanded cards, search, and status bar share one coherent visual system.
- Existing keyboard and mouse behavior remains intact.
- Build and tests pass.
- No new dependency-heavy UI framework is introduced for this pass.

### Deferred visual scope

- Dark mode.
- User-selectable themes.
- Animated transitions beyond very subtle focus/hover polish.
- Major layout restructure.
- Replacing WPF controls with a new UI framework.

## Supabase Sync Implementation Plan

### Current implementation checkpoint

- Settings now captures `sync_enabled`, `supabase_url`, and `supabase_publishable_key` through the polished Settings dialog.
- The bottom status-bar sync button distinguishes unconfigured sync from saved Supabase settings.
- SQLite now has `SyncOutbox` and `SyncCursor` tables. Local page, heading, task, tag, task-tag, and reminder writes enqueue sync records; reminders now use `deleted_at` tombstones for propagation-safe deletes.
- `SupabaseSyncOptions.FromDatabase` reads the local sync settings and creates a stable local `device_id`.
- No remote data replication is active yet. The remaining work is the actual Supabase schema/auth/session, conflict-safe pull/push transport, and live/transport tests described below.

### Current code shape to preserve

- The app is local-first today. `App.xaml.cs` constructs one `Database` instance and passes it into `MainWindow`, `QuickAddWindow`, `GlobalEntryWindow`, `TaskEditorWindow`, and `ReminderEngine`.
- SQLite persistence is centralized in `src/Infrastructure/Persistence/Database.cs`. It creates and migrates `Page`, `Heading`, `TaskItem`, `Tag`, `TaskTag`, `Reminder`, and `AppSetting`, using GUID primary keys and UTC `created_at` / `updated_at` timestamps.
- Deletes are mixed but sync-safe for user content: pages, headings, tasks, and reminders use `deleted_at` soft deletes/tombstones; task/tag links are rebuilt by deleting local `TaskTag` rows and inserting the extracted `@token` tags during `SaveTask`, with outbox tombstones for removed links.
- UI refreshes are read-heavy and direct: `MainWindow.Refresh()` archives old completed tasks, loads pages, tasks, headings, reminders, and tags from `Database`, then rebuilds view models. Reminder ticks also write back through `Database.SaveReminder`.
- Existing tests cover `Database.CreateTemp`, page scoping, tag extraction, reminder uniqueness, task state migrations, archival, recurrence, filters, and move math. Sync should add tests around persistence behavior rather than moving logic into WPF.

### Target behavior

- Keep SQLite as the source used by the UI so the app starts instantly, works offline, and preserves the existing tray/reminder behavior.
- Add Supabase as a remote replica and cross-device sync target, not as a replacement for the local database.
- Use Supabase Auth plus a publishable key in the desktop app. Do not store or ship service-role/secret keys in WPF. Enable Row Level Security on all synced tables so each authenticated user only sees their own rows.
- Support manual sync from the bottom status-bar sync control first, then background sync on startup, after local writes, and on a timer. Realtime subscriptions can be a later optimization after polling/outbox sync is reliable.
- Resolve v1 conflicts with row-level last-write-wins using `updated_at`, while protecting unsynced local rows from being overwritten until their outbox entries are pushed. Deeper per-field conflict UI is deferred.

### Supabase project work

- Create remote tables that mirror local persistence, using snake_case names: `pages`, `headings`, `task_items`, `tags`, `task_tags`, `reminders`, and optionally `app_settings`.
- Add required remote-only columns to each synced entity table: `user_id uuid not null`, `device_id text`, `remote_updated_at timestamptz default now()`, and `deleted_at timestamptz` where missing locally.
- Keep existing GUID ids as primary keys so local and remote records can upsert deterministically.
- Make `task_tags` syncable without ambiguity by using composite key `(task_id, tag_id)` plus `user_id`, `created_at`, and `deleted_at`. Because local code currently rebuilds task tags on every `SaveTask`, v1 can either sync `task_tags` as derived data after each task push or regenerate them remotely from synced task titles.
- Add `deleted_at` semantics for reminders before syncing them. Current hard deletes would otherwise be invisible to other devices.
- Add RLS policies for each table where `user_id = auth.uid()`. Grant the authenticated role only the select/insert/update/delete operations needed by the app.
- Enable Realtime only after basic sync works. If Realtime is used for update/delete payloads, configure replication for the synced tables and use full previous-row data where needed for delete/conflict handling.

### Local SQLite migration

Implemented locally in phase M: `SyncOutbox`, `SyncCursor`, sync settings/device id, and `Reminder.deleted_at` tombstones are now in place.

- Add a `SyncOutbox` table:
  - `id TEXT PRIMARY KEY`
  - `entity_type TEXT NOT NULL`
  - `entity_id TEXT NOT NULL`
  - `operation TEXT NOT NULL` (`upsert` or `delete`)
  - `payload_json TEXT`
  - `created_at TEXT NOT NULL`
  - `attempt_count INTEGER NOT NULL DEFAULT 0`
  - `last_error TEXT`
  - `locked_at TEXT`
- Add a `SyncCursor` table:
  - `key TEXT PRIMARY KEY`
  - `value TEXT`
  - `updated_at TEXT NOT NULL`
- Add a `SyncIdentity` or `AppSetting` keys for `supabase_url`, `supabase_publishable_key`, `supabase_session`, `sync_enabled`, and a generated `device_id`.
- Add local sync metadata where useful: `last_synced_at`, `sync_dirty`, or track dirty state solely through `SyncOutbox`. Prefer the outbox as the authoritative pending-write source to avoid column churn across every domain model.
- Add `Reminder.deleted_at` locally and migrate `DeleteRemindersForTask` from hard delete to soft delete or outbox-backed remote tombstones.

### C# implementation steps

- Add the `supabase` NuGet package to `WindowsTrayTasks.Infrastructure`. Supabase's C# library is community-maintained and expects API models deriving from its base model with table/primary-key/column attributes, so keep those DTOs separate from the domain models.
- Add `Infrastructure/Sync/SupabaseSyncOptions.cs` for URL, publishable key, user/session state, device id, and sync interval.
- Add `Infrastructure/Sync/SupabaseSyncClient.cs` to own Supabase client initialization, auth/session restore, CRUD/upsert calls, and optional Realtime channel wiring.
- Add remote DTOs under `Infrastructure/Sync/Models`, for example `RemotePage`, `RemoteHeading`, `RemoteTaskItem`, `RemoteTag`, `RemoteTaskTag`, and `RemoteReminder`. Map nullable enums and UTC timestamps explicitly.
- Add `Infrastructure/Sync/SyncMapper.cs` to convert between local domain models / database rows and remote DTOs. Keep tag extraction in the existing `TagExtractor` / `Database.SaveTask` path so one feature owns tag semantics.
- Add database methods that expose sync snapshots and apply remote rows without re-enqueuing local outbox entries:
  - `GetSyncChanges()`
  - `EnqueueSyncChange(...)`
  - `MarkSyncChangeComplete(...)`
  - `ApplyRemotePage(...)`
  - `ApplyRemoteHeading(...)`
  - `ApplyRemoteTask(...)`
  - `ApplyRemoteTag(...)`
  - `ApplyRemoteTaskTag(...)`
  - `ApplyRemoteReminder(...)`
- Update `SavePage`, `SaveHeading`, `SaveTask`, `DeletePage`/page save with `DeletedAt`, `DeleteHeading`, `DeleteTask`, `SaveReminder`, and reminder deletion so each user-originated write adds an outbox item in the same SQLite transaction as the data change.
- Add `App/Sync/SyncService.cs` or `Infrastructure/Sync/SyncCoordinator.cs` that runs:
  - Pull remote rows changed after the stored cursor, oldest tables first: pages, headings, tasks, tags, task tags, reminders, settings.
  - Apply remote rows locally when they are newer than local rows and no pending local outbox exists for that row.
  - Push local outbox entries using Supabase upsert/delete calls.
  - Save the new remote cursor and update sync status.
  - Back off and retain outbox rows on network/auth/RLS failures.
- Wire the sync service in `App.xaml.cs` after `Database` construction and before windows are shown. It should be optional when credentials are absent.
- Update `SettingsDialog` to capture Supabase URL/key and sign-in state, plus a clear "sync enabled" setting. Keep status/error text in settings or the bottom sync placeholder, not modal popups during background sync.
- Update the bottom status-bar sync placeholder to show disabled, syncing, pending count, last synced time, and error state. Add a manual sync click target.
- Trigger sync after Quick Add, Global Entry, Task Editor saves, inline saves/reorders, reminder actions, undo restore, archival, and settings changes. Prefer a debounced request so bursts of row moves do not start many remote calls.

### Conflict and ordering rules

- Use UTC `updated_at` as the v1 conflict clock. Every local save already updates this in `Database`; remote upserts should preserve the local value and also set a remote server timestamp for cursor queries.
- Never overwrite a local row that has a pending outbox entry unless the user explicitly runs a future conflict resolution command.
- Preserve soft deletes: a newer `deleted_at` wins over an older active row. Undo should create a normal newer upsert with `deleted_at = null`.
- Preserve ordering by syncing `sort_order` exactly. Reordering bursts should be pushed as row upserts for the affected tasks/headings.
- Preserve page scoping. `page_id` is required for headings, tasks, and tags; remote pulls must apply pages before dependent rows.
- Treat Agenda as a virtual view only. It should not have a remote row.
- Treat local-only UI state carefully. Good v1 candidates for sync are user content and optional app preferences; window placement, active page, and transient expanded/search state should remain local unless explicitly desired later.

### Test plan

- Add infrastructure tests for the local outbox: save/update/delete page, heading, task, reminder, tag reconciliation, and undo restore all enqueue the expected changes.
- Add tests for applying remote rows: newer remote upsert wins, older remote row is ignored, pending local outbox blocks remote overwrite, remote soft delete hides the row, remote undo restores the row.
- Add tests for reminder delete migration so disabling/removing reminders can propagate to another device.
- Add mapper tests for every remote DTO, covering nullable dates, nullable priority/effort, recurrence, archived/completed/deleted states, and task tags.
- Add a fake `ISyncTransport` and unit-test the sync coordinator without live Supabase credentials.
- Keep live Supabase integration tests opt-in via environment variables such as `SUPABASE_URL`, `SUPABASE_KEY`, and a test account/session. They should never run in the default `dotnet test Todo-App.sln` path.
- After implementation, run `dotnet build Todo-App.sln` and `dotnet test Todo-App.sln --logger "console;verbosity=minimal"`.

### Deferred sync scope

- Multi-user/shared pages.
- Field-level conflict resolution UI.
- End-to-end encryption of task content.
- Server-side recurring task generation.
- Supabase Storage attachments.
- Push notifications through Supabase/Edge Functions.

## Known Bugs

- None currently tracked.

## Open Questions

- "Items do not disappe": which items are disappearing, and during what action: filtering/search, switching pages, expanding/collapsing, deleting/undoing, archiving, or dragging?

## Product Decisions

- Completed-task cleanup soft-deletes tasks into a new Archived state after 7 days.
- Recurring task completion creates a fresh next task rather than mutating the completed task.
- Recurrence v1 presets: daily, weekly, biweekly, monthly, quarterly, and yearly.
- Reminder time is derived from due date.
- Reminders should support an opt-in continuous nag mode similar to Due.
- Archived tasks should appear only in an explicit Archived view, not in Show All.
- Archived is not part of keyboard state cycling; it is applied only by automatic completed-task cleanup or manually through the explicit state dropdown/menu.
- Modern visual refresh direction: utility visible, minimal, clean, light theme first, no sidebar, polished across all app windows.
- Supabase sync settings may be stored locally, but the app must not store or ship service-role keys. Only project URL and publishable/anon keys belong in desktop settings.

## Deferred and Larger Scope

### Larger SDD scope

- MSIX packaging and Windows toast action activation.
- Outlook drag/drop and source-reference integration.
- Backup/restore implementation beyond current stubs.
- Settings implementation beyond current stubs.
- Standalone Due-style countdown timers, if they become a separate surface.
- Multi-language and theme work.

### Test and quality follow-up

- UI smoke tests with a Windows desktop session, especially pointer-level drag/drop timing.
- Performance/load tests for the 20k-task target.
- Visual regression coverage.

## Notes for Future Agents

These describe behavior that has shipped and must be preserved. When a Pending Work item lands, update the matching note instead of leaving both.

- Keep task creation inline in `MainWindow`; do not reintroduce a separate task-input popup.
- When creating a task or headline, immediately focus and select the title text input so typing can begin right away.
- State icon left-click toggles Action <-> Next; double-click toggles Action <-> Complete; right-click opens the explicit state menu. **Shift+Right/Left** cycles the selected task forward/backward through non-Archived states only (not Ctrl+Arrow, which is used for word-jump in text fields). Archived is not part of keyboard cycling.
- Completed tasks are automatically moved into the Archived state after 7 days. Archived tasks use `TaskState.Archived` plus `archived_at`, and are visible only in the explicit Archived perspective, not Show All.
- Space on a selected task expands or retracts that task when the title text is not being edited. Space on a selected headline toggles focus mode: show only that headline and its subtasks; press Space again to return to the complete page.
- Tasks without a heading appear under an `Inbox` heading rendered in italic. This heading is visible only on pages that currently have at least one task without a heading.
- Do not show task counts after heading titles.
- Completely remove the File/View bar, its open hotkey, and any menu-only commands outright. Do not preserve the previous **Shift+Alt** menu reveal behavior.
- Keep a compact bottom status bar for settings, tag filters, the combined filter/perspective dropdown, and sync.
- The app uses a shared light visual system in `src/App/Resources/Theme.xaml`: warm off-white canvas, white raised surfaces, calm teal accent, rounded controls, and modern native Windows typography. Preserve the no-sidebar, utility-visible design direction.
- The search bar starts hidden and empty on app launch and page restore. `Ctrl+F` toggles it; showing the bar focuses/selects the search text, and hiding it clears the search text and resets filtering. `/` opens/focuses search. Escape closes the search bar and resets/clears the current search.
- Task state glyphs use the preferred compact set: Action full circle, Next play/forward triangle, On Hold pause, Waiting hourglass, Someday cloud, Done checkmark, Archived `A`.
- Expanded task rows default to a calmer notes/metadata view. A small inline settings toggle opens a horizontal second expansion by revealing the fixed-width right rail with aligned controls for Start, Due, Priority/Effort, Link, Repeat, and reminders. Preserve the dense utility feel and existing selected/hover row styling.
- Start and end date fields open a calendar popup so the date can be selected directly. When a start or due date is set, the corresponding button in the expanded row shows the date value instead of a label; a ✕ button next to it clears the date. The separate date display that used to appear at the bottom of the expanded area has been removed.
- Do not show a time when only a date is set and no time was explicitly set.
- In addition to start and due dates, tasks support recurrence and a link field. The link field can point to external context such as a OneNote page, is editable in the expanded row's right column, and underlines the task title when set. Clicking an underlined title opens the link with the default shell handler.
- Users can set or clear a task's recurrence from the expanded task UI. Supported v1 presets are daily, weekly, biweekly, monthly, quarterly, and yearly. Completing a recurring task creates a fresh next Action task rather than mutating the completed task.
- Task note text supports markdown.
- Single click and double click both open editing for task titles in collapsed and expanded rows. When rendered notes text is visible, single click and double click both open the notes editor. `MainWindow` keeps an in-memory draft for the active notes editor so unrelated refreshes, including reminder ticks, do not replace in-progress text with the last saved value.
- When a task is expanded its title renders **bold**. Opening the inline title editor or notes editor places the cursor at the end of the existing text. Double-clicking inside an already-open title text input still selects all text.
- For tasks due within the next 2 weeks, show days remaining instead of the due date (`in Nd`). Due-date text turns red when fewer than 7 days remain, including overdue tasks. Overdue tasks display `Nd ago`; tasks overdue by less than 1 day display `Today`.
- Escape first closes any open text box: title editor, note editor, date picker, page rename, or heading editor. If search is open or contains text, Esc closes the search bar and resets/clears search. If a task row is expanded, Esc collapses the task row and any inner horizontal expansion. When no editor is open, search is empty, and no task is expanded, Esc does nothing.
- Enter while editing a task title commits the change and exits the editor. In the note editor, Alt+Enter inserts a new bullet point: if the current line has no leading `- ` it adds one first, then moves to a new line with `- `. WPF sends `Key.System` (not `Key.Enter`) when Alt is held; check `e.Key == Key.System && e.SystemKey == Key.Return`.
- Tags render as flat text with no border frame. Selected tags have a light-blue background.
- Tags extracted from a task title (via `@token` regex) are **not shown inside the title string**. They are displayed as flat-text tokens in the task row to the left of the due date, bound through `TagsRowText` / `TagsRowVisibility` on `TaskRowVm`. The visible title uses `DisplayTitle` which strips the `@token` fragments.
- Tasks in the **Someday** or **Maybe** state render their title text greyed out (same color as Done, but no strikethrough). Done still gets strikethrough.
- The **DEL key** deletes all currently selected tasks and/or headlines. Deleting a headline moves its subtasks to headingless (Inbox) rather than deleting them. An undo bar appears at the bottom of the main window for ~5 seconds; pressing Undo restores all deleted items.
- The ListBox uses `SelectionMode="Extended"` so Shift-click and Ctrl-click multi-select work natively. `DEL` collects `TaskList.SelectedItems.OfType<TaskRowVm>()`.
- **Alt+Up / Alt+Down** moves the selected task or headline one position. Moving a task across a heading boundary reassigns it to the new heading. Moving a headline moves all its subtasks with it. At boundaries the move silently does nothing.
- When dragging a headline to a page tab that switches to an empty page, a blue insertion line is shown at the bottom of the empty list so the user knows they can drop there.
- Heading/task rows remain visible during heading drags so vertical drop targets stay stable after page-tab hover switches.
- Dragging a task, heading, or inbox heading onto a page tab moves it to that page.
- Hovering a page tab during task, heading, or inbox drag switches to that page; dropping vertically in the switched page uses the visible list target.
- Dropping a task on a tag or a tag on a task assigns the tag idempotently through `TaskTitleTags`.
- Keep cross-page heading and inbox move state changes in `TaskBoardMoves` so unit tests cover the behavior independently from WPF pointer events.
- **Ctrl+Alt+A** is a global hotkey (active when app is minimized) that opens `GlobalEntryWindow` — a small ToolWindow popup with Title, Notes, Start, and Due fields. On save, the task is created in Action state and placed in the Inbox page (auto-created if missing). The popup closes on Enter or Escape; the main window stays minimized.
- Window placement is saved in `AppSetting` keys.
- New tasks default to `TaskPriority.Medium` through `EntityFactory.CreateTask`, so inline creation, quick add, and global entry share the same default.
- The expanded `done: yyyy-mm-dd` completion chip is intentionally hidden. Done tasks still keep the Done state styling and title strikethrough.
- Linked tasks open their link only with **Ctrl+click** on the title text itself, not from clicking the rest of the task row. Hovering a linked task title should keep the normal mouse cursor instead of switching to a hand/link cursor.
- Page tabs, including the optional Agenda tab, live in a custom WPF `WindowChrome` title-bar strip to save vertical space. The right side uses explicit WPF minimize/maximize/close buttons because native Aero caption buttons did not paint reliably with the custom title bar. Mark title-bar interactive controls with `WindowChrome.IsHitTestVisibleInChrome="True"`.
- Plain Up/Down navigation in the task list is handled explicitly in `TaskList_PreviewKeyDown` through `MoveSelectionBy` so selection remains anchored after expanding/collapsing rows instead of jumping to the first recycled item.
- Expanded task rows auto-collapse when selection moves away from them. The selected task can remain expanded; moving to any other task or heading closes the previous expanded row and its settings rail.
- The expanded-row settings control is a gear-only button anchored next to the notes text. Opening the horizontal settings expansion must not move the gear; when the rail is open, the gear remains above the horizontal expansion rather than beside it.
- Empty headings are always shown; the old `Show Empty Headings` toolbar checkbox has been removed and the menu item is locked on.
- Done visibility is controlled by the combined filter/perspective dropdown; the old `+ Done` toolbar checkbox has been removed.
- The bottom status bar owns the settings gear, tag filters, combined filter/perspective dropdown, and sync placeholder.
- The title bar uses the app icon instead of `Tasks` text. Header tabs scroll horizontally with left/right scroll buttons only; no visible horizontal scrollbar is shown. Hide both tab-scroll buttons when all tabs fit; when they overflow, keep the right scroll button immediately after the tab viewport so remaining title-bar space stays draggable.
- **Alt+Left / Alt+Right** switches pages and includes the optional Agenda tab in the cycle.
- The Settings dialog stores Supabase URL, publishable key, and sync enabled/disabled state. The status-bar sync button is currently a configuration/status surface only; do not imply data has synced until the outbox/transport/auth work lands.
- `SyncOutbox` is the durable local queue for user-originated syncable writes. Do not bypass it for page, heading, task, tag, task-tag, or reminder changes that should eventually replicate.
- `SaveCurrentPageViewState` calls `SavePage(..., enqueueSync: false)` intentionally so transient active-page view state does not create sync outbox rows.
- Reminders support `DeletedAt`; `DeleteRemindersForTask` soft-deletes reminders and enqueues tombstones so another device can receive reminder removal later.
- **Esc while editing a title or notes box commits (accepts) the current text**, same as Enter or clicking away. The outer Esc chain only clears non-empty search; it does not exit heading focus or hide the window when idle.
- **Ctrl+W** hides the main window to the system tray (`HideToTray()`).
- Each task has optional **Priority** (`TaskPriority` enum: Low/Medium/High) and **Effort** (integer hours) fields. Both are nullable. They are stored in `TaskItem.Priority` and `TaskItem.EffortHours`, persisted in `priority` / `effort_hours` columns (added via `EnsureColumn` migration). They appear in the expanded row's right column below the due date row. The ComboBox is populated via `PriorityCombo_Loaded` (not via XAML binding) because WPF tag-to-enum matching doesn't round-trip cleanly.
- The **Agenda tab** is an optional virtual tab (not a real Page entity). It is toggled via Settings → "Show Agenda tab" checkbox (stored in `AppSetting key='show_agenda_tab'`). It is always rendered before user page tabs. When active (`_agendaMode = true`), `Refresh()` branches to `RefreshAgenda()` which queries non-archived tasks from all pages, defaults to Show All, and still honors normal filter shortcuts/search. Agenda groups tasks under headings labelled with their page name, e.g. `Work [Projects]`, and uses `Inbox [Page]` for headingless tasks. Agenda allows dragging tasks and headings, including moves between pages; any move is reflected in the underlying page tabs/source pages. Switching any normal page tab sets `_agendaMode = false`.
- **Shift+Left/Right state cycle** is handled in `OnPreviewKeyDown` (window-level tunneling) so the ListBox's Extended selection never sees the Shift+Arrow and cannot trigger range-select. It was moved out of `TaskList_KeyDown`. Archived must be excluded from this cycle and remain available only through automatic completed-task cleanup or the explicit state dropdown/menu.
- Due date is shown in the title row only; the expanded area's due chip is hidden when a date is set (`DueExpandedChipVisibility`). The "↗ due" prompt still appears in the expanded section when no date is set.
- Agenda's tag bar aggregates tags across all visible agenda tasks.
- Expanded tasks can enable a reminder derived from the task due date. Reminders support acknowledge, 5-minute snooze, and opt-in continuous nagging through `Reminder.AutoSnoozeEnabled`; one-shot reminders fire once and remain visibly overdue until handled.
- If implementing title-bar controls, use WPF `WindowChrome` and mark interactive controls with `WindowChrome.IsHitTestVisibleInChrome="True"` so tabs/buttons/search remain clickable while preserving drag-to-move, resize, snap, and caption button behavior.
- Task and heading rows use the shared modern hover/selected brushes from `Theme.xaml`; the selected-row highlight remains visible while selected rows are hovered.
- `rg` may be unavailable in this workspace; use PowerShell `Select-String` if needed.
