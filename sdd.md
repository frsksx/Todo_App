# Software Design Document: Windows Tray Task Manager

Status: Draft 0.4
Date: 2026-05-01
Product name: TBD

## 1. Purpose

This document describes a native Windows desktop task manager inspired by:

- Due's persistent, low-friction reminders and auto-snooze behavior.
- Tudumo's keyboard-driven, GTD-style task management model.

The product is intended for users who want a fast local task system that lives quietly in the Windows desktop tray and reliably resurfaces tasks until they are completed, rescheduled, or dismissed.

## 2. Product Summary

The application is a full Tudumo-like Windows task manager with Due-like reminders.

It runs locally, starts with Windows, lives in the system tray, and provides three primary interaction surfaces:

- A keyboard-first task window for organizing work into headings/projects, states, notes, start dates, due dates, and reminders.
- A compact quick-add window triggered by a global hotkey.
- A drop target accepting Outlook emails and (via a generic link-provider abstraction) other rich-content sources, so tasks can reference the source that caused the task.

Reminder behavior is persistent but configurable. The least intrusive first reminder is a tray icon color/state change. More visible reminders, such as Windows app notifications, follow based on user preference.

## 3. Research Notes

### 3.1 Due

Due's defining reminder behavior is Auto Snooze: overdue reminders repeatedly notify the user until the reminder is completed, rescheduled, or auto-snooze is disabled. Due for Mac supports common intervals such as 1, 5, 10, 15, 30, and 60 minutes. Due also supports quick access times, notification actions, recurring reminders, repeat-from-completion, reusable timers, and sync.

Relevant sources:

- Due overview: https://www.dueapp.com/
- Auto Snooze behavior: https://www.dueapp.com/support/osx/fine-tuning-reminders.html
- Setting reminders: https://www.dueapp.com/support/osx/setting-a-reminder.html
- Recurring reminders: https://www.dueapp.com/support/osx/setting-a-recurring-reminder.html
- Repeat from completion: https://www.dueapp.com/support/osx/setting-a-reminder-that-repeats-from-the-date-of-completion.html
- Due URL scheme recurrence fields: https://www.dueapp.com/developer.html

### 3.2 Tudumo

Tudumo was a Windows GTD-style task manager built around speed, headings/projects, task states, due/start dates, notes, search, and keyboard control. Historical sources describe global tray hotkeys, quick-add, keyboard navigation, note editing, date editing, state filtering, heading focus, and incremental search.

This product keeps Tudumo's keyboard-first task-management spirit and adopts Tudumo-style inline `@tag` contexts. Headings/projects remain the primary planning axis, while tags provide the cross-heading context axis for views like `@Calls`, `@Computer`, or `@Errands`. Tags are page-scoped, auto-extracted from task titles, and represented as a many-to-many `TaskTag` relation so one task can carry multiple contexts.

Relevant sources:

- How-To Geek Tudumo review: https://www.howtogeek.com/898/keyboard-ninja-manage-your-gtd-tasks-with-tudumo/
- Tudumo shortcut overview: https://www.paperblog.fr/288222/tudumo-une-todo-list-entierement-controlable-par-raccourcis-clavier/
- Tudumo file type notes: https://filext.com/file-extension/TUDUMO
- Tudumo summary: https://tudumo.en.lo4d.com/windows

### 3.3 Windows Platform Notes

Current Microsoft guidance recommends Windows App SDK `AppNotificationManager` for app notifications in WinUI 3, WPF, WinForms, and unpackaged Win32 apps. WPF apps can use Windows App SDK notification APIs through the `Microsoft.WindowsAppSDK` package.

A subtle but load-bearing consequence: notification *action callbacks* (Done/Snooze toast buttons) require a registered COM activator CLSID. This is straightforward in MSIX packaging and significantly fiddlier in unpackaged deployments (sparse signed manifest plus per-user COM server registration). This affects the packaging decision in §10 and §17.

Native startup integration is available for packaged desktop apps through startup tasks; unpackaged apps use conventional startup registration (Run key or Task Scheduler).

Relevant sources:

- Windows notifications overview: https://learn.microsoft.com/en-us/windows/apps/develop/notifications/
- App notifications overview: https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/
- WPF app notifications: https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-wpf
- Notification content/actions: https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-content
- Desktop packaging extensions and startup tasks: https://learn.microsoft.com/en-us/windows/apps/desktop/modernize/desktop-to-uwp-extensions
- .NET support policy: https://dotnet.microsoft.com/en-us/platform/support/policy

### 3.4 Outlook Integration Notes

Many real tasks begin as emails. The app supports dragging messages from Outlook directly into the task list or quick-add window.

For classic Outlook, the durable reference is an Outlook item reference using `EntryID` and `StoreID`, opened through Outlook's object model with `NameSpace.GetItemFromID(entryId, storeId)`. Microsoft documents that EntryIDs can change when an item is moved into another store, so the app stores fallback metadata (subject, sender, received time, internet message ID).

Relevant sources:

- MailItem EntryID: https://learn.microsoft.com/en-us/office/vba/api/outlook.mailitem.entryid
- Working with EntryIDs and StoreIDs: https://learn.microsoft.com/office/vba/outlook/How-to/Items-Folders-and-Stores/working-with-entryids-and-storeids

## 4. Goals

- Provide a full local Windows task manager, not only a reminder utility.
- Keep task capture and task navigation fast from the keyboard.
- Live in the Windows tray and remain available without occupying taskbar space by default.
- Support Due-like repeating reminder nudges until the user acts.
- Allow a non-intrusive reminder mode where tray icon color/state is the first reminder.
- Support Due-like auto-snooze intervals: 1, 5, 10, 15, 30, and 60 minutes, plus a custom snooze.
- Support recurring reminders that repeat from scheduled date and from completion date.
- Run at Windows startup.
- Recover cleanly after sleep, hibernation, reboot, or app restart.
- Handle missed reminders predictably (coalesced, never per-reminder storms).
- Survive scenarios where the app process is not running (Task Scheduler safety net).
- Store data locally first.
- Provide backup functionality in MVP.
- Prepare the data model and architecture for later sync without implementing cloud sync initially.
- Let users create or enrich tasks by dragging Outlook emails (and other rich content) into the app.

## 5. Non-Goals

- No mobile app in the first version.
- No cloud sync in the first version.
- No natural-language date parsing in the first version.
- No manual tag-management-heavy workflow in the first version; tags are auto-extracted from task titles.
- No team collaboration.
- No web app.
- No email/calendar integration in the first version.
- No full email client or mailbox synchronization.
- No automatic importing of email attachments by default.
- No AI task generation or prioritization in the first version.
- No inline rendering of email bodies in the first version (link out to Outlook only).

## 6. User Experience Principles

- Keyboard-first: every common workflow must be possible without the mouse.
- Quiet by default: the app should not steal focus for routine reminders.
- Persistent when needed: ignored reminders must not disappear into stale task lists.
- Local and trustworthy: the app should work offline and preserve data across crashes and reboots.
- Fast capture: adding a task should take seconds.
- Visible state: the tray icon should communicate whether something needs attention.

### 6.1 Visual And Interaction Design Direction

The visual direction is explicitly oriented around Due and Tudumo, not around a generic modern dashboard.

Design cues to preserve:

- Compact desktop utility, not a spacious project-management suite.
- List-first layout where reminders/tasks are the primary visual object.
- Calm, refined surfaces with restrained color and strong readability.
- Small, purposeful controls that stay out of the way until needed.
- Fast row-level editing rather than heavy modal workflows.
- Clear task rows with title, state, dates/reminder metadata, and notes disclosure.
- Keyboard focus that is always visible and predictable.
- Tray icon as a meaningful status object, not just a launcher.
- A quick-add surface that feels like a command window: immediate, narrow, and disposable.
- Subtle Due-like use of color for reminder urgency and time controls.
- Tudumo-like dense rows, headings, incremental search, and direct keyboard manipulation.

Design constraints:

- Avoid card-heavy layouts, dashboards, Kanban boards, large hero panels, or marketing-style empty states.
- Avoid tag UI entirely.
- Avoid oversized touch-first controls unless the user explicitly chooses a relaxed density setting.
- Avoid stealing focus for reminders unless the user opts into an assertive mode.
- Prefer inline editors, split panes, popovers, and compact keyboard-friendly pickers.

Proposed main-window shape:

- Top: narrow command/search/add bar.
- Center: dense task list grouped by headings/projects.
- Right or bottom inspector: only shown when editing details, notes, recurrence, or reminder settings.
- Bottom/status area: counts and active filter/search status; no tag bar.

Proposed quick-add shape:

- Single compact floating window, pre-warmed at app start (hidden, off-screen) to meet the 200 ms cold-show NFR (§9.2).
- First field is always task title.
- Secondary fields appear through keyboard shortcuts or a compact details row.
- Save, save-and-add-another, and cancel are reachable without mouse movement.

The design should feel closer to a careful native desktop instrument than a web app. It should have Due's polish and Tudumo's speed.

## 7. Primary Workflows

### 7.1 Quick Add

1. User presses the global quick-add hotkey.
2. The pre-warmed compact input window is shown and focused.
3. User enters a task title.
4. Optional fields can be reached from the keyboard:
   - Heading/project
   - State
   - Start date
   - Due date/reminder time
   - Auto-snooze interval
   - Recurrence rule
   - Notes
5. User presses Enter to save.
6. Window hides (or stays open in "save-and-add-another" mode).

The MVP does not parse natural language. Date/time entry uses explicit controls or structured fields, but those controls must be keyboard-friendly.

### 7.2 Show/Hide Main Window

1. User presses the global show/hide hotkey or clicks the tray icon.
2. The main task window opens near its previous position.
3. Pressing Escape or the same hotkey hides the window to tray.
4. Closing the window hides it by default; quitting requires an explicit command.

### 7.3 Create And Organize Tasks

1. User creates headings/projects.
2. User creates tasks under headings.
3. User changes task state with number keys.
4. User edits due date, start date, note, recurrence, or reminder settings with shortcuts.
5. User filters by state, heading, date, or search.

### 7.4 Reminder Lifecycle

1. A task reaches its reminder time.
2. Reminder engine marks it due.
3. Tray icon changes state immediately.
4. Depending on settings, a Windows app notification appears either immediately or at the next auto-snooze interval after the initial tray-only reminder.
5. User can:
   - Complete the task.
   - Snooze it (predefined intervals or custom).
   - Reschedule it.
   - Open it in the app.
   - Disable reminder/auto-snooze for that task.
6. If no action is taken, the reminder repeats at its auto-snooze interval.
7. For recurring tasks, completion creates the next occurrence as a new `TaskItem` linked by `recurrenceSeriesId` (see §8.5).

### 7.5 Wake From Sleep Or App Restart

1. App starts or receives a system resume event.
2. Reminder engine scans all active reminders.
3. Any reminder that should have fired while the app/system was unavailable is marked missed.
4. Missed reminders are coalesced into a single summary, never one notification per missed reminder.
5. Tray icon changes to overdue state.
6. The app shows a single summary notification if toast mode is enabled.
7. Auto-snooze continues from the recovery time.

### 7.6 Backup

1. App creates automatic local backups on a configured cadence.
2. User can create a manual backup.
3. User can restore from a backup file.
4. Backups include tasks, headings, settings, reminder schedules, and recurrence rules.
5. Backup format is documented and versioned (`schema_version` field at the top of every backup).
6. Backups are taken using SQLite's online backup API so the live database can stay open.

### 7.7 Drag An Outlook Email Or Other Rich Content Into The App

1. User drags an email from Outlook (or content from another supported source — see §8.10) into the main task list, a heading, an existing task, or the quick-add window.
2. If dropped onto a heading or empty list area, the app creates a task whose title defaults to the source's natural title (email subject, page title, file name).
3. If dropped onto an existing task, the app attaches the source reference to that task.
4. The app stores a provider-specific reference plus fallback metadata.
5. The task row shows a small source-type indicator.
6. User can press a shortcut or click the indicator to open the original source.
7. If the original source cannot be opened, the app explains the situation and shows the stored metadata.

MVP target: classic Outlook for Windows first. New Outlook support is treated as best-effort until its drag/drop behavior is verified. Other providers are scaffolded behind the same `LinkProvider` interface but only Outlook is shipped at MVP.

### 7.8 Today Review Mode

1. User presses a single keystroke (default `Ctrl+Alt+R`) to enter Review.
2. The app walks through every overdue + due-today task one at a time.
3. For each, the user can press a single key to: done, snooze (with sub-menu), reschedule, skip, or open.
4. Review ends when the queue is empty or the user presses Escape.
5. This is a pure UX layer over the existing reminder model — no new data.

## 8. Functional Requirements

### 8.1 Task Management

- Create, edit, complete, archive, and delete tasks.
- Organize tasks under headings/projects.
- Support notes per task.
- Support start date and due date.
- Support explicit reminder time independent of due date when needed.
- Support task states.
- Support search.
- Support filtering by:
  - State
  - Heading/project
  - Today
  - Upcoming
  - Overdue
  - Completed/archived
- Support task reordering within a heading.
- Support moving tasks between headings.
- Support copy/paste import/export of plain text task lists in a documented Markdown-style format: `## Heading` lines for headings, `- [ ] task` / `- [x] task` for tasks, indented lines for notes. This format round-trips losslessly for title/heading/state/notes; dates and reminders are dropped on plain-text export.

### 8.2 Task States

Default action states (6 total):

1. Action
2. Next
3. On Hold
4. Waiting
5. Someday/Maybe
6. Done

Inbox is a capture view for tasks without an assigned heading/project, not a task state. Scheduled work is represented by `startAt`, `dueAt`, or an active reminder rather than a separate state. Archived work is represented by `archivedAt` on completed tasks.

State-set hotkeys are `1`–`6` (see §8.7). State-filter hotkeys are `Ctrl+1`–`Ctrl+8`.

Reminders may be attached to tasks in any state. A task in `Someday` or `Waiting` with a reminder fires normally; the state controls visibility in default filters, not reminder behavior.

### 8.3 Reminders

- A task may have **at most one *active* reminder** plus zero or more *historical* reminder rows representing past fires/acknowledgements (see §12.3). The active reminder is enforced by a partial unique index on `(taskId)` where `status = 'active'`.
- A reminder has:
  - `fireAt` — the originally scheduled fire time (used by recurrence calculation; never mutated by snooze).
  - `nextFireAt` — the next live fire cursor; updated by snooze and auto-snooze.
  - `autoSnoozeEnabled`
  - `autoSnoozeIntervalMinutes`
  - `notificationMode`
  - `lastFiredAt`
  - `lastAcknowledgedAt`
  - `status` (active/snoozed/overdue/acknowledged/disabled)
- Supported auto-snooze intervals: 1, 5, 10, 15, 30, 60 minutes, plus a free-form "custom" snooze (any datetime in the future, e.g. "tomorrow at 9 am", "next Monday").
- Supported actions: mark done, snooze, reschedule, open task, disable reminder.
- A task **may have a reminder without a `dueAt`**, and may have a `dueAt` without a reminder. The reminder is the canonical surfacing mechanism; the due date is metadata.
- Reminder state survives app restart and system reboot (see §13).

### 8.4 Notification Modes

Per app and per task, with per-heading defaults that override the app default and are themselves overridden by per-task settings:

- Tray only
- Tray first, toast later (default)
- Toast immediately
- Silent overdue list only

Default behavior:

- First reminder: tray icon color/state change.
- If not acknowledged by the next auto-snooze interval: show Windows app notification.
- Continue with notifications at the selected interval until acted upon.

Focus Assist / Do Not Disturb: when the system reports an active focus session, toast notifications are suppressed and queued. The tray indicator continues to update. When focus ends, queued reminders are coalesced into a single summary toast (same coalescing path as §13.3 missed reminders).

### 8.5 Recurrence

The app supports two recurrence modes:

- **Repeat from scheduled date**: next occurrence is based on the prior scheduled occurrence.
- **Repeat from completion date**: next occurrence is based on when the user marks the task complete.

Recurrence model:

- Each recurring task series shares a `recurrenceSeriesId`.
- On completion of a recurring task, the current `TaskItem` is marked `Done` and a **new** `TaskItem` row is created for the next occurrence, sharing `recurrenceSeriesId` and the same `RecurrenceRule`. This preserves a full history per occurrence (good for sync, audit, "completed N times this month" reporting) and avoids the ambiguity of a single mutating row.
- The `RecurrenceRule` is owned by the series, not the individual task instance.

Supported recurrence rules for MVP:

- Daily every N days
- Weekly every N weeks
- Weekly on selected weekdays (with explicit `weekStart` per rule, defaulting to Windows locale first-day-of-week)
- Monthly on day of month
- Monthly on first/last weekday
- Yearly every N years

Edge-case rules (specified, not "decide later"):

- **End-of-month rounding**: if `dayOfMonth` does not exist in a given month (e.g. 31 in February), clamp to the last valid day of that month.
- **Leap day**: yearly recurrence on Feb 29 falls back to Feb 28 in non-leap years.
- **DST transitions**: if the wall-clock fire time falls in the spring-forward gap, the reminder fires at the start of the post-transition hour. If it falls in the fall-back overlap, it fires once at the first occurrence of the wall-clock time.
- **Week start**: weekly rules store an explicit `weekStart` (Mon/Sun/Sat). "Every 2 weeks on Monday" anchors against `weekStart`.

The recurrence model stores structured recurrence fields rather than opaque display strings, for sync/export compatibility.

### 8.6 Tray Behavior

Tray icon states:

- Neutral: no active overdue reminders.
- Due soon: reminder due within a configurable window.
- Overdue: one or more reminders due.
- Snoozed: overdue reminders exist but are snoozed.
- Paused: reminders globally paused.
- Error: backup, database, or notification subsystem issue.

Tooltip: always describes the state in words, never relies on color alone (accessibility).

Tray menu:

- Quick Add
- Open Tasks
- Today Review
- Overdue
- Snooze All
- Pause Reminders
- Backup Now
- Settings
- Quit

### 8.7 Keyboard Shortcuts

Global defaults are configurable. First-run conflict detection: on app startup, attempted hotkey registrations that fail due to conflict are reported in settings with a one-click "pick a different hotkey" remediation.

Global defaults (chosen to avoid known conflicts with PowerToys Run, Windows search, and common IMEs):

- Quick add: `Ctrl+Alt+Q`
- Show/hide main window: `Ctrl+Alt+T`
- Today review: `Ctrl+Alt+R`

In-app shortcuts:

- `Ctrl+N`: new task
- `Ctrl+H`: new heading/project
- `Enter` or `F2`: edit selected task/heading
- `Ctrl+Enter`: edit note
- `Ctrl+D`: edit due date/reminder
- `Ctrl+Shift+D`: edit start date
- `1`–`6`: set task state
- `Ctrl+1`–`Ctrl+8`: filter by view (`Only Next`, `Actions + Next`, `All except Done`, `Show All`, `Only On Hold`, `Only Waiting For`, `Only Someday/Maybe`, `Only Completed`)
- `Ctrl+Tab`: switch pages
- `Ctrl+Alt+1`–`Ctrl+Alt+9`: jump to page 1-9
- `Arrow Up/Down`: move selection
- `Ctrl+Arrow Up/Down`: jump between headings
- `Ctrl+Shift+Arrow Up/Down`: move selected task
- `Left/Right`: collapse/expand or focus/defocus heading
- `Space`: expand/collapse the selected task row or focus/defocus the selected heading
- `/` or `Ctrl+F`: search
- `Esc`: close search, close editor, or hide window depending on context

### 8.8 Startup

- App can be configured to start when the user logs into Windows.
- First launch prompts for the startup setting.
- MSIX deployment uses Windows startup task integration.
- Startup initializes silently into the tray unless the previous session had the main window open.

### 8.9 Backup, Crash Recovery, And Sync Preparation

MVP backup:

- Local automatic backup, default cadence: every 24 hours plus on every clean shutdown.
- Manual backup.
- Restore flow with explicit confirmation.
- Backup rotation: keep the last 14 daily, 8 weekly, 6 monthly. Configurable.
- Backup file validation on creation and on restore (schema version, row counts, integrity check).
- Default backup location: `%LOCALAPPDATA%\<AppName>\Backups`. The settings UI explicitly warns against pointing the backup folder at a OneDrive/Dropbox/Google Drive path, since cloud-sync of an active SQLite file can cause corruption and would silently create a half-supported "sync" path that doesn't own conflict resolution.

Sync preparation:

- Stable UUIDs for all syncable records.
- `createdAt`, `updatedAt`, `deletedAt` (tombstone) fields on all syncable entities.
- **Hybrid Logical Clock (HLC) timestamps** per row, in addition to wall-clock timestamps, so future sync can totally-order edits across devices without relying on system clock accuracy.
- Schema versioning.
- A `ChangeLog` table that stores **per-field deltas**, not just hashes (see §12.7).
- Conflict strategy: **per-field last-write-wins keyed by HLC**. This is conflict-safe in the sense that no edit is silently lost at the row level — two devices editing different fields of the same task both win on their respective fields. This is documented now even though no sync UI ships in MVP.

### 8.10 Source References (Outlook + Generic LinkProvider)

The drag/drop ingestion is built on a `LinkProvider` abstraction. Each provider knows how to:

- Inspect a drag/drop payload and decide if it can handle it.
- Extract a default title and metadata.
- Persist a durable reference.
- Attempt to re-open the source.
- Report broken-reference state.

MVP ships these providers:

- **OutlookClassicProvider** (full support).
- **OutlookNewProvider** (best-effort; gated by validation).
- **GenericFileProvider** (path reference; opens via shell).
- **GenericUrlProvider** (URL reference; opens in default browser).

Provider behavior:

- Drop onto main list empty area: create task.
- Drop onto heading/project: create task under heading.
- Drop onto existing task: attach source reference.
- Drop onto quick-add window: populate or attach to the draft task.
- One dropped item: default task title to the source's natural title.
- Multiple dropped items: create one task per item by default. A modifier-key drop (`Shift`) attaches all to a single new task instead.

Outlook-specific behavior:

- Store both `EntryID` and `StoreID`; do not rely on `EntryID` alone.
- Store fallback metadata because Outlook EntryIDs can change if the item moves to another store.
- Open via Outlook COM `NameSpace.GetItemFromID(entryId, storeId)`. On failure, surface stored metadata.
- Optional setting: save a local `.msg` snapshot at drop time using Outlook COM `SaveAs(olMSG)`. Off by default; togglable globally and per-task. Snapshots are stored under `%LOCALAPPDATA%\<AppName>\EmailSnapshots\` and are GC'd when the parent task is hard-deleted.
- Toast actions and "Done" behavior on a referenced task do not modify the email itself (read state, flags, etc.) — the reference is one-way.

## 9. Non-Functional Requirements

### 9.1 Reliability

- Reminder scanning is resilient to sleep, hibernation, reboot, and app crashes.
- A Windows Task Scheduler entry registered at install time provides a safety-net wake: the OS will launch (or signal) the app shortly before the next persisted `nextFireAt` if the app is not currently running. This handles the case where Windows Update reboots overnight and the user has not yet logged back in.
- Database writes are transactional. SQLite is configured with `journal_mode=WAL` and `synchronous=NORMAL`.
- App never loses a task because of notification failure.
- Notification failure degrades to tray/icon state and overdue list.

### 9.2 Performance

- Quick-add window appears in under 200 ms after hotkey press, achieved by pre-warming a hidden quick-add window at app start.
- Main window remains responsive with at least **20,000 active tasks** (raised from 5,000 to match power-user usage). List virtualization is required.
- Reminder scan runs in under 100 ms on a 20k-task database on startup.

### 9.3 Privacy And Telemetry

- All data is local in MVP.
- No analytics by default.
- No network access required for core functionality.
- **Opt-in crash reports**: a single setting toggles upload of minidumps + scrubbed log tail to a configured endpoint. Off by default; the first-run prompt explicitly does not enable it.
- Future sync must be opt-in.

### 9.4 Accessibility

- Full keyboard operation.
- Screen-reader-friendly labels for controls (UIAutomation peers on all custom controls).
- High contrast mode support.
- Tray state never relies on color alone; tooltip text always describes status.

### 9.5 Internationalization And Time

- MVP ships English-only.
- Date/time formatting uses Windows locale settings.
- All persisted timestamps are stored as UTC instants **plus** the originating IANA timezone id for any timestamp that has wall-clock semantics (reminders, recurrence anchors, due dates with explicit time-of-day). This lets "every weekday at 9 am" remain correct across DST and travel.

### 9.6 Logging And Diagnostics

- Structured logs written to `%LOCALAPPDATA%\<AppName>\Logs\` with daily rotation and 14-day retention.
- Log levels: error/warn/info; debug behind a setting.
- A "Copy Diagnostic Bundle" command in settings packages the last N log files, the schema version, the active hotkey registrations, and a redacted settings dump into a single `.zip` for support.
- The reminder engine emits a structured event per fire/snooze/ack so post-mortem of "why didn't I get the reminder?" is mechanical, not guesswork.

### 9.7 Security At Rest

- The SQLite database lives in the user's `%LOCALAPPDATA%`, relying on Windows ACLs for isolation between user accounts.
- An optional setting enables DPAPI-based at-rest encryption of the database file (per-user key). Off by default in MVP; documented as a hook for users with sensitive notes/email refs. Encrypted databases are not portable to another Windows account without re-keying — this is called out in the UI.
- Email snapshots (`.msg` files) inherit the same encryption setting.

### 9.8 Updates

- MSIX deployment uses standard Microsoft Store / sideloaded MSIX update channels.
- Update cadence and channel selection (stable / beta) is exposed in settings.
- Backup format is forward-compatible: an older app refuses to restore a newer-version backup with a clear error rather than silently dropping fields.

## 10. Recommended Technical Stack

### 10.1 Recommendation

Use a native C# Windows desktop stack:

- Runtime: .NET 10 LTS
- Language: C#
- UI: WPF
- Notifications: Windows App SDK `Microsoft.Windows.AppNotifications`
- Persistence: SQLite via `Microsoft.Data.Sqlite` with hand-rolled repositories (see §10.2 for rationale; **no EF Core**)
- Tray icon: WPF plus Win32/WinForms tray interop, using multiple `.ico` assets for states
- Global hotkeys: Win32 `RegisterHotKey` interop
- Single-instance: named mutex + named pipe for hotkey forwarding from the second instance to the first
- Packaging: **MSIX, required for MVP** (see §10.2)
- Tests: xUnit for domain/reminder logic; FlaUI or WinAppDriver for critical-path UI smoke tests

### 10.2 Rationale And Key Decisions

**Why MSIX, not unpackaged.** Toast-action callbacks (Done/Snooze on the notification) require a registered COM activator CLSID. MSIX makes this trivial; unpackaged makes it a per-user COM server registration with HKCU manifest tricks. The product depends on actionable notifications; therefore packaging is not optional. MSIX also gives clean startup-task and update stories.

**Why direct SQLite, not EF Core.** The schema has ~10 tables, several of which are append-only logs. EF Core's strengths (LINQ, migrations across complex graph queries) are not load-bearing here, while its costs (cold-start time, less direct control over WAL/PRAGMA settings, opaque SQL for the reminder hot path) work against the tray-app footprint and the 200 ms quick-add NFR. A thin repository layer plus hand-written SQL with parameterized commands and a small migration runner is the better fit.

**Why WPF, not WinUI 3.** WPF is mature for keyboard-heavy native desktop apps, has strong data binding, supports compact productivity interfaces well, has the better accessibility story today, and avoids the WinUI 3 packaging/startup edge cases. WinUI 3 is reconsidered post-MVP if "modern Windows 11 look" becomes a real product requirement.

**.NET 10 LTS** is supported until November 2028, comfortably covering the MVP and post-MVP roadmap.

### 10.3 Key Libraries And APIs

- `Microsoft.WindowsAppSDK` for notifications and Windows integration.
- `Microsoft.Data.Sqlite` for SQLite access.
- A small in-house migration runner over `Microsoft.Data.Sqlite` (no EF migrations).
- `System.Windows.Forms.NotifyIcon` or a focused tray-icon wrapper for WPF.
- Win32 `RegisterHotKey` for global shortcuts.
- `Microsoft.Win32.SystemEvents.PowerModeChanged` plus `WM_TIMECHANGE` and `WM_DISPLAYCHANGE` handling for resume/clock-change detection.
- Windows Task Scheduler integration for the safety-net wake (§9.1).
- Outlook COM interop for classic Outlook references, isolated behind the `LinkProvider` adapter.
- Serilog (or equivalent) for structured logging.

## 11. Architecture

### 11.1 Logical Components

- App Shell
  - Application startup
  - Single-instance handling (named mutex + named pipe)
  - Tray icon lifecycle
  - Window show/hide behavior
  - Quick-add pre-warm

- Task UI
  - Main window
  - Quick-add window
  - Today Review mode
  - Keyboard routing
  - Filters/search
  - Settings

- Task Domain
  - Task aggregate
  - Heading/project aggregate
  - Recurrence series aggregate
  - State transitions
  - Recurrence rules
  - Validation

- Reminder Engine
  - Due scan
  - Auto-snooze scheduling
  - Missed reminder recovery
  - Sleep/resume handling
  - Clock-change handling
  - Notification escalation
  - Task Scheduler safety-net registration

- Notification Adapter
  - Tray state updates
  - Windows app notifications
  - COM activator for toast actions
  - Focus Assist awareness

- LinkProvider Subsystem
  - `OutlookClassicProvider`
  - `OutlookNewProvider` (gated)
  - `GenericFileProvider`
  - `GenericUrlProvider`
  - Drag/drop data inspection routing

- Persistence
  - SQLite database (WAL)
  - Schema migrations
  - Repositories
  - Change log writer

- Backup Service
  - Online backup via SQLite backup API
  - Manual backup
  - Restore
  - Backup rotation
  - Backup validation

- Future Sync Boundary
  - Sync-ready IDs
  - HLC-stamped change feed
  - Tombstones
  - Per-field conflict metadata

### 11.2 Suggested Project Structure

```text
src/
  App/
    WindowsTrayTasks.App.csproj
    App.xaml
    App.xaml.cs
    Shell/
    Views/
    ViewModels/
  Domain/
    WindowsTrayTasks.Domain.csproj
    Tasks/
    Reminders/
    Recurrence/
  Infrastructure/
    WindowsTrayTasks.Infrastructure.csproj
    Persistence/
    Notifications/
    Tray/
    Hotkeys/
    Startup/
    Backup/
    LinkProviders/
      Outlook/
      Generic/
    Logging/
tests/
  WindowsTrayTasks.Domain.Tests/
  WindowsTrayTasks.Infrastructure.Tests/
docs/
  sdd.md
```

## 12. Data Model

All datetime fields are stored as UTC instants. Fields with wall-clock semantics (anything the user picks on a calendar) additionally store an IANA `timezoneId` so DST/travel are handled correctly. All syncable rows carry an HLC timestamp (`hlc`) alongside `updatedAt`.

### 12.1 Entity: TaskItem

- `id`: UUID
- `pageId`: UUID
- `headingId`: UUID nullable
- `recurrenceSeriesId`: UUID nullable (set for tasks that are part of a recurring series)
- `title`: string
- `notes`: string nullable
- `state`: enum (`action`/`next`/`on_hold`/`waiting`/`someday`/`done`)
- `sortOrder`: fractional sortable key
- `startAt`: datetime (UTC) nullable
- `startAtTimezone`: string nullable
- `dueAt`: datetime (UTC) nullable
- `dueAtTimezone`: string nullable
- `completedAt`: datetime (UTC) nullable
- `archivedAt`: datetime (UTC) nullable
- `createdAt`, `updatedAt`, `deletedAt`: datetime (UTC)
- `hlc`: string
- `version`: integer

### 12.2 Entity: Heading

- `id`: UUID
- `pageId`: UUID
- `title`: string
- `sortOrder`: fractional sortable key
- `collapsed`: boolean
- `defaultNotificationMode`: enum nullable (per-heading override of app default; per-task settings override this in turn)
- `createdAt`, `updatedAt`, `deletedAt`: datetime (UTC)
- `hlc`: string

### 12.2a Entity: Page

- `id`: UUID
- `name`: string
- `sortOrder`: fractional sortable key
- `lastFilterView`: string
- `lastFocusedHeadingId`: UUID nullable
- `lastSearchText`: string nullable
- `isDefault`: boolean
- `createdAt`, `updatedAt`, `deletedAt`: datetime (UTC)
- `hlc`: string

### 12.2b Entities: Tag and TaskTag

`Tag`:

- `id`: UUID
- `pageId`: UUID
- `name`: normalized string, unique per page among non-deleted tags
- `displayName`: string
- `sortOrder`: fractional sortable key
- `color`: string nullable
- `createdAt`, `updatedAt`, `deletedAt`: datetime (UTC)
- `hlc`: string

`TaskTag`:

- `taskId`: UUID
- `tagId`: UUID
- `createdAt`: datetime (UTC)

Tags are auto-extracted from `@token` patterns in task titles. Removing a token from the title removes that `TaskTag` link on save.

### 12.3 Entity: Reminder

- `id`: UUID
- `taskId`: UUID
- `enabled`: boolean
- `fireAt`: datetime (UTC) — original scheduled fire; never mutated by snooze
- `fireAtTimezone`: string nullable
- `nextFireAt`: datetime (UTC) nullable — live cursor advanced by snooze/auto-snooze
- `lastFiredAt`: datetime (UTC) nullable
- `lastAcknowledgedAt`: datetime (UTC) nullable
- `autoSnoozeEnabled`: boolean
- `autoSnoozeIntervalMinutes`: enum (1/5/10/15/30/60)
- `notificationMode`: enum nullable (null = inherit from heading/app)
- `status`: enum (`active`/`snoozed`/`overdue`/`acknowledged`/`disabled`)
- `createdAt`, `updatedAt`: datetime (UTC)
- `hlc`: string

Constraint: a partial unique index on `(taskId)` where `status IN ('active', 'snoozed', 'overdue')` enforces "at most one active reminder per task." Acknowledged/disabled rows accumulate as history.

### 12.4 Entity: RecurrenceSeries

- `id`: UUID (= `recurrenceSeriesId` referenced by `TaskItem`)
- `enabled`: boolean
- `unit`: enum (`daily`/`weekly`/`monthly`/`yearly`)
- `interval`: integer
- `weekdays`: bitmask nullable (weekly mode only)
- `weekStart`: enum (`monday`/`sunday`/`saturday`) — defaults to Windows locale first-day-of-week
- `monthlyMode`: enum (`day_of_month`/`nth_weekday`/`last_weekday`) nullable
- `dayOfMonth`: integer nullable
- `nthWeekday`: integer nullable (1–4, or -1 for last)
- `weekdayOfMonth`: integer nullable
- `endOfMonthRule`: enum (`clamp`/`skip`) — defaults to `clamp`
- `fromMode`: enum (`scheduled_date`/`completion_date`)
- `timezoneId`: string (IANA)
- `seriesStartedAt`: datetime (UTC)
- `seriesEndsAt`: datetime (UTC) nullable
- `createdAt`, `updatedAt`: datetime (UTC)
- `hlc`: string

When the series ends (no more occurrences), the last completed occurrence stays visible with a "series complete" marker; no orphan task is created.

### 12.5 Entity: AppSetting

- `key`: string
- `value`: string/json
- `scope`: enum (`user`/`machine`/`profile`) — `user` is per-Windows-account; `profile` is reserved for future multi-profile support
- `syncable`: boolean — whether this setting participates in the change log
- `updatedAt`: datetime (UTC)

### 12.6 Entity: SourceReference (was EmailReference)

Renamed to reflect the generic LinkProvider abstraction. Outlook is one provider among several.

- `id`: UUID
- `taskId`: UUID
- `provider`: enum (`outlook_classic`/`outlook_new`/`file`/`url`/`future`)
- `title`: string (subject for email, page title for URL, file name for file)
- `senderName`: string nullable
- `senderEmail`: string nullable
- `sentAt`: datetime (UTC) nullable
- `receivedAt`: datetime (UTC) nullable
- `outlookEntryId`: string nullable
- `outlookStoreId`: string nullable
- `internetMessageId`: string nullable
- `filePath`: string nullable
- `url`: string nullable
- `localSnapshotPath`: string nullable
- `lastOpenedAt`: datetime (UTC) nullable
- `lastOpenError`: string nullable
- `createdAt`, `updatedAt`, `deletedAt`: datetime (UTC)
- `hlc`: string

Constraints (enforced via CHECK constraints, indexed for dedup):

- `provider = 'outlook_classic'` → `outlookEntryId` and `outlookStoreId` NOT NULL.
- `provider = 'outlook_new'` → at least one of `internetMessageId` / `outlookEntryId` NOT NULL.
- `provider = 'file'` → `filePath` NOT NULL.
- `provider = 'url'` → `url` NOT NULL.

Indices:

- `(internetMessageId)` for dedup on email drops.
- `(taskId)` for "open all references for this task."

### 12.7 Entity: ChangeLog

- `id`: UUID
- `entityType`: string
- `entityId`: UUID
- `operation`: enum (`insert`/`update`/`delete`)
- `fieldDeltas`: JSON object — `{ "fieldName": { "old": ..., "new": ... } }` for `update`; full row snapshot for `insert`; null for `delete`
- `changedAt`: datetime (UTC)
- `hlc`: string
- `originDeviceId`: UUID — local device id (set up at first run); allows future sync to filter own writes

The change log enables future per-field-LWW sync, backup verification, and undo of recent edits.

## 13. Reminder Engine Design

### 13.1 Engine Loop

The reminder engine maintains an in-memory schedule derived from persisted reminders.

It wakes when:

- App starts.
- User creates/edits/completes/snoozes a reminder.
- A timer reaches the next known reminder time.
- Windows resumes from sleep (`PowerModeChanged.Resume`).
- System clock or timezone changes (`WM_TIMECHANGE`, `SystemEvents.TimeChanged`).
- Task Scheduler safety-net wake fires (§9.1).

On wake:

1. Recompute "now" in UTC and the local zone.
2. Load active reminders with `nextFireAt <= now`.
3. Mark reminders due or missed.
4. Coalesce notifications.
5. Update tray state.
6. Dispatch Windows app notification if policy and Focus Assist permit.
7. Persist state.
8. Schedule the next in-process timer and update the Task Scheduler safety-net target time.

### 13.2 Auto-Snooze

When a reminder becomes overdue and auto-snooze is enabled:

1. Set `status = overdue`.
2. Fire configured reminder surface.
3. Set `nextFireAt = now + autoSnoozeInterval`. (`fireAt` is never mutated.)
4. If no user action occurs by `nextFireAt`, repeat.

User actions:

- **Complete**: stops the active reminder; if recurring, the current `TaskItem` is marked `Done` and a new occurrence is created (see §8.5). If the new occurrence has a reminder, it is created with the next scheduled `fireAt`.
- **Snooze**: set `nextFireAt` to selected snooze time (predefined interval or custom datetime).
- **Reschedule**: set `fireAt` and `nextFireAt` to the chosen time and reset `status` to `active`.
- **Disable**: set `enabled = false`, `status = disabled`.

Toast "Done" UX for recurring tasks: when Done is invoked from a notification (no app focus), the toast is replaced with a confirmation toast that states "Done — next: <date/time>" so the user has visible confirmation that the next occurrence was scheduled.

### 13.3 Missed Reminders

Missed reminders are reminders with `nextFireAt` in the past when the app starts or resumes.

Rules:

- **Never emit a separate toast per missed reminder** on startup. Always coalesce.
- Show tray overdue state immediately, with count in tooltip.
- Show one summary toast if toast mode is enabled, listing the top N missed by time, with an "Open Today Review" action.
- Continue auto-snooze from the recovery time.
- Preserve the original missed `nextFireAt` in the change log for audit/debugging.

### 13.4 Sleep, Resume, Clock, And Timezone Changes

The app listens for resume, time-change, and significant clock-change events.

Examples handled:

- Laptop wakes after reminder time.
- User changes timezone (recurrence anchors are recomputed against the new zone using the rule's stored `timezoneId`, not the system zone).
- DST boundary occurs (see §8.5 for spring-forward/fall-back rules).
- System clock manually adjusted: if the jump is > 60 seconds, the engine treats it as a resume event and re-scans.

The recurrence engine calculates next occurrences using the rule's stored `timezoneId`, not raw UTC arithmetic, for any rule with wall-clock semantics.

## 14. Notification Design

### 14.1 Tray-First Reminder

Tray-first mode:

- Changes icon color/state.
- Updates tooltip with overdue count and next reminder.
- Plays no sound.
- Does not steal focus.

Icon states (final colors subject to design iteration):

- Gray: idle
- Blue: active reminders scheduled
- Amber: due soon
- Red: overdue
- Purple: reminders paused
- Warning overlay: error

Every state has a distinct shape/badge in addition to color (accessibility).

### 14.2 Windows App Notification

Notification content:

- Task title
- Heading/project
- Due/missed time (relative + absolute)
- Buttons: Done, Snooze (with selection input), Open

Snooze input options on the toast:

- 1, 5, 10, 15, 30, 60 minutes
- "Custom…" — opens the app focused on a snooze picker

Notification click opens the task in the main window.

Toast actions are handled via the registered COM activator (§3.3, §10.2).

## 15. Error Handling

- **Database unavailable**: tray error state, prevent edits, offer restore from backup.
- **Notification API unavailable or disabled**: continue tray reminders; show warning in settings.
- **Focus Assist active**: queue toasts, keep tray accurate, coalesce on resume.
- **Startup registration fails**: setting-level error with retry.
- **Backup fails**: tray error state only on repeated/severe failure; log error.
- **Corrupt backup**: refuse restore, preserve current database, surface error with diagnostic bundle prompt.
- **Outlook not installed / not running**: store the source reference, mark `lastOpenError`, show clear message that Outlook isn't available; metadata remains visible.
- **Outlook crashes during open**: catch the COM exception, surface stored metadata, do not destabilize the tray app.
- **Hotkey registration conflict**: surface the conflict in settings; tray menu remains a fallback launcher.

## 16. Testing Strategy

### 16.1 Unit Tests

- Recurrence calculation (incl. DST boundaries, leap year, end-of-month, week-start anchoring).
- Repeat from scheduled date.
- Repeat from completion date.
- Auto-snooze intervals incl. custom snooze.
- Missed reminder coalescing.
- State transitions.
- Backup manifest validation.
- HLC monotonicity.
- ChangeLog field-delta correctness.
- LinkProvider routing (which provider wins for a given drag payload).

### 16.2 Integration Tests

- SQLite migrations.
- Reminder persistence across restart.
- Backup and restore (online backup API on a live DB under load).
- Notification action handling through adapter abstraction (mocked COM activator).
- Global hotkey registration conflict handling.
- Task Scheduler safety-net registration and refresh.
- Outlook reference persistence and open-command behavior behind a testable adapter.
- DPAPI encryption round-trip.

### 16.3 Manual System Tests

- Start with Windows.
- Wake from sleep after reminder time.
- Disable Windows notifications and verify tray fallback.
- Enable Focus Assist and verify queue/coalesce behavior.
- Change timezone forward and backward.
- Change system clock by < 60 s and by > 60 s.
- Run with tray icon hidden by Windows overflow area.
- Restore backup over current database.
- Drag one email from classic Outlook onto a heading and open it from the resulting task.
- Drag multiple emails from classic Outlook and verify one task per email; verify Shift-drop attaches all to one task.
- Move an email to another store and verify broken-reference behavior is clear and non-destructive.
- Test New Outlook drag/drop behavior separately before claiming support.
- Reboot mid-recurrence and verify next occurrence is correct.
- Run a 50k-task seed and verify quick-add latency, scroll, and search.

### 16.4 Load And Stress

- Seed databases of 5k / 20k / 50k tasks; record cold-start, quick-add latency, search latency, reminder-scan latency.
- Soak test: 72 hours of simulated reminder fires through sleep/resume cycles.

## 17. MVP Scope

### Included

- Native WPF desktop app.
- MSIX packaging.
- Tray icon and tray menu.
- Main keyboard-first task window.
- Quick-add window (pre-warmed).
- Today Review mode.
- Headings/projects.
- 7 task states.
- Notes.
- Start date and due date.
- Reminder time, independent of due date.
- Due-like auto-snooze intervals plus custom snooze.
- Tray-first reminder mode.
- Windows app notifications with toast actions.
- Focus Assist awareness.
- Recurrence from scheduled date and completion date (new-instance model).
- Startup setting via MSIX startup task.
- Sleep/resume missed reminder handling.
- Task Scheduler safety-net wake.
- Local SQLite database (WAL).
- Local backup and restore via SQLite online backup API.
- Backup rotation and validation.
- Optional DPAPI at-rest encryption.
- Structured logs and diagnostic bundle.
- Opt-in crash reports.
- LinkProvider abstraction with Outlook Classic, generic file, and generic URL providers.
- HLC-stamped change log (foundation only; no sync UI).

### Deferred

- Cloud sync.
- Mobile companion app.
- Natural language parsing.
- Calendar integration.
- Import from Due/Tudumo/Todoist/TickTick/Things CSV.
- Advanced reporting.
- Custom themes beyond light/dark/system.
- Full New Outlook support until drag/drop and item-opening behavior is validated.
- Multi-profile (per Windows user is supported in MVP; multi-profile within one Windows user is deferred).

## 18. Roadmap

### Phase 1: Prototype

- WPF shell with MSIX packaging.
- Tray icon.
- Global hotkeys.
- Quick add (pre-warmed).
- SQLite task persistence (WAL).
- Simple reminder scan.
- Manual overdue list.
- LinkProvider scaffolding + Outlook Classic spike.

### Phase 2: MVP Reminder Engine

- Auto-snooze incl. custom snooze.
- Missed reminder coalescing.
- Windows app notifications via COM activator.
- Tray-first mode.
- Reminder actions.
- Sleep/resume handling.
- Task Scheduler safety-net wake.
- Focus Assist awareness.

### Phase 3: Full Task Manager

- Headings/projects with per-heading defaults.
- States and filters.
- Notes.
- Search.
- Reordering.
- Today Review mode.
- Keyboard polish.

### Phase 4: Outlook Hardening And Backup

- Outlook reference durability tests (move/delete/reopen).
- Optional `.msg` snapshot.
- Generic file/URL providers.
- Backup/restore via online backup API.
- Backup rotation and validation.
- Migration tests.
- Startup integration via MSIX.
- Error states and recovery flows.
- DPAPI encryption.
- Logs and diagnostic bundle.

### Phase 5: Sync Preparation

- HLC fully wired through writes.
- Per-field change feed.
- Tombstone GC.
- Export format.
- Conflict-resolution dry-run.
- Sync provider interface.

## 19. Risks And Mitigations

- **Tray icon hidden in Windows overflow area.** → Optional toast escalation and clear onboarding.
- **Windows notifications suppressed by Focus Assist/DND.** → Tray state, queued summary toast on resume, visible settings warning.
- **Global hotkey conflicts.** → Configurable hotkeys with first-run conflict detection and one-click remediation.
- **Calendar recurrence around DST/timezone changes.** → Timezone-aware rules with explicit DST/leap/end-of-month policies; targeted unit tests.
- **Repeat notifications become annoying.** → Per-task notification mode, per-heading defaults, pause reminders, snooze all, quiet hours.
- **Future sync becomes hard if MVP data model is too local-only.** → HLC stamps, tombstones, per-field change deltas, stable IDs from day one.
- **Outlook EntryID links break when emails move stores.** → Store StoreID + fallback metadata; preserve task; optional `.msg` snapshot.
- **New Outlook may not expose the same drag/drop or COM behavior.** → Classic-Outlook-first; LinkProvider abstraction isolates provider-specific code.
- **App not running when reminder is due.** → Task Scheduler safety-net wake registered at install time and refreshed on every persisted reminder change.
- **MSIX dependency limits where the app can be installed.** → Document the requirement; offer sideload instructions for environments without the Microsoft Store.
- **SQLite corruption from cloud-synced backup paths.** → Settings UI warns against OneDrive/Dropbox/Google Drive paths for the live DB or for actively-written backups.
- **Quick-add latency on cold app state.** → Pre-warm hidden window at startup; measured against the 200 ms NFR in load tests.

## 20. Open Decisions

The following intentionally remain open for product/design iteration; everything previously listed has been resolved in this draft.

- Product name.
- Final tray icon palette and badge shapes.
- Default cadence for automatic backups (current proposal: every 24 h + on clean shutdown).
- Whether Today Review should auto-launch on app start when there are >N overdue items.
- Visual style: classic compact productivity UI vs. a more modern Windows 11 style (within the constraints in §6.1).
- Whether per-heading defaults are exposed in MVP or deferred to Phase 3 polish.

## Appendix A: Resolved Decisions (changes from Draft 0.3)

This section is informational; future drafts may remove it.

- Reminder cardinality: at most one *active* reminder per task; historical rows accumulate. Enforced by partial unique index. (§8.3, §12.3)
- Recurrence model: each completion creates a new `TaskItem` linked by `recurrenceSeriesId`; `RecurrenceRule` becomes `RecurrenceSeries`. (§8.5, §12.4)
- Packaging: MSIX required for MVP, primarily because of toast-action COM activator requirements. (§3.3, §10.2)
- Persistence: direct `Microsoft.Data.Sqlite` with hand-rolled repositories; no EF Core. (§10.2)
- Reminders without due dates are supported; reminder is canonical, due date is metadata. (§8.3)
- Hotkeys retargeted to avoid PowerToys/IME conflicts: `Ctrl+Alt+Q` (quick-add), `Ctrl+Alt+T` (show/hide), `Ctrl+Alt+R` (review). (§8.7)
- Task states are 6 action states; Inbox is a capture view, Scheduled is date/reminder metadata, and Archived is `archivedAt`. Hotkeys are `1`-`6` for state and `Ctrl+1`-`Ctrl+8` for filter views. (§8.2, §8.7)
- DST/leap-year/end-of-month/week-start rules defined. (§8.5)
- Timestamps: UTC + IANA zone for wall-clock fields; HLC for sync ordering. (§9.5, §12)
- ChangeLog stores per-field deltas, not just hashes; conflict strategy is per-field LWW keyed by HLC. (§8.9, §12.7)
- App-not-running case: Windows Task Scheduler safety-net wake. (§9.1, §13.1)
- Logging, opt-in crash reports, optional DPAPI encryption, and update mechanism added. (§9.3, §9.6, §9.7, §9.8)
- 200 ms quick-add met via pre-warmed hidden window. Active-task perf bar raised to 20k. (§9.2)
- `EmailReference` generalized to `SourceReference` behind a `LinkProvider` abstraction (Outlook Classic, Outlook New, file, URL). (§8.10, §12.6)
- Backup uses SQLite online backup API; default location warns against cloud-sync paths. (§7.6, §8.9)
- Plain-text import/export format defined as Markdown-style. (§8.1)
- Today Review mode added. (§7.8)
- Tags are in scope as page-scoped, title-extracted, many-to-many contexts. (§3.2, §12.2b)
