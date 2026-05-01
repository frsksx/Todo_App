# Plan: Move v1 toward Tudumo speed, Due persistence, and OmniFocus workflow

This plan reviews the v1 prototype against three useful reference points:

- **Tudumo** for dense keyboard-first GTD task manipulation.
- **Due** for persistent reminders, quick rescheduling, and low-friction snooze controls.
- **OmniFocus** for capture, review, forecast, projects/headings, tags, and saved views.

The plan is still UI- and workflow-led, but it now includes the minimum persistence and domain changes required to support those interactions safely. It should be read as a bridge from the current prototype toward the SDD, not as a separate product direction.

## 0. Review adjustments

This pass made the following corrections:

- **A0 was stale.** The repo already has `src/Domain`, `src/Infrastructure`, `src/App`, `IClock`, `IIdGenerator`, `DateInputParser`, `TaskFilter`, `ReminderScanner`, and `Database.CreateTemp()`. Phase A0 is now a hardening/test pass, not a fresh project split.
- **Due needs explicit product weight.** The old plan mentioned reminders as out of scope even though Due is one of the product's core inspirations. Full reminder-engine expansion still belongs to the SDD, but quick snooze, preset date controls, notification actions, and visible auto-snooze state belong in this plan.
- **OmniFocus changes the state-model decision.** Tudumo's six action states are useful, but OmniFocus-style Inbox capture should not be lost. The adjusted model separates "capture location" from "action state" so Inbox survives without polluting the state filter.
- **Pages remain useful but should stay humble.** Pages are top-level personal/work separations, not a replacement for OmniFocus-style projects/headings, tags, forecast, or review.

## 1. Screenshot analysis

### 1.1 First screenshot — main window, list, headings, tag bar

What the Tudumo window shows, top to bottom:

- **Title bar**: standard Windows chrome plus a small custom button (top-right, before minimize) that appears to be "send to tray". Window stays out of the way without quitting.
- **Menu bar**: `File` `Edit` `View` `Tools` `Help`. Classic Win32 menus.
- **Toolbar row**: `New ▼` (compound new-task / new-heading dropdown) · `☑ Show Empty Headings` · `Find` (text link) · `+ Done ▼` (state-include toggle) · `Filter By Date ▼`. Single line, dense, no icons.
- **Task list**: tasks grouped under bold headings. Each heading shows a count and a chevron (collapse/expand). Each task row has:
  - A **shape-distinct state icon** on the left (open circle / green check / blue double-bar / blue clock — color *and* silhouette).
  - A title (with `@context` tags inline — the trailing tags are auto-detected).
  - On the right: a small note-attached glyph when notes exist, then a chevron `>` to expand inline notes.
  - Done items show their completion date inline (`done: 29 Apr`).
- **Tag bar at bottom**: `Untagged | @Computer | @online- | extra | testing | All`. Click-to-filter, multi-tag aware.
- **Status bar**: `14 active actions | Unregistered - 39 trial days left`. Short, factual.

Defining interaction patterns:

- **Inline editing**: title is editable by double-click / F2 directly in the row. The screenshot literally instructs `double-click on this text to edit`.
- **Inline note disclosure**: chevron expands the row to reveal/edit notes without leaving the list.
- **State icon click-to-cycle**: `click the icon on the left-hand side` cycles task state in place.
- **Heading focus**: `Select this heading and press <space>` enters a focused view of just that heading. Space is context-sensitive (heading focus vs. task done-toggle).
- **Heading collapse**: chevron next to count collapses the heading.
- **Show Empty Headings**: explicit toggle keeps the outline stable while filtering.
- **`+ Done` button**: dedicated toggle for "include Done items in the active view" — separate from the date filter.

What is **central** to Tudumo and now also **in scope** for this plan:

- **Tags (`@context`).** SDD §5 / §3.2 had removed them, but the screenshot makes clear they are Tudumo's primary cross-heading navigation axis. **Decision: full multi-tag support is in scope (§4).** The SDD will need a follow-up edit to reflect this.

### 1.2 Second screenshot — state filter dropdown, expanded row, inline dates

The second screenshot adds material that the first didn't show. New observations:

- **Filter button reflects current selection.** What the first screenshot labeled `+ Done ▼` is in fact a **state-filter button whose label and icon mirror the active filter** — here it reads `Active Actions ▼` with a red-circle state icon. This is more discoverable than a generic combo: the user always sees what is filtered.
- **State-filter dropdown** with eight entries, each bound to a `Ctrl+N` hotkey:
  1. `Ctrl+1` — Only Next Actions (red open circle)
  2. `Ctrl+2` — Actions + Next Actions (blue circle)
  3. `Ctrl+3` — All except Done (blue pause-bars)
  4. `Ctrl+4` — Show All (green check)
  5. `Ctrl+5` — Only On Hold (blue pause-bars)
  6. `Ctrl+6` — Only Waiting For (purple figure)
  7. `Ctrl+7` — Only Someday/Maybe (gray cloud / swirl)
  8. `Ctrl+8` — Only Completed (green check)
  Items 1–4 form one group (active-work views, including composites), 5–8 form a second group (single-status pivots), separated by a divider.
- **Tudumo's action-state model is six states**, not seven: `Next Action` · `Action` · `On Hold` · `Waiting For` · `Someday/Maybe` · `Completed`. There is no `Scheduled` state (start date carries that), and no `Archived` state (Completed + delete/archive metadata carries that). OmniFocus argues for preserving an Inbox, but as capture location / processing view rather than as an action state. The "Action / Next Action" split is GTD-canonical: every actionable item is an Action, with Next Actions being the small subset chosen as immediately actionable.
- **Composite filter views** (`All except Done`, `Show All`, `Actions + Next Actions`) sit alongside single-state filters in the same dropdown. They are not states; they are named views of the state filter.
- **Expanded task row.** "Edit a note" appears in expanded form — pressing Space on a highlighted task expands the row to show:
  - The full multi-line note inline.
  - **Date chips** underneath the note: start date and due date as clickable, underline-styled chips with a leading checkmark glyph (indicates "set") and a trailing arrow (`→` for start, `↗` for due/reminder).
  - **Inline tag chips** at the right of the date row (here: `softonic`).
  - The right-edge chevron rotates from `>` (collapsed) to `↗` / down (expanded).
- **Space is the expand/collapse toggle** for a highlighted task row. **It does not toggle done.** Done is set by clicking the state icon (or its keyboard equivalent — see Phase A5).
- **Heading rows are very plain** when nothing is collapsed — no chevron rendered. The chevron only appears when the heading is collapsed.
- **Status bar** confirms the wording: `7 active actions`.

### 1.3 Beyond Tudumo — Pages (self-introduced)

Tudumo does not show this in either screenshot, but the project is adopting it as a top-level concept: **Pages** are wholly separate workspaces within the same app, e.g. *Work* and *Private*. Each page owns its own headings, tasks, and tags, and remembers its own filter / focus / search state. Reminders fire across pages (the tray icon reflects overdue items globally), but the main window only ever shows one page at a time.

Use case framing: a single Windows user wants their personal todos and their job's todos in the same app without those lists ever bleeding into each other's filters or tag bars. A user-facing concept, not a sync boundary — multiple pages still live in one local SQLite file under one Windows account.

### 1.4 Due and OmniFocus inspiration guardrails

Due adds three important behaviors that should be visible in the app, not buried in implementation:

- **Persistent reminders.** A reminder keeps resurfacing until the user completes, reschedules, snoozes, or disables it. The row, tray tooltip, and notification should all make that state obvious.
- **Quick time changes.** Due's strength is the speed of setting and postponing reminders. This app should expose a small preset grid (`+10m`, `+1h`, `tomorrow 9`, `next workday`, custom) anywhere a reminder date is edited.
- **Notification actions.** Done, Snooze, and Open are first-class actions. The current balloon path is acceptable for v1, but the UX model should be designed so Windows toast actions can replace it later without changing the domain model.

OmniFocus adds workflow concepts that Tudumo alone does not cover:

- **Inbox as capture, not state.** Fast capture should default to an Inbox/capture bucket until the user assigns a heading/project. This should survive the Tudumo six-state alignment.
- **Forecast.** A dedicated date/reminder view should gather overdue, today, upcoming, and scheduled-notification items without forcing the user to reconfigure filters.
- **Review.** Headings/projects need a periodic review loop so Someday/Waiting/On Hold work does not disappear forever.
- **Perspectives.** Saved filter presets are the scalable version of Tudumo's state dropdown once tags, pages, dates, search, and review status all compose.

## 2. Where v1 falls short

| Capability | v1 today | Tudumo |
|---|---|---|
| State indicator | colored square | shape-distinct icon (circle / check / bars / clock) |
| Edit task title | opens TaskEditorWindow dialog | inline (F2 / double-click) |
| Edit notes | opens TaskEditorWindow | inline chevron expansion |
| Cycle state from row | number keys only | click left icon to cycle, plus number keys |
| Heading collapse/expand | no | per-heading chevron + count |
| Heading focus mode | no | Space on selected heading |
| Show empty headings toggle | n/a (always shown) | explicit checkbox |
| Done-include toggle | hidden in filter combo | dedicated `+ Done` toggle |
| Date filter | a generic combo entry | dedicated `Filter By Date` |
| Menu bar | none | File / Edit / View / Tools / Help |
| Send-to-tray button in title bar | no | small icon top-right |
| Tag system | none | multi-tag, auto-extracted from `@word` in titles |
| Bottom tag bar | none | always-visible, click-to-filter |
| Tag-aware filtering across headings | none | yes |
| Reorder tasks within a heading | none | keyboard + drag-and-drop |
| Move tasks between headings | none | keyboard + drag-and-drop |
| Multiple workspaces (Pages) | none | Work / Private / etc. with isolated headings, tasks, tags |
| State filter button | generic combo | stateful button with current-filter icon |
| Composite filter views | none | "All except Done", "Show All", "Actions + Next Actions" |
| Filter hotkeys | none | `Ctrl+1`–`Ctrl+8` for filter views |
| Expand task row to show notes/dates inline | none | `Space` toggles expanded row |
| Inline date chips with type indicator | dialog only | clickable inline (start `→`, due `↗`) |
| State model | 7 states (Inbox/Next/Waiting/Scheduled/Someday/Done/Archived) | 6 action states plus Inbox as capture view |
| Status text | "N tasks · M overdue" | "N active actions" wording |
| Row density | medium | high |

Additional gaps from Due and OmniFocus:

| Capability | v1 today | Direction |
|---|---|---|
| Quick reminder presets | typed shorthand only | Due-style preset buttons and one-keystroke snooze choices |
| Reminder persistence visibility | engine exists, row affordance is light | visible auto-snooze / overdue / next-fire metadata in rows and notifications |
| Notification actions | tray balloon only | design for Done / Snooze / Open actions; toast implementation can land later |
| Inbox processing | `Inbox` is a state | OmniFocus-style capture bucket or unprocessed view, separate from action state |
| Forecast view | generic date filters | dedicated overdue / today / upcoming / scheduled-reminder perspective |
| Review loop | Today Review exists in SDD, not in this plan | project/heading review metadata and review queue |
| Saved perspectives | none | named composed filters over state, page, tags, date, search, and review status |

## 3. Phased delivery

The phases are independent enough to land in separate sessions and ship one at a time. Each task lists the file(s) most affected.

### Phase A0 — Foundations for testability

Lands first, but this is now a **finish and verify** phase. The repo already has the project split and several pure-domain seams. Phase A0 makes that work complete, buildable from the solution, and test-covered before UI changes start.

1. **Fix solution/project wiring.** `Todo-App.sln` still points at `src/App/WindowsTrayTasks.csproj`, but the app project is `src/App/WindowsTrayTasks.App.csproj`. Update the solution to include:
   - `src/Domain/WindowsTrayTasks.Domain.csproj`
   - `src/Infrastructure/WindowsTrayTasks.Infrastructure.csproj`
   - `src/App/WindowsTrayTasks.App.csproj`
   - `tests/Domain.Tests/WindowsTrayTasks.Domain.Tests.csproj`
   - `tests/Infrastructure.Tests/WindowsTrayTasks.Infrastructure.Tests.csproj`
   - optional `tests/TestSupport/WindowsTrayTasks.TestSupport.csproj`
2. **Complete the time seam.** `IClock` and `SystemClock` exist, and `Database` already accepts a clock. Remove remaining `DateTime.UtcNow` defaults from domain models by moving default timestamps into creation factories / repositories / view-model constructors. UI-only formatting can still use local current time.
3. **Complete the id seam.** `IIdGenerator` and `SystemIdGenerator` exist, but domain model initializers still call `Guid.NewGuid()`. Move ID creation into creation paths that can accept `IIdGenerator`; tests use `SequentialIdGenerator`.
4. **Confirm reminder split.** `Domain/Reminders/ReminderScanner.cs` exists. Add tests around its current behavior before changing it. Then decide whether `Scan` should mutate reminders in place or return immutable update records; immutable updates are easier to reason about once page aggregation and notification actions arrive.
5. **Confirm filter extraction.** `Domain/TaskFilter.cs` exists with the v1 string-mode filter. Add tests for the current modes, then evolve the type in A3 into a composed `TaskFilter` record with state view, date bucket, search, page, tags, focus, and show-empty-headings.
6. **Confirm date parser extraction.** `Domain/DateInputParser.cs` exists. Add tests for `+5m` / `+2h` / `+1d` / explicit / invalid / empty / null. Defer natural-language parsing despite Due's support; this product keeps structured shorthand and preset buttons for now.
7. **Harden `Database.CreateTemp()`.** The helper exists. Add integration tests proving temp DB creation, WAL behavior, round-trips, and cleanup of `.db`, `.db-wal`, and `.db-shm` files.
8. **Add test projects and diagnostics.** Add xUnit projects, `tests/run-tests.ps1`, `tests/run-tests.sh`, `tests/run-tests.cmd`, and `DIAGNOSTICS.md` around `dotnet test --logger "trx;LogFilePrefix=test-results" --logger "console;verbosity=normal" --results-directory tests/_results`.
9. **Baseline build contract.** `dotnet build Todo-App.sln` and `dotnet test` must pass from the repo root before Phase A starts. This is the gate for later, riskier migrations.

### Phase A — Visual + interaction parity (high value, low risk)

1. **Shape-distinct state icons.** Replace the colored square in [`MainWindow.xaml`](src/App/Views/MainWindow.xaml) with a small icon control whose visual changes per state: open circle (Inbox/Next), green check (Done), blue clock (Scheduled), amber pause-bars (Waiting), gray dot (Someday), gray box-out (Archived). Render as path geometry or unicode glyph. Update [`TaskRowVm`](src/App/Views/MainWindow.xaml.cs) to expose `StateGlyph` plus `StateBrush`.
2. **Tighter row density.** Reduce `Padding` in `TaskRowStyle` from `8,5` to `6,3`. Drop default font size to 12.5 in the row template. Keep heading group header padding the same.
3. **Inline title editing.** In [`MainWindow.xaml.cs`](src/App/Views/MainWindow.xaml.cs), bind `F2` and `MouseDoubleClick` to swap the row's `TextBlock` for a `TextBox` that commits on `Enter`/`LostFocus` and reverts on `Escape`. Move the heavy `TaskEditorWindow` to `Ctrl+Enter` (notes-and-details path) and remove the `MouseDoubleClick → editor` binding.
4. **Inline row expansion.** Add a chevron control on the right of each task row. **`Space` on a highlighted task row** toggles expansion (mirrors Tudumo); a click on the chevron does the same. Expanded content lives in Phase A5 — for Phase A only the toggle plumbing and the chevron rotation land here. The current `Space → toggle done` binding in [`MainWindow.xaml.cs`](src/App/Views/MainWindow.xaml.cs) is **removed**; "done" is set by clicking the state icon (A.5) or by the state-cycle hotkey added in A5.
5. **State icon click-to-cycle.** Make the left state icon a clickable button. Click cycles through actionable states (the cycle order is finalized in Phase A5 once the state model is locked).
6. **Heading collapse with count.** Render heading group headers (already in `MainWindow.xaml`) with: title (bold), count of items in the heading, and a chevron. Click toggles `Heading.Collapsed` (already on the model — see [`Models.cs`](src/Domain/Models.cs)) and re-runs `Refresh`. Persist to DB.
7. **Heading focus mode.** Track a `_focusedHeadingId` in `MainWindow`. When a heading row is selected and `Space` is pressed, set/clear the focus. While focused, show only that heading's tasks; status bar shows "Focused: <heading> · Esc to exit". Bind `Esc` to clear focus when active. The Space dispatch is now: heading-selected → toggle focus; task-selected → toggle row expansion (A.4). The two never overlap because exactly one is selected at a time.
8. **`Show Empty Headings` toggle.** Add a `CheckBox` to the top toolbar in [`MainWindow.xaml`](src/App/Views/MainWindow.xaml). When unchecked (default), `Refresh` filters out headings whose visible-task count is 0.
9. **`+ Done` toggle.** Replace the `Done` and `Archived` entries in `FilterCombo` with a dedicated checkbox-button on the toolbar that toggles whether Done items appear alongside the current filter. Archived stays in the combo.
10. **Status bar wording.** Change `"{n} tasks"` to `"{n} active actions"` to match Tudumo. Keep the overdue and pause segments.

### Phase Ap — Pages

Lands **before Phase A2** so the tag schema can include `page_id` from the start. Pages are top-level workspaces: each owns its headings, tasks, tags, and view state. Reminders are page-agnostic — they fire across all pages and the tray reflects global overdue.

1. **Domain entity** in [`Models.cs`](src/Domain/Models.cs):
   - `Page`: `Id` (UUID), `Name` (string, case-insensitive unique among non-deleted), `SortOrder` (double), `LastFilterView` (enum, set by A5.3), `LastFocusedHeadingId` (Guid?), `LastSearchText` (string?), `IsDefault` (bool — true for the auto-created page; the default page cannot be deleted), `CreatedAt` / `UpdatedAt` / `DeletedAt`.
2. **Schema** in [`Database.cs`](src/Infrastructure/Persistence/Database.cs):
   - New `Page` table with `id` PK, `name`, `sort_order`, `is_default`, view-state columns, timestamps. Unique index on `LOWER(name)` where `deleted_at IS NULL`.
   - Add `page_id TEXT NOT NULL` to `Heading` and `TaskItem`. Foreign keys to `Page(id)` with `ON DELETE RESTRICT` (deletion of a non-empty page is blocked at the DB level — UI deletes children first or refuses).
   - Define the tag schema contract ahead of A2: when `Tag` is created, it includes `page_id` from day one. Tags are page-scoped (`@home` on Work and `@home` on Private are independent rows). Unique constraint on `(page_id, LOWER(name))` where `deleted_at IS NULL`.
   - Migration: on first run after upgrade, create one `Page` named `"Tasks"` with `is_default = 1`, then backfill all existing `Heading` / `TaskItem` rows with that page's id. Subsequent runs are no-ops.
   - Index `(page_id, sort_order)` on Heading and TaskItem for fast page-scoped reads.
3. **Active page state.** `MainWindow` tracks `_activePageId`. Persisted in `AppSetting` (key `active_page_id`) so the last-used page survives restarts.
4. **Page tab strip** in [`MainWindow.xaml`](src/App/Views/MainWindow.xaml):
   - Compact horizontal tab row docked above the toolbar (or just below the menu bar once Phase B lands). One tab per non-deleted Page in `sort_order`. Active tab is bold + accent underline.
   - Right edge: a small `+` button to add a page. Clicking opens an inline rename editor for the new tab.
   - Right-click on a tab: `Rename` · `Move Left` · `Move Right` · `Delete` (disabled for the default page or any page with content; choose a fallback target if delete is allowed).
5. **Page management dialog.** `Tools → Pages…` opens a small `PageManagerWindow` to bulk-rename, reorder via drag, set the default page, and merge two pages (move all `Heading` / `TaskItem` / `Tag` rows from `A` to `B`, then soft-delete `A`). Merging is the supported alternative to deletion when a non-empty page must go away.
6. **Keyboard.**
   - `Ctrl+Tab` / `Ctrl+Shift+Tab`: cycle pages forward / backward.
   - `Ctrl+Alt+1`–`Ctrl+Alt+9`: jump to the Nth page (intra-app, distinct from the global `Ctrl+Alt+Q` / `Ctrl+Alt+T` / `Ctrl+Alt+R`). No clash with `Ctrl+1`–`Ctrl+8` filter hotkeys (A5.4).
   - These are configurable per the SDD hotkey conflict-detection conventions.
7. **Filter scope.** `TaskFilter.Apply` (Phase A0.5) gains a `pageId` predicate. All list reads are page-scoped. The bottom tag bar (A2.5) shows only tags within the active page.
8. **Quick-add page selector.** [`QuickAddWindow`](src/App/Views/QuickAddWindow.xaml) gets a small `Page` dropdown defaulting to `_activePageId` and remembered per-session. The global hotkey always opens quick-add bound to the current active page; a user override is one keystroke away (`Tab` to the page combo). New tasks always belong to a page — there is no "no page" state.
9. **Cross-page drag-and-drop.** Extends Phase A4: while a task is being dragged, hovering the pointer over a different tab for ~600 ms switches the active page (auto-expand of the target page list); dropping completes the move and updates `page_id` plus `heading_id` atomically. `Esc` aborts cleanly without page-state mutation.
10. **Per-page view state.** When the user switches pages, persist the leaving page's filter view, focused heading, and search text into its `Page` row. When entering a page, restore them. This prevents Work-mode filters from bleeding into Private-mode browsing.
11. **Reminder engine remains page-agnostic.** `ReminderScanner.Scan` (Phase A0.4) does not read `page_id`. Tray tooltip aggregates across all pages: `"3 overdue · 2 in Work, 1 in Private"`. The toast / balloon includes the page name in the title for context (`"Reminder · Work"`).
12. **Settings / backup.** Backup snapshots the `Page` table with everything else; restore is whole-DB, not per-page. Per-page export is deferred (call out as a future enhancement, not blocking).

Tests for this phase (added per §7.4):

- `Database_FreshInit_CreatesDefaultPage`.
- `Database_BackfillMigration_AssignsAllExistingRowsToDefaultPage`.
- `Database_DeletePageWithTasks_IsRefused`.
- `Database_MergePages_MovesAllChildrenAndSoftDeletesSource`.
- `TaskFilter_ScopedByPage_ExcludesOtherPages`.
- `Database_TagsArePageScoped_HomeOnWorkAndHomeOnPrivate_AreSeparateRows`.
- `ReminderScanner_FiresAcrossPages_AggregatesByPageInResult` (the scan result groups overdue counts by page so the tray adapter can format the tooltip).
- `Database_RenameToCollidingName_OffersMerge`.
- `Database_PageSortOrder_RoundTripsThroughReorder`.

Implementation review after Ap: the core page model, default-page migration, page-scoped reads/writes, active-page persistence, tab strip, tab context menu, keyboard switching, and quick-add page selector landed here. The full `PageManagerWindow` / merge workflow moves to Phase B because it depends on the menu/dialog polish, and cross-page drag remains part of Phase A4 where the drag/drop infrastructure is introduced.

### Phase A2 — Tag system

This phase introduces multi-tag support and the bottom tag bar. It must land before the filter rework in Phase A3 so the date and tag filters can compose.

1. **Schema additions** in [`Database.cs`](src/Infrastructure/Persistence/Database.cs):
   - New `Tag` table: `id` (UUID), `page_id` (UUID — added by Phase Ap), `name` (TEXT NOT NULL — normalized, lowercase), `display_name` (TEXT — preserves original casing), `sort_order` (REAL), `color` (TEXT, nullable, hex), `created_at`, `updated_at`, `deleted_at`. `UNIQUE(page_id, name)` where `deleted_at IS NULL`. Tags are **page-scoped**: `@home` on Work and `@home` on Private are independent rows.
   - New `TaskTag` join: `task_id` (UUID), `tag_id` (UUID), `created_at`. `PRIMARY KEY(task_id, tag_id)`. `ON DELETE CASCADE` from both sides. The page-scoping invariant is enforced at insert time: `task.page_id` must equal `tag.page_id` (a `CHECK` constraint or an application-layer guard — the latter is simpler in SQLite).
   - Migration runner: bump schema version, add tables, backfill existing tasks by parsing `@tokens` from titles within each page (one-off on first run after upgrade).
2. **Domain model** in [`Models.cs`](src/Domain/Models.cs):
   - `Tag` entity with `Id`, `Name`, `DisplayName`, `SortOrder`, `Color`, timestamps.
   - `TaskItem` gets a `Tags` collection (loaded lazily or via a `JOIN` projection).
3. **Tag extraction** in a new `Domain/TagExtractor.cs`: pure function `ExtractTags(string title) → IEnumerable<string>` that finds `@token` patterns. Token rules:
   - Starts with `@`, followed by 1+ word characters; allows `-`, `_`, `.` mid-token; trailing punctuation excluded.
   - Case-preserved in returned tokens; matching is case-insensitive against `Tag.name`.
   - Returns an empty enumerable for `null`/whitespace input.
4. **Tag synchronization** at task save: after `SaveTask`, parse the title, upsert any new tags into `Tag`, and reconcile the `TaskTag` rows so the link set matches the parsed tokens. Implemented as a single transaction in `Database.SaveTask` to avoid partial states.
5. **Bottom tag bar UI** in [`MainWindow.xaml`](src/App/Views/MainWindow.xaml):
   - Horizontal flow panel docked to the bottom (above the status bar).
   - Pseudo-items first: `Untagged` and `All`. Then real tags in alphabetical order with task counts (`@computer (3)`).
   - Click a tag → toggles it in the active filter set. Multi-select (additive) by default; `Ctrl+click` to make it exclusive; clicking `All` clears.
   - Selected tags are visually highlighted; the count next to a tag reflects how many tasks would match if you added that tag to the current filter.
6. **Filter composition.** Update `MainWindow.Refresh` to AND together: state filter, date filter (Phase A3), search, **tag filter**. `Untagged` matches tasks with zero tags. `All` is the empty filter.
7. **Tag management.** A `Tools → Tags…` menu item (Phase B menu bar) opens a small `TagManagerWindow` for rename, color, merge (move all `TaskTag` rows from `A` to `B`, then soft-delete `A`), and delete. Rename normalizes the name and warns if it would collide with an existing tag (offers merge instead).
8. **Inline editing must reconcile tags.** When the title is edited inline (Phase A.3), tags are re-extracted and the `TaskTag` set is updated. A typo-fix that drops `@home` removes that link.

Implementation review after A2: automatic extraction, page-scoped `Tag` / `TaskTag` persistence, task tag projections, bottom tag bar filtering, and inline-title reconciliation landed here. The full `Tools → Tags…` management dialog moves to Phase B with the rest of the menu/dialog polish.

### Phase A3 — Filter rework

Lands after A2 so the filter pipeline can include tags as a first-class dimension.

1. **`Filter By Date` dropdown.** Replace any remaining date-flavored entries in `FilterCombo` with a dedicated date dropdown: `All dates · Today · Tomorrow · This week · No date`. Composes with state, search, and tag filters via AND.
2. **Filter coordination.** Centralize filter state in a small `TaskFilter` record (state, includeDone, dateBucket, search, tagSet, focusedHeadingId, showEmptyHeadings). All UI controls write to it; `Refresh` reads from it. Removes the current scattered `_filterMode` / `_searchText` fields.

Implementation review after A3: state and date controls are now split, and page, focus, state, date, search, tag, and untagged predicates are routed through the pure Domain `TaskFilter` composed criteria.

### Phase A4 — Task reordering (keyboard + drag-and-drop)

Independent of A2 / A3 — only depends on Phase A row infrastructure. SDD §8.1 calls out "Support task reordering within a heading" and "Support moving tasks between headings"; SDD §8.7 reserves `Ctrl+Shift+Arrow Up/Down` for move. This phase delivers both the keyboard and the drag-and-drop paths.

1. **Sort order strategy.** `TaskItem.sort_order` is already `REAL` ([`Models.cs`](src/Domain/Models.cs)). Adopt fractional reordering: when inserting between siblings with sort `a` and `b`, write `(a + b) / 2`. When the gap shrinks below a small epsilon, run a per-heading renumber (1.0, 2.0, 3.0, …) in a single transaction. This keeps reorders to a single row write in the common case.
2. **Keyboard reordering.** Bind in [`MainWindow.xaml.cs`](src/App/Views/MainWindow.xaml.cs) `OnPreviewKeyDown`:
   - `Ctrl+Shift+Up` / `Ctrl+Shift+Down`: move the selected task one slot within its current heading; selection follows the row.
   - `Ctrl+Shift+Left` / `Ctrl+Shift+Right`: move the selected task to the previous / next heading (top of that heading by default), preserving selection.
   - When a *heading* is selected (Phase A.6/A.7), `Ctrl+Shift+Up/Down` reorders the heading itself in the heading list (uses `Heading.SortOrder`).
   - All movements call `_db.SaveTask` (or `SaveHeading`) and re-run `Refresh`. Movements are no-ops at boundaries (top of first heading, etc.).
3. **Drag-and-drop within the list.** Standard WPF drag-source on the task row:
   - Initiate on left-mouse-drag past a 4 px threshold from a task row (avoid conflict with single-click selection).
   - `DataObject` payload: `TaskRowVm.TaskId` (Guid). Effects: `Move` only (no copy semantics in v1).
   - Drop targets:
     - **Between two task rows** (insertion line): inserts at the midpoint sort-order between neighbors; if neighbors are in different headings, the dropped task adopts the lower row's heading.
     - **On a heading header**: appends to the end of that heading (or start, if `Shift` held — keep parity with file managers).
     - **On a collapsed heading**: same as header drop — moves into that heading and leaves the heading collapsed.
   - **Visual feedback**: a 2 px horizontal insertion bar drawn between rows during drag; the heading header lights up when it is the active target. Use the `DragOver` event to compute the target and update the adorner.
   - **Auto-scroll** when the drag pointer is within 24 px of the top/bottom of the list viewport.
   - **Auto-expand collapsed headings** after a short hover (~600 ms) so the user can drop deeper into a heading without first expanding it.
   - **Cancel**: `Esc` during drag aborts the drop.
4. **Multi-select drag.** ListBox already supports `Shift+Click` / `Ctrl+Click` multi-select. Allow dragging the whole selection as one move; sort-order assignment preserves their original relative order.
5. **Drag from outside the app** is a separate concern (Outlook drag/drop, SDD §8.10) and is **not** delivered by this phase. The drop handler must distinguish internal drags (our own `DataObject` format) from external ones and decline external drops in v1 — they will be wired up in the LinkProvider work.
6. **Drop on the bottom tag bar (A2)** is intentionally **not** a drop target. Tag membership is derived from title parsing; dragging a task onto a tag would be a hidden side effect on the title. Out of scope.
7. **Persistence and atomicity.** A single move is one row write. A renumber is wrapped in a transaction over `Database.Open`. No change log entries needed until the broader change log work in SDD Phase 5.
8. **Tests**: unit tests for the midpoint calculator and the renumber trigger; integration test that simulated drag/drop produces the same end state as keyboard moves.

Implementation review after A4: keyboard reordering for tasks/headings and simple internal drag/drop moves landed, backed by `SortOrderMath` tests. The visual insertion adorner, multi-select drag polish, auto-scroll, and cross-page hover-switch behavior remain refinements after the main reorder mechanics.

### Phase A5 — Filter button, expanded row content, state-model alignment

This phase implements the second-screenshot material: the stateful filter button, composite filter views, the `Ctrl+1`–`Ctrl+8` hotkey scheme, the expanded row body, inline date chips, and the alignment of our state model with Tudumo's. It depends on Phase A (row template) and Phase A2 (tags surface inline in the expanded row).

1. **Stateful state-filter button.** Replace the `FilterCombo` state entries with a custom toolbar button whose face shows the **icon and label of the currently active filter** (`Active Actions`, `Show All`, `Only Waiting For`, …). Clicking opens a popup styled like the Tudumo dropdown: 8 entries, two visual groups, each with its state glyph on the left and its `Ctrl+N` hotkey on the right.
2. **State-model alignment** ([`Models.cs`](src/Domain/Models.cs), [`Database.cs`](src/Infrastructure/Persistence/Database.cs) migration). Adopt Tudumo's six-state action model while preserving OmniFocus-style capture:
   - `Action` (ordinary actionable item)
   - `Next` (subset of Action — chosen as the immediately actionable next step)
   - `OnHold`
   - `Waiting` (renamed in UI to "Waiting For" for parity)
   - `Someday`
   - `Done` (renamed in UI to "Completed" for parity)
   `Inbox` is no longer a long-lived action state. It becomes a per-page capture bucket / unprocessed view: quick-add tasks with no heading land there with state `Action`, and processing the inbox means assigning a heading/project, tags, dates, and a more precise state. `Scheduled` is dropped because start date metadata carries the same meaning. `Archived` is dropped as a filterable state and becomes `ArchivedAt` / soft-hidden metadata on a completed or deleted row. Migration: existing `Inbox` rows become `Action` and are assigned to the page's Inbox capture heading if they have no heading; `Scheduled` rows become `Next` with the start date preserved; `Archived` rows become `Done` with `ArchivedAt` retained for audit. State enum integers are renumbered with care; the migration runner backfills old values.
3. **Composite filter views.** The state filter is a **filter view**, not a single state. Define eight named views matching the dropdown:
   - `OnlyNext`, `ActionsAndNext`, `AllExceptDone`, `ShowAll`, `OnlyOnHold`, `OnlyWaiting`, `OnlySomeday`, `OnlyCompleted`. Each view is a predicate over `TaskState`. Stored as the user's chosen filter; default is `ActionsAndNext` (matches Tudumo on launch).
4. **Filter hotkeys.** Bind `Ctrl+1`–`Ctrl+8` in `OnPreviewKeyDown` to set the corresponding filter view. This **supersedes** the SDD §8.7 "Ctrl+1–6: filter by state" reservation — see Phase C.5 SDD update. **Direct state-set hotkeys are kept**: bare number keys `1`–`6` set the selected task's state (`1`=Action, `2`=Next, `3`=OnHold, `4`=Waiting, `5`=Someday, `6`=Done). Number keys are routed to state-set only when a task row has focus and no inline editor is open; the search box and inline editors swallow them as ordinary input.
5. **Expanded row body.** When a task row is expanded (Phase A.4), render below the title:
   - The full **note** as wrapped multi-line text. Editable inline by `Ctrl+Enter`; `Enter` exits edit mode.
   - A row of **date chips** (see A5.6).
   - A row of **inline tag chips** (one chip per linked tag; click a chip to filter by it; chips are read-only here — tag membership is edited via the title, per Phase A2.4).
   - When the row is collapsed, the chevron points right; when expanded, it points down. Animate with a 120 ms ease.
6. **Inline date chips.** Each task row has up to two date chips in its expanded body:
   - **Start chip** with a leading checkmark (when set) and a trailing `→` arrow.
   - **Due/Reminder chip** with a leading checkmark (when set) and a trailing `↗` arrow.
   Chips render the local-formatted date+time as an underlined link. Clicking opens an inline date picker popup; `Esc` cancels, `Enter` commits. The checkmark click clears the date. Done items show their completion date as a small `done: <date>` label inline (matching the first screenshot's "done: 29 Apr"), in addition to the start/due chips.
7. **Heading rendering** (refines Phase A.6). Drop the chevron from heading rows when the heading is expanded; show it only when `Heading.Collapsed = true`. Keep the per-heading task count visible in both states. Heading row is a single bold line, no extra padding.
8. **State-icon glyph table** (refines Phase A.1). Now that the state set is six, lock the glyphs:
   - `Action` — blue open circle
   - `Next` — red open circle (thicker stroke)
   - `OnHold` — blue pause-bars
   - `Waiting` — purple person/figure glyph (or stylized `…`)
   - `Someday` — gray cloud / soft swirl
   - `Done` — green check
   The cycle order for the click-to-cycle action (A.5) is `Action → Next → OnHold → Waiting → Someday → Done → Action`.

### Phase A6 — Due + OmniFocus workflow polish

This phase balances the original Tudumo-centered plan with Due and OmniFocus. It brings the reminder and review workflows up to the same level as the list interactions without requiring the full SDD reminder engine, recurrence engine, or MSIX toast-action work to land first.

1. **Due-style quick time presets.** Add a shared preset model for reminder/date editing: `+10m`, `+1h`, `this evening`, `tomorrow 9`, `next workday`, and custom. Use it in `QuickAddWindow`, expanded row date chips, and snooze popups. Presets are keyboard reachable and user-editable later, but hard-code a small useful set for v1.
2. **Visible auto-snooze state.** In task rows and expanded bodies, show whether a task has auto-snooze enabled, its interval, and the next live fire time. Overdue reminders should read like "overdue · repeats every 5m" rather than just changing color.
3. **Notification action model.** Define a `ReminderAction` domain/application command set (`Complete`, `Snooze(duration)`, `Reschedule(instant)`, `OpenTask`, `DisableReminder`). Wire the current tray balloon/menu path through these commands so Windows toast buttons can call the same path later.
4. **Inbox processing view.** Add a per-page Inbox capture heading or equivalent unprocessed predicate. Quick-add tasks default there when no heading is selected. Provide a filter/perspective that shows Inbox items and supports fast assignment to heading, tag, date, and action state.
5. **Forecast perspective.** Add a dedicated date/reminder view grouping `Overdue`, `Today`, `Tomorrow`, `This Week`, and `Later`. Include tasks with due dates, start dates, or active reminder `nextFireAt`. This is an OmniFocus-inspired view, not just a date dropdown.
6. **Review loop.** Add review metadata to `Heading` (or a future `Project` entity if headings are renamed): `reviewIntervalDays`, `lastReviewedAt`, `nextReviewAt`. Add a Review perspective that walks headings due for review and lets the user mark reviewed, defer, or open the heading focus view.
7. **Saved perspectives.** Introduce a small `Perspective` definition over the composed filter record from Phase A3: state view, date bucket/forecast group, tags, page, search text, heading focus, inbox-only, review-due-only, and show completed. Ship built-ins first: `Inbox`, `Forecast`, `Available`, `Waiting`, `Someday`, `Completed`, `Review`.
8. **Due timers stay deferred.** Reusable countdown timers are useful, but they are a separate surface from GTD task management. Do not overload task rows with timer semantics in this plan.

### Phase B — Chrome polish

1. **Menu bar** at the top of [`MainWindow.xaml`](src/App/Views/MainWindow.xaml): `File` (New / New Heading / Quit), `Edit` (Undo placeholder / Delete / Archive), `View` (Show Empty Headings / Show Done / Toggle Focus / Density), `Tools` (Settings / Backup Now placeholder), `Help` (About). Most items mirror existing keyboard bindings.
2. **Send-to-tray button** in the title bar (custom chrome). Render a small button in the non-client area or use an in-client title strip; clicking hides the window (mirrors `Esc`).
3. **Heading row affordances.** Show the tutorial-style hint `(press Space to focus)` in the status bar when a heading is selected, instead of mixing it into the heading title.
4. **`New ▼` compound button** on the toolbar (drop-down with `New Task` / `New Heading`) so the toolbar is reachable by mouse, not only by `Ctrl+N` / `Ctrl+H`.
5. **`Find` activation.** Tudumo treats Find as a toggle. Our always-on search box is already faster, so leave it — but add `Esc` behavior: if search box is empty, close it back to a label. Low priority.

### Phase C — Bug-fixes and gaps noticed during this pass

1. `Ctrl+1`–`Ctrl+7` are reserved in the SDD for state filtering but are not yet bound. Replace that reservation with the Phase A5 `Ctrl+1`–`Ctrl+8` filter-view scheme once the stateful filter button lands.
2. Number keys `1`–`7` to set state currently work in `TaskList_KeyDown`, but if an inline editor is open per Phase A.3, the editor must swallow them.
3. Heading creation prompt currently uses a `SimplePromptWindow`. Replace with an inline editable heading row when `Ctrl+H` is pressed.
4. `_db.GetTasks()` is called repeatedly across handlers. Cache the latest snapshot in `MainWindow` and refresh on save events to reduce DB churn (will matter once we hit the SDD's 20k-task target). After A2, this snapshot must include the tag projection so the bottom bar doesn't trigger an extra round trip per refresh.
5. **SDD must be updated** alongside Phase Ap / A2 / A5 / A6 to reflect:
   - Pages: new §2 / §6.1 / §12 entries for `Page` and `page_id` on `Heading` / `TaskItem` / `Tag`; §7 updated with the page-switching workflow; §8 covers the tab strip and cross-page move; §13 confirms the reminder engine is page-agnostic and aggregates by page in scan results.
   - Multi-tag decision: §5 "no tags" non-goal removed; §3.2 rewritten; §12 expanded with `Tag` (page-scoped) and `TaskTag` entities; §6.1 constraints updated to allow the bottom tag bar; §16 tests for tag extraction and merge.
   - State-model alignment (six action states, Action / Next / OnHold / Waiting / Someday / Done, with Inbox as capture view): §8.2 rewritten; §8.7 hotkey table changed (`Ctrl+1`–`Ctrl+8` are now filter views; bare `1`–`6` set state; `Ctrl+Tab` / `Ctrl+Alt+1`–`Ctrl+Alt+9` switch pages); §12.1 enum renumbered with a documented migration; SDD Appendix A updated.
   - Due / OmniFocus workflow: §7 adds Inbox processing, Forecast, and Review workflows; §8 adds saved perspectives and quick reminder presets; §13/§14 clarify notification action commands and visible auto-snooze state.
   Open as a single SDD edit pass once Phase Ap, A2, A5, and A6 designs are locked.

## 4. Decision — multi-tag support

Tudumo's bottom tag bar is unmistakably central in the screenshot. The SDD originally removed tags on the rationale that:

> headings/projects already give one strong axis of grouping, GTD-style "context" can be encoded in heading naming conventions

In practice the screenshot shows users put `@Computer`, `@online-`, etc. inline in titles and rely on the auto-extracted tag bar to slice across headings. Headings carry projects; tags carry contexts — they are not redundant.

**Decision: full multi-tag support is in scope** (formerly option 2 of three). Implementation lands in **Phase A2** (§3). Key shape:

- New `Tag` table (id, normalized `name`, `display_name`, `sort_order`, optional `color`, timestamps, soft-delete) with a unique-name constraint among non-deleted rows.
- New `TaskTag` join table with `(task_id, tag_id)` primary key and `ON DELETE CASCADE` from both sides.
- Tags are **auto-extracted from `@token` patterns in task titles** at save time. The user does not separately type tags in a tag field — typing `@home` in the title creates and links the tag.
- Inline title edits re-run the extraction so removing `@home` from a title also removes the link.
- **Bottom tag bar** with `Untagged` / `All` pseudo-items plus real tags in alphabetical order, with per-tag counts. Multi-select (additive); `Ctrl+click` for exclusive; clicking `All` clears.
- **Tag management** (rename / color / merge / delete) lives in a `Tools → Tags…` window introduced in Phase B.
- **Sync foundation** (HLC, change log, tombstones) extends to `Tag` and `TaskTag`. Conflict semantics on `TaskTag` are insert-wins / delete-by-newer-HLC, matching the SDD's per-field LWW model adapted for join rows.

This contradicts SDD §5 ("No tags") and §3.2 (rationale for removal). The SDD must be updated to match — see Phase C.5.

Why not the single-`context` middle ground? Tudumo's screenshot shows tasks like `@Computer @online-` co-occurring on one task; multi-tag is the natural model for GTD contexts and is no harder to reason about for sync than headings already are.

## 5. Out of scope for this plan

- Full reminder-engine rewrite, recurrence engine, and MSIX toast activation internals (covered by SDD Phase 2+). Due-style preset controls and action-command wiring are in scope via Phase A6.
- Outlook drag/drop (SDD Phase 4).
- Recurrence (SDD Phase 3+).
- MSIX packaging and Windows toast actions (SDD §10.2).
- Multi-language and theme work.

## 6. Suggested order

1. **Commit 0 — testability foundations.** All of Phase A0: fix solution wiring, add test projects, finish the time/id seams, test the existing `ReminderScanner`, `TaskFilter`, `DateInputParser`, and `Database.CreateTemp`, add `tests/run-tests.*`, and add `DIAGNOSTICS.md`. **No user-visible behavior changes** — this is the gate; nothing else lands until build and tests are green.
2. **Commit 1 — pure visual changes.** A.1 (state icons) · A.2 (density) · A.6 (heading collapse + count) · A.10 (status wording). Lowest risk, highest visible delta. State icons here use the v1 enum; the locked glyph table arrives in A5.8.
3. **Commit 2 — inline interaction.** A.3 (inline title edit) · A.4 (row expansion via Space) · A.5 (state click-to-cycle, cycle order finalized in A5). Expanded body content is empty in this commit; A5 fills it.
4. **Commit 3 — focus & headers.** A.7 (heading focus mode, Space disambiguated) · A.8 (Show Empty Headings) · A.9 (`+ Done` toggle).
5. **Commit 4 — pages.** All of Phase Ap. Schema gains `Page` plus `page_id` on `Heading` / `TaskItem`, and the A2 tag schema is locked to include `page_id` when created. Default-page backfill runs once. Page tab strip lights up. Lands before tags so the tag schema is correct from the first commit it ships in.
6. **Commit 5 — tags.** All of Phase A2. Bottom tag bar, `@token` extraction, tag manager. SDD edit (Phase C.5) lands in this commit.
7. **Commit 6 — filter rework.** Phase A3. Centralizes filter state and adds the date dropdown. Lands after A2 so tags and pages compose into the same `TaskFilter` record.
8. **Commit 7 — reordering.** Phase A4 (keyboard + drag-and-drop, including cross-page drag from Phase Ap.9). Independent of A2/A3; can branch in parallel with commit 5 or 6 but conceptually belongs after the row template settles.
9. **Commit 8 — state model + filter UX + expanded body.** Phase A5. Largest *risk* commit because it touches the state enum, Inbox migration, archive semantics, the filter dropdown, and the expanded row body. The SDD state-model update lands in this commit. Migration tests gate the merge. Pre-flight: take a backup of `tasks.db` before running the migration on real user data.
10. **Commit 9 — Due + OmniFocus workflow.** Phase A6. Quick time presets, visible auto-snooze state, reminder action commands, Inbox processing, Forecast, Review, and built-in perspectives. This commit makes the app feel like more than a Tudumo clone.
11. **Commit 10+ — chrome polish.** Phase B in any order. Phase C bug-fixes can be folded into earlier commits opportunistically.

Every commit from Commit 1 onward must add or extend tests in the projects established by Commit 0; see §7.4 for per-phase obligations.

## 7. Testing & AI verifiability

Goal: every functional change in this plan is covered by tests an AI agent can run, read, and diagnose without GUI access. This section defines the contract.

### 7.1 What "AI-checkable" means concretely

- **One command to run everything.** From the repo root, `dotnet test` (or `tests/run-tests.sh` / `.ps1` from Phase A0.8) builds and runs the full test suite. Exit code 0 = pass, non-zero = at least one failure.
- **Headless.** No tests require a display, a tray, an Outlook installation, network access, or admin rights. UI smoke tests live in a separate, opt-in project (out of MVP).
- **Deterministic.** No `DateTime.UtcNow` / `Guid.NewGuid()` / random / `Task.Delay` in test paths. All non-determinism passes through the seams in Phase A0 (`IClock`, `IIdGenerator`).
- **Self-describing failures.** Test names follow `MethodUnderTest_Scenario_ExpectedBehavior`. xUnit's default `Assert.Equal(expected, actual)` output names the field in question; failures should not require reading the test source to diagnose.
- **Machine-readable output.** `tests/_results/test-results*.trx` is regenerated on every run. The `.trx` schema is stable and parseable. Console output uses `--verbosity normal` so individual test names appear in stdout.
- **Diagnosable.** When a test fails, the AI agent should be able to:
  1. Read `test-results.trx` for the full failure list and stack traces.
  2. Read structured logs at `%LOCALAPPDATA%\WindowsTrayTasks\Logs\` (SDD §9.6 — added in SDD Phase 4) when an integration test reproduces a runtime issue.
  3. Reproduce the failure with `dotnet test --filter "FullyQualifiedName~<TestName>"`.
- **No flaky tests.** A test that fails intermittently is a bug; the suite must not contain `[Trait("Skip", "flaky")]` or `Thread.Sleep` retry loops.

### 7.2 Test categories

Three categories, each with its own project:

**A. Pure unit tests** (`tests/Domain.Tests`)
- Run in milliseconds. No filesystem, no DB, no time, no UI.
- Cover all logic in `Domain/`.
- Examples (per phase):
  - **Phase A0.6** — `DateInputParser_Plus5m_ReturnsNowPlus5Minutes`, `DateInputParser_InvalidString_ReturnsNull`, `DateInputParser_DstTransition_ParsesUnambiguousLocalTime`.
  - **Phase A2.3** — `TagExtractor_AtHome_ReturnsHome`, `TagExtractor_TrailingPunctuation_Stripped`, `TagExtractor_Hyphen_Allowed`, `TagExtractor_Empty_ReturnsEmpty`, `TagExtractor_NotAtMark_NotExtracted` (e.g. `email@example.com` does not yield `example`).
  - **Phase A4.1** — `SortOrderMath_Midpoint_ReturnsAverage`, `SortOrderMath_TopOfList_ReturnsLowerMinusOne`, `SortOrderMath_GapBelowEpsilon_RequestsRenumber`.
  - **Phase A5.2** — `StateMigrator_Inbox_BecomesActionInCaptureBucket`, `StateMigrator_Scheduled_BecomesNextWithStartDatePreserved`, `StateMigrator_Archived_BecomesDoneWithArchivedAtPreserved`.
  - **Phase A6** — `ReminderPreset_Tomorrow9_ReturnsNextLocalTomorrowNine`, `Perspective_Forecast_GroupsOverdueTodayUpcoming`, `ReviewSchedule_MarkReviewed_AdvancesNextReviewAt`.
  - **Phase A5.3** — `TaskFilter_OnlyCompleted_ExcludesNonDone`, `TaskFilter_Untagged_ExcludesTaggedTasks`, `TaskFilter_TagSetIsOr_NotAnd` (multi-tag selection unions, matches Tudumo).
  - **Reminder scanner** (Phase A0.4) — `ReminderScanner_PastNextFireAt_MarksOverdueAndAdvances`, `ReminderScanner_FutureNextFireAt_NoFire`, `ReminderScanner_ManyMissed_CoalescesToOneFireResult`, `ReminderScanner_AcknowledgedReminder_NoFire`.

**B. Integration tests** (`tests/Infrastructure.Tests`)
- Run in seconds. Use temp-file SQLite via `Database.CreateTemp()` (Phase A0.7); each test gets its own DB and disposes it in a `finally` block.
- Cover schema migrations, repository round-trips, transactional invariants, and cross-table cascades.
- Examples:
  - `Database_FreshInit_AppliesAllMigrations`.
  - `Database_SaveTask_RoundTrip` (write, re-open connection, read, assert equal).
  - `Database_OneActiveReminderPerTask_PartialUniqueIndexEnforced`.
  - `Database_DeleteTask_CascadesToReminders`.
  - **Phase A2** — `Database_SaveTaskWithAtTokens_CreatesTagAndTaskTagRows`, `Database_RenameTagToCollidingName_OffersMerge`.
  - **Phase A4** — `Database_ReorderTasks_ManyMoves_NeverViolatesUniqueSortOrder`.
  - **Phase A5.2** — `Migration_V1ToV2_RenumbersStateEnumWithoutDataLoss` (seed an old-schema DB, run migrator, assert post-state).

**C. UI smoke tests** (out of MVP)
- Future project `tests/Ui.Smoke` using FlaUI or WinAppDriver.
- Requires a Windows desktop session; not runnable in headless CI or by a sandboxed AI agent.
- Excluded from `dotnet test` by default via a `[Trait("Category", "Ui")]` filter; opt in with `--filter "Category=Ui"`.

### 7.3 Required test fixtures and helpers

Lives in `tests/TestSupport/` (a small shared project referenced by both test projects):

- `FakeClock` (implements `IClock`): supports `Set(DateTime)` and `Advance(TimeSpan)`; `UtcNow` is the controllable property.
- `SequentialIdGenerator` (implements `IIdGenerator`): returns `00000000-0000-0000-0000-{counter:D12}`. Counter resettable per test.
- `TempDatabase` (implements `IAsyncDisposable`): wraps `Database.CreateTemp()`, yields the path and disposes the file at end of test.
- `TaskBuilder` / `ReminderBuilder` / `HeadingBuilder`: fluent builders that produce valid domain objects with sensible defaults so tests only state what they care about.
- `TrxAssert`: assertions that emit context (e.g. surrounding row state) on failure so the `.trx` line is enough to diagnose without re-running.

### 7.4 Per-phase test obligations

Each phase commit must land with its tests in the same change. The plan's existing tasks are amended:

- **Phase A0.2 (`IClock`)** — add `FakeClock` tests asserting `Advance` and `Set` semantics.
- **Phase A0.3 (`IIdGenerator`)** — add `SequentialIdGenerator` deterministic-output tests.
- **Phase A0.4 (`ReminderScanner`)** — pure unit tests as listed in §7.2.A.
- **Phase A0.5 (filter pipeline)** — `TaskFilter_*` tests.
- **Phase A0.6 (`DateInputParser`)** — full input matrix.
- **Phase A0.7 (`Database.CreateTemp`)** — round-trip integration tests for `Heading`, `TaskItem`, `Reminder`.
- **Phase A.1–A.10** — visual changes are not unit-testable, but the supporting model changes (e.g. `Heading.Collapsed` toggling) get integration tests.
- **Phase Ap (pages)** — `Database_FreshInit_CreatesDefaultPage`, `Database_BackfillMigration_*`, `Database_DeletePageWithTasks_IsRefused`, `Database_MergePages_*`, `TaskFilter_ScopedByPage_*`, `Database_TagsArePageScoped_*`, `ReminderScanner_FiresAcrossPages_AggregatesByPageInResult`. The default-page backfill migration test gates the commit.
- **Phase A2 (tags)** — `TagExtractor` unit tests, `Database_SaveTaskWithAtTokens_*` integration tests, tag-merge integration test, `Database_CrossPageTagAssignment_IsRefused`.
- **Phase A3 (filter rework)** — extend `TaskFilter_*` to cover composed filters (state ∧ tag ∧ date ∧ search).
- **Phase A4 (reordering)** — `SortOrderMath_*` unit tests, plus the move-many-times-without-collision integration test in §7.2.B.
- **Phase A5 (state model + filter UX)** — `StateMigrator_*` unit tests, plus the migration integration test seeded with a v1-schema DB. Tests must prove Inbox is preserved as capture, not silently flattened. **Migration tests gate this commit** — refuse to merge if any test fails.
- **Phase A6 (Due + OmniFocus workflow)** — reminder preset unit tests, reminder action-command tests, Forecast grouping tests, Inbox processing tests, Perspective serialization tests, and Review scheduling tests.
- **Phase B / C** — chrome work and bug-fixes use whichever category fits.

### 7.5 How an AI agent diagnoses a failure

Documented in a new `DIAGNOSTICS.md` at the repo root (added with Phase A0):

1. Run `tests/run-tests.sh` (or `.ps1`). Capture stdout and exit code.
2. If exit code ≠ 0:
   - Parse `tests/_results/test-results*.trx` for `<UnitTestResult outcome="Failed">` nodes.
   - For each failure, the `<Output><ErrorInfo><Message>` and `<StackTrace>` fields contain enough to localize the bug.
   - Re-run a single failing test for an isolated repro: `dotnet test --filter "FullyQualifiedName~<TestName>" --logger "console;verbosity=detailed"`.
3. If the failure is in an integration test that touches the DB, locate the temp DB path printed by `TempDatabase` on failure (it does **not** delete on assertion failure — only on success). Inspect with the `sqlite3` CLI.
4. If a runtime crash reproduces only at app startup, run the app once, then read `%LOCALAPPDATA%\WindowsTrayTasks\Logs\latest.log` (added in SDD Phase 4) for the structured event trail.

`DIAGNOSTICS.md` lives at the repo root and is the canonical entry point for any AI / agent investigating a failure.

### 7.6 What this section does NOT cover

- **Performance tests / load tests** — SDD §9.2 / §16.4. Out of scope for this plan; revisit when the 20k-task NFR becomes load-bearing.
- **Mutation testing / property-based tests** — useful for `SortOrderMath` and `TagExtractor` but not required for v1.
- **Visual regression** — would require the UI smoke project to land first.

## 8. References checked in this review

- Tudumo historical behavior: How-To Geek's Tudumo review (`https://www.howtogeek.com/898/keyboard-ninja-manage-your-gtd-tasks-with-tudumo/`) and MillionClues' GTD-with-Tudumo notes (`https://millionclues.com/reviews/gtd-with-tudumo/`) confirm quick-add, tags, notes, start/due-date shortcuts, find, tray hiding, and six task states.
- Due current behavior: Due's official overview (`https://www.dueapp.com/`) and support docs (`https://www.dueapp.com/support/osx/fine-tuning-reminders.html`, `https://www.dueapp.com/support/osx/setting-a-reminder.html`) confirm Auto Snooze, quick due-date controls, notification snooze, recurring reminders, and supported auto-snooze intervals.
- OmniFocus current behavior: Omni Group's feature page (`https://www.omnigroup.com/products/omnifocus/features/`) confirms Inbox capture, projects, tags, Forecast, Review, rich notes/attachments, and customizable views/perspectives.
