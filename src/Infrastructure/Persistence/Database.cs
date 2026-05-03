using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure;

namespace WindowsTrayTasks.Infrastructure.Persistence;

public sealed record SyncOutboxEntry(
    Guid Id,
    string EntityType,
    string EntityId,
    string Operation,
    string? PayloadJson,
    DateTime CreatedAt,
    int AttemptCount,
    string? LastError,
    DateTime? LockedAt);

public sealed class Database : IDisposable
{
    private static int _tempDatabaseCounter;

    private readonly string _connectionString;
    private readonly IClock _clock;
    private readonly IIdGenerator _ids;
    private readonly string? _tempPath;

    public Database(IClock clock, string? overridePath = null, IIdGenerator? ids = null)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _ids = ids ?? new SystemIdGenerator();
        var path = overridePath ?? DefaultDatabasePath();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
        }.ToString();
        Initialize();
    }

    private Database(IClock clock, IIdGenerator ids, string tempPath, bool _)
        : this(clock, tempPath, ids)
    {
        _tempPath = tempPath;
    }

    public static string DefaultDatabasePath()
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(local, "WindowsTrayTasks", "tasks.db");
    }

    /// <summary>
    /// Creates a Database backed by a fresh temp file. The instance is <see cref="IDisposable"/>;
    /// disposing it deletes the temp file (and its WAL/SHM siblings) so tests do not leak.
    /// On test failure, callers can opt out of cleanup by calling <see cref="GetTempPath"/> before
    /// disposing and reading the file from disk.
    /// </summary>
    public static Database CreateTemp(IClock? clock = null, IIdGenerator? ids = null)
    {
        clock ??= new SystemClock();
        ids ??= new SystemIdGenerator();
        string path;
        do
        {
            var n = Interlocked.Increment(ref _tempDatabaseCounter);
            path = Path.Combine(Path.GetTempPath(), $"WindowsTrayTasksTest-{Environment.ProcessId}-{n}.db");
        }
        while (File.Exists(path));

        return new Database(clock, ids, path, true);
    }

    public string? GetTempPath() => _tempPath;

    public void Dispose()
    {
        if (_tempPath is null) return;
        // Force close the shared SQLite cache so the file handles are released, then delete.
        SqliteConnection.ClearAllPools();
        TryDelete(_tempPath);
        TryDelete(_tempPath + "-wal");
        TryDelete(_tempPath + "-shm");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best-effort */ }
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var pragma = conn.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;";
        pragma.ExecuteNonQuery();
        return conn;
    }

    private void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS Page (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    sort_order REAL NOT NULL,
    last_filter_view TEXT NOT NULL DEFAULT 'All',
    last_focused_heading_id TEXT,
    last_search_text TEXT,
    is_default INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    deleted_at TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_page_name ON Page(lower(name)) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS Tag (
    id TEXT PRIMARY KEY,
    page_id TEXT NOT NULL,
    name TEXT NOT NULL,
    display_name TEXT NOT NULL,
    sort_order REAL NOT NULL,
    color TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    deleted_at TEXT,
    FOREIGN KEY (page_id) REFERENCES Page(id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_tag_page_name ON Tag(page_id, name) WHERE deleted_at IS NULL;

CREATE TABLE IF NOT EXISTS TaskTag (
    task_id TEXT NOT NULL,
    tag_id TEXT NOT NULL,
    created_at TEXT NOT NULL,
    PRIMARY KEY(task_id, tag_id),
    FOREIGN KEY (task_id) REFERENCES TaskItem(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES Tag(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS AppSetting (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS Heading (
    id TEXT PRIMARY KEY,
    page_id TEXT NOT NULL,
    title TEXT NOT NULL,
    sort_order REAL NOT NULL,
    collapsed INTEGER NOT NULL DEFAULT 0,
    review_interval_days INTEGER NOT NULL DEFAULT 7,
    last_reviewed_at TEXT,
    next_review_at TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    deleted_at TEXT,
    FOREIGN KEY (page_id) REFERENCES Page(id) ON DELETE RESTRICT
);

CREATE TABLE IF NOT EXISTS TaskItem (
    id TEXT PRIMARY KEY,
    page_id TEXT NOT NULL,
    heading_id TEXT,
    title TEXT NOT NULL,
    notes TEXT,
    state INTEGER NOT NULL,
    sort_order REAL NOT NULL,
    start_at TEXT,
    due_at TEXT,
    recurrence TEXT,
    link TEXT,
    priority INTEGER,
    effort_hours INTEGER,
    completed_at TEXT,
    archived_at TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    deleted_at TEXT,
    FOREIGN KEY (page_id) REFERENCES Page(id) ON DELETE RESTRICT,
    FOREIGN KEY (heading_id) REFERENCES Heading(id)
);

CREATE TABLE IF NOT EXISTS Reminder (
    id TEXT PRIMARY KEY,
    task_id TEXT NOT NULL,
    enabled INTEGER NOT NULL DEFAULT 1,
    fire_at TEXT NOT NULL,
    next_fire_at TEXT,
    last_fired_at TEXT,
    last_acknowledged_at TEXT,
    auto_snooze_enabled INTEGER NOT NULL DEFAULT 1,
    auto_snooze_interval_minutes INTEGER NOT NULL DEFAULT 5,
    status INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    deleted_at TEXT,
    FOREIGN KEY (task_id) REFERENCES TaskItem(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS SyncOutbox (
    id TEXT PRIMARY KEY,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    operation TEXT NOT NULL,
    payload_json TEXT,
    created_at TEXT NOT NULL,
    attempt_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    locked_at TEXT
);

CREATE TABLE IF NOT EXISTS SyncCursor (
    key TEXT PRIMARY KEY,
    value TEXT,
    updated_at TEXT NOT NULL
);

";
        cmd.ExecuteNonQuery();

        var defaultPageId = EnsureDefaultPage(conn);
        EnsureColumn(conn, "Heading", "page_id", "TEXT");
        EnsureColumn(conn, "Heading", "review_interval_days", "INTEGER NOT NULL DEFAULT 7");
        EnsureColumn(conn, "Heading", "last_reviewed_at", "TEXT");
        EnsureColumn(conn, "Heading", "next_review_at", "TEXT");
        EnsureColumn(conn, "TaskItem", "page_id", "TEXT");
        EnsureColumn(conn, "TaskItem", "recurrence", "TEXT");
        EnsureColumn(conn, "TaskItem", "link", "TEXT");
        EnsureColumn(conn, "TaskItem", "priority", "INTEGER");
        EnsureColumn(conn, "TaskItem", "effort_hours", "INTEGER");
        EnsureColumn(conn, "TaskItem", "archived_at", "TEXT");
        EnsureColumn(conn, "Reminder", "deleted_at", "TEXT");
        ExecuteNonQuery(conn, "UPDATE Heading SET page_id=$page WHERE page_id IS NULL OR page_id=''", ("$page", defaultPageId.ToString()));
        ExecuteNonQuery(conn, "UPDATE TaskItem SET page_id=$page WHERE page_id IS NULL OR page_id=''", ("$page", defaultPageId.ToString()));
        MigrateTaskStatesIfNeeded(conn);

        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_heading_page_sort ON Heading(page_id, sort_order) WHERE deleted_at IS NULL");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_task_page_sort ON TaskItem(page_id, sort_order) WHERE deleted_at IS NULL");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_tag_page_name ON Tag(page_id, name) WHERE deleted_at IS NULL");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_tasktag_tag ON TaskTag(tag_id)");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_task_heading ON TaskItem(heading_id) WHERE deleted_at IS NULL");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_reminder_active ON Reminder(next_fire_at) WHERE status IN (0, 1, 2) AND enabled = 1");
        ExecuteNonQuery(conn, "CREATE UNIQUE INDEX IF NOT EXISTS ux_reminder_one_active_per_task ON Reminder(task_id) WHERE status IN (0, 1, 2) AND enabled = 1 AND deleted_at IS NULL");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_sync_outbox_created ON SyncOutbox(created_at)");
        ExecuteNonQuery(conn, "CREATE INDEX IF NOT EXISTS idx_sync_outbox_entity ON SyncOutbox(entity_type, entity_id)");
    }

    private Guid EnsureDefaultPage(SqliteConnection conn)
    {
        using (var query = conn.CreateCommand())
        {
            query.CommandText = "SELECT id FROM Page WHERE is_default=1 AND deleted_at IS NULL ORDER BY created_at LIMIT 1";
            var existing = query.ExecuteScalar() as string;
            if (!string.IsNullOrWhiteSpace(existing)) return Guid.Parse(existing);
        }

        var now = _clock.UtcNow;
        var id = _ids.NewId();
        using var insert = conn.CreateCommand();
        insert.CommandText = @"
INSERT INTO Page (id, name, sort_order, last_filter_view, is_default, created_at, updated_at)
VALUES ($id, 'Tasks', 0, 'All', 1, $created, $updated);";
        insert.Parameters.AddWithValue("$id", id.ToString());
        insert.Parameters.AddWithValue("$created", Iso(now));
        insert.Parameters.AddWithValue("$updated", Iso(now));
        insert.ExecuteNonQuery();
        return id;
    }

    private static void EnsureColumn(SqliteConnection conn, string table, string column, string definition)
    {
        if (ColumnExists(conn, table, column)) return;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition}";
        cmd.ExecuteNonQuery();
    }

    private static bool ColumnExists(SqliteConnection conn, string table, string column)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static void ExecuteNonQuery(SqliteConnection conn, string sql, params (string Name, string Value)[] parameters)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            cmd.Parameters.AddWithValue(name, value);
        }
        cmd.ExecuteNonQuery();
    }

    private static string JsonPayload(object value)
        => JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = false });

    private void EnqueueSyncChange(
        SqliteConnection conn,
        SqliteTransaction? tx,
        string entityType,
        string entityId,
        string operation,
        string? payloadJson)
    {
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO SyncOutbox (id, entity_type, entity_id, operation, payload_json, created_at)
VALUES ($id, $entityType, $entityId, $operation, $payload, $created);";
        cmd.Parameters.AddWithValue("$id", _ids.NewId().ToString());
        cmd.Parameters.AddWithValue("$entityType", entityType);
        cmd.Parameters.AddWithValue("$entityId", entityId);
        cmd.Parameters.AddWithValue("$operation", operation);
        cmd.Parameters.AddWithValue("$payload", (object?)payloadJson ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", Iso(_clock.UtcNow));
        cmd.ExecuteNonQuery();
    }

    public void EnqueueSyncChange(string entityType, string entityId, string operation, string? payloadJson = null)
    {
        using var conn = Open();
        EnqueueSyncChange(conn, null, entityType, entityId, operation, payloadJson);
    }

    public List<SyncOutboxEntry> GetSyncChanges(int limit = 100)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT id, entity_type, entity_id, operation, payload_json, created_at, attempt_count, last_error, locked_at
FROM SyncOutbox
ORDER BY created_at ASC, id ASC
LIMIT $limit";
        cmd.Parameters.AddWithValue("$limit", limit);
        using var r = cmd.ExecuteReader();
        var changes = new List<SyncOutboxEntry>();
        while (r.Read())
        {
            changes.Add(new SyncOutboxEntry(
                Guid.Parse(r.GetString(0)),
                r.GetString(1),
                r.GetString(2),
                r.GetString(3),
                r.IsDBNull(4) ? null : r.GetString(4),
                ParseUtc(r.GetString(5)),
                r.GetInt32(6),
                r.IsDBNull(7) ? null : r.GetString(7),
                ParseUtcNullable(r.IsDBNull(8) ? null : r.GetValue(8))));
        }
        return changes;
    }

    public int GetPendingSyncChangeCount()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM SyncOutbox";
        return Convert.ToInt32(cmd.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public void MarkSyncChangeComplete(Guid id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SyncOutbox WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
    }

    public void MarkSyncChangeFailed(Guid id, string error)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
UPDATE SyncOutbox
SET attempt_count=attempt_count + 1,
    last_error=$error,
    locked_at=NULL
WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.Parameters.AddWithValue("$error", error);
        cmd.ExecuteNonQuery();
    }

    public string? GetSyncCursor(string key)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM SyncCursor WHERE key=$key";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SaveSyncCursor(string key, string? value)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO SyncCursor (key, value, updated_at)
VALUES ($key, $value, $updated)
ON CONFLICT(key) DO UPDATE SET value=excluded.value, updated_at=excluded.updated_at;";
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", (object?)value ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$updated", Iso(_clock.UtcNow));
        cmd.ExecuteNonQuery();
    }

    private void MigrateTaskStatesIfNeeded(SqliteConnection conn)
    {
        var version = "";
        using (var get = conn.CreateCommand())
        {
            get.CommandText = "SELECT value FROM AppSetting WHERE key='state_model_version'";
            version = get.ExecuteScalar() as string ?? "";
            if (version == "3") return;
        }

        if (version != "2")
        {
            ExecuteNonQuery(conn, @"
UPDATE TaskItem
SET archived_at = CASE WHEN state = 7 AND archived_at IS NULL THEN updated_at ELSE archived_at END,
    state = CASE state
        WHEN 1 THEN 1
        WHEN 2 THEN 2
        WHEN 3 THEN 4
        WHEN 4 THEN 2
        WHEN 5 THEN 5
        WHEN 6 THEN 6
        WHEN 7 THEN 7
        ELSE 1
    END");
        }
        else
        {
            ExecuteNonQuery(conn, @"
UPDATE TaskItem
SET state = 7
WHERE archived_at IS NOT NULL");
        }

        using var set = conn.CreateCommand();
        set.CommandText = @"
INSERT INTO AppSetting (key, value, updated_at)
VALUES ('state_model_version', '3', $updated)
ON CONFLICT(key) DO UPDATE SET value='3', updated_at=excluded.updated_at;";
        set.Parameters.AddWithValue("$updated", Iso(_clock.UtcNow));
        set.ExecuteNonQuery();
    }

    private static string Iso(DateTime dt) => dt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture);
    private static string? IsoNullable(DateTime? dt) => dt.HasValue ? Iso(dt.Value) : null;
    private static DateTime ParseUtc(string s) => DateTime.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
    private static DateTime? ParseUtcNullable(object? o) => o is null or DBNull ? null : ParseUtc((string)o);
    private static Guid? ParseGuidNullable(object? o) => o is null or DBNull ? null : Guid.Parse((string)o);

    // ---------- Pages ----------

    public List<Page> GetPages()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, name, sort_order, last_filter_view, last_focused_heading_id,
last_search_text, is_default, created_at, updated_at, deleted_at
FROM Page WHERE deleted_at IS NULL ORDER BY sort_order ASC, name ASC";
        using var r = cmd.ExecuteReader();
        var list = new List<Page>();
        while (r.Read())
        {
            list.Add(ReadPage(r));
        }
        return list;
    }

    public Page GetDefaultPage()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, name, sort_order, last_filter_view, last_focused_heading_id,
last_search_text, is_default, created_at, updated_at, deleted_at
FROM Page WHERE is_default=1 AND deleted_at IS NULL ORDER BY created_at LIMIT 1";
        using var r = cmd.ExecuteReader();
        if (r.Read()) return ReadPage(r);
        throw new InvalidOperationException("No default page exists.");
    }

    public Page? GetPage(Guid id)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, name, sort_order, last_filter_view, last_focused_heading_id,
last_search_text, is_default, created_at, updated_at, deleted_at
FROM Page WHERE id=$id AND deleted_at IS NULL";
        cmd.Parameters.AddWithValue("$id", id.ToString());
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadPage(r) : null;
    }

    public void SavePage(Page page, bool enqueueSync = true)
    {
        if (page.Id == Guid.Empty) throw new ArgumentException("Page id must be set before saving.", nameof(page));
        var now = _clock.UtcNow;
        if (page.CreatedAt == default) page.CreatedAt = now;
        page.UpdatedAt = now;
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO Page (id, name, sort_order, last_filter_view, last_focused_heading_id, last_search_text,
                  is_default, created_at, updated_at, deleted_at)
VALUES ($id, $name, $sort, $filter, $focused, $search, $isDefault, $created, $updated, $deleted)
ON CONFLICT(id) DO UPDATE SET
    name=excluded.name,
    sort_order=excluded.sort_order,
    last_filter_view=excluded.last_filter_view,
    last_focused_heading_id=excluded.last_focused_heading_id,
    last_search_text=excluded.last_search_text,
    is_default=excluded.is_default,
    updated_at=excluded.updated_at,
    deleted_at=excluded.deleted_at;";
        cmd.Parameters.AddWithValue("$id", page.Id.ToString());
        cmd.Parameters.AddWithValue("$name", page.Name);
        cmd.Parameters.AddWithValue("$sort", page.SortOrder);
        cmd.Parameters.AddWithValue("$filter", page.LastFilterView);
        cmd.Parameters.AddWithValue("$focused", (object?)page.LastFocusedHeadingId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$search", (object?)page.LastSearchText ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$isDefault", page.IsDefault ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", Iso(page.CreatedAt));
        cmd.Parameters.AddWithValue("$updated", Iso(page.UpdatedAt));
        cmd.Parameters.AddWithValue("$deleted", (object?)IsoNullable(page.DeletedAt) ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        if (enqueueSync)
            EnqueueSyncChange(conn, tx, "page", page.Id.ToString(), page.DeletedAt is null ? "upsert" : "delete", JsonPayload(page));
        tx.Commit();
    }

    public Guid GetActivePageId()
    {
        var raw = GetSetting("active_page_id");
        if (Guid.TryParse(raw, out var id) && GetPage(id) is not null) return id;
        var fallback = GetDefaultPage().Id;
        SaveActivePageId(fallback);
        return fallback;
    }

    public void SaveActivePageId(Guid pageId)
    {
        SaveSetting("active_page_id", pageId.ToString());
    }

    public string? GetSetting(string key)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM AppSetting WHERE key=$key";
        cmd.Parameters.AddWithValue("$key", key);
        return cmd.ExecuteScalar() as string;
    }

    public void SaveSetting(string key, string? value)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO AppSetting (key, value, updated_at)
VALUES ($key, $value, $updated)
ON CONFLICT(key) DO UPDATE SET
    value=excluded.value,
    updated_at=excluded.updated_at;";
        cmd.Parameters.AddWithValue("$key", key);
        cmd.Parameters.AddWithValue("$value", (object?)value ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$updated", Iso(_clock.UtcNow));
        cmd.ExecuteNonQuery();
    }

    private static Page ReadPage(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        Name = r.GetString(1),
        SortOrder = r.GetDouble(2),
        LastFilterView = r.GetString(3),
        LastFocusedHeadingId = ParseGuidNullable(r.IsDBNull(4) ? null : r.GetValue(4)),
        LastSearchText = r.IsDBNull(5) ? null : r.GetString(5),
        IsDefault = r.GetInt32(6) != 0,
        CreatedAt = ParseUtc(r.GetString(7)),
        UpdatedAt = ParseUtc(r.GetString(8)),
        DeletedAt = ParseUtcNullable(r.IsDBNull(9) ? null : r.GetValue(9)),
    };

    // ---------- Headings ----------

    public List<Heading> GetHeadings(Guid? pageId = null)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = pageId.HasValue
            ? @"SELECT id, page_id, title, sort_order, collapsed, review_interval_days, last_reviewed_at, next_review_at, created_at, updated_at, deleted_at
FROM Heading WHERE deleted_at IS NULL AND page_id=$page ORDER BY sort_order ASC, title ASC"
            : @"SELECT id, page_id, title, sort_order, collapsed, review_interval_days, last_reviewed_at, next_review_at, created_at, updated_at, deleted_at
FROM Heading WHERE deleted_at IS NULL ORDER BY sort_order ASC, title ASC";
        if (pageId.HasValue) cmd.Parameters.AddWithValue("$page", pageId.Value.ToString());
        using var r = cmd.ExecuteReader();
        var list = new List<Heading>();
        while (r.Read())
        {
            list.Add(new Heading
            {
                Id = Guid.Parse(r.GetString(0)),
                PageId = Guid.Parse(r.GetString(1)),
                Title = r.GetString(2),
                SortOrder = r.GetDouble(3),
                Collapsed = r.GetInt32(4) != 0,
                ReviewIntervalDays = r.GetInt32(5),
                LastReviewedAt = ParseUtcNullable(r.IsDBNull(6) ? null : r.GetValue(6)),
                NextReviewAt = ParseUtcNullable(r.IsDBNull(7) ? null : r.GetValue(7)),
                CreatedAt = ParseUtc(r.GetString(8)),
                UpdatedAt = ParseUtc(r.GetString(9)),
                DeletedAt = ParseUtcNullable(r.IsDBNull(10) ? null : r.GetValue(10)),
            });
        }
        return list;
    }

    public void SaveHeading(Heading h)
    {
        if (h.Id == Guid.Empty) throw new ArgumentException("Heading id must be set before saving.", nameof(h));
        var now = _clock.UtcNow;
        if (h.PageId == Guid.Empty) h.PageId = GetDefaultPage().Id;
        if (h.CreatedAt == default) h.CreatedAt = now;
        h.UpdatedAt = now;
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO Heading (id, page_id, title, sort_order, collapsed, review_interval_days, last_reviewed_at, next_review_at, created_at, updated_at, deleted_at)
VALUES ($id, $page, $title, $sort, $collapsed, $reviewInterval, $lastReviewed, $nextReview, $created, $updated, $deleted)
ON CONFLICT(id) DO UPDATE SET
    page_id=excluded.page_id,
    title=excluded.title,
    sort_order=excluded.sort_order,
    collapsed=excluded.collapsed,
    review_interval_days=excluded.review_interval_days,
    last_reviewed_at=excluded.last_reviewed_at,
    next_review_at=excluded.next_review_at,
    updated_at=excluded.updated_at,
    deleted_at=excluded.deleted_at;";
        cmd.Parameters.AddWithValue("$id", h.Id.ToString());
        cmd.Parameters.AddWithValue("$page", h.PageId.ToString());
        cmd.Parameters.AddWithValue("$title", h.Title);
        cmd.Parameters.AddWithValue("$sort", h.SortOrder);
        cmd.Parameters.AddWithValue("$collapsed", h.Collapsed ? 1 : 0);
        cmd.Parameters.AddWithValue("$reviewInterval", h.ReviewIntervalDays);
        cmd.Parameters.AddWithValue("$lastReviewed", (object?)IsoNullable(h.LastReviewedAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$nextReview", (object?)IsoNullable(h.NextReviewAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", Iso(h.CreatedAt));
        cmd.Parameters.AddWithValue("$updated", Iso(h.UpdatedAt));
        cmd.Parameters.AddWithValue("$deleted", (object?)IsoNullable(h.DeletedAt) ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        EnqueueSyncChange(conn, tx, "heading", h.Id.ToString(), h.DeletedAt is null ? "upsert" : "delete", JsonPayload(h));
        tx.Commit();
    }

    public void DeleteHeading(Guid id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE Heading SET deleted_at=$now, updated_at=$now WHERE id=$id";
        var now = _clock.UtcNow;
        cmd.Parameters.AddWithValue("$now", Iso(now));
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
        EnqueueSyncChange(conn, tx, "heading", id.ToString(), "delete", JsonPayload(new { id, deleted_at = Iso(now) }));
        tx.Commit();
    }

    // ---------- Tasks ----------

    public List<TaskItem> GetTasks(bool includeArchived = false, Guid? pageId = null)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        var archivedPredicate = includeArchived ? "" : " AND archived_at IS NULL";
        var pagePredicate = pageId.HasValue ? " AND page_id=$page" : "";
        cmd.CommandText = $@"SELECT id, page_id, heading_id, title, notes, state, sort_order, start_at, due_at,
recurrence, link, priority, effort_hours, completed_at, archived_at, created_at, updated_at, deleted_at
FROM TaskItem WHERE deleted_at IS NULL{archivedPredicate}{pagePredicate} ORDER BY sort_order";
        if (pageId.HasValue) cmd.Parameters.AddWithValue("$page", pageId.Value.ToString());
        using var r = cmd.ExecuteReader();
        var list = new List<TaskItem>();
        while (r.Read())
        {
            list.Add(new TaskItem
            {
                Id = Guid.Parse(r.GetString(0)),
                PageId = Guid.Parse(r.GetString(1)),
                HeadingId = ParseGuidNullable(r.IsDBNull(2) ? null : r.GetValue(2)),
                Title = r.GetString(3),
                Notes = r.IsDBNull(4) ? null : r.GetString(4),
                State = (TaskState)r.GetInt32(5),
                SortOrder = r.GetDouble(6),
                StartAt = ParseUtcNullable(r.IsDBNull(7) ? null : r.GetValue(7)),
                DueAt = ParseUtcNullable(r.IsDBNull(8) ? null : r.GetValue(8)),
                Recurrence = r.IsDBNull(9) ? null : r.GetString(9),
                Link = r.IsDBNull(10) ? null : r.GetString(10),
                Priority = r.IsDBNull(11) ? null : (TaskPriority?)r.GetInt32(11),
                EffortHours = r.IsDBNull(12) ? null : (int?)r.GetInt32(12),
                CompletedAt = ParseUtcNullable(r.IsDBNull(13) ? null : r.GetValue(13)),
                ArchivedAt = ParseUtcNullable(r.IsDBNull(14) ? null : r.GetValue(14)),
                CreatedAt = ParseUtc(r.GetString(15)),
                UpdatedAt = ParseUtc(r.GetString(16)),
                DeletedAt = ParseUtcNullable(r.IsDBNull(17) ? null : r.GetValue(17)),
            });
        }
        LoadTagsForTasks(conn, list);
        return list;
    }

    private static void LoadTagsForTasks(SqliteConnection conn, List<TaskItem> tasks)
    {
        if (tasks.Count == 0) return;

        var tasksById = tasks.ToDictionary(t => t.Id);
        foreach (var chunk in tasks.Chunk(500))
        {
            using var cmd = conn.CreateCommand();
            var parameterNames = new List<string>();
            var index = 0;
            foreach (var task in chunk)
            {
                var name = "$task" + index++;
                parameterNames.Add(name);
                cmd.Parameters.AddWithValue(name, task.Id.ToString());
            }

            cmd.CommandText = $@"SELECT tt.task_id, tag.id, tag.page_id, tag.name, tag.display_name, tag.sort_order, tag.color,
tag.created_at, tag.updated_at, tag.deleted_at
FROM Tag tag
JOIN TaskTag tt ON tt.tag_id = tag.id
WHERE tt.task_id IN ({string.Join(", ", parameterNames)}) AND tag.deleted_at IS NULL
ORDER BY tt.task_id, tag.display_name COLLATE NOCASE";
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var taskId = Guid.Parse(r.GetString(0));
                if (!tasksById.TryGetValue(taskId, out var task)) continue;
                task.Tags.Add(new Tag
                {
                    Id = Guid.Parse(r.GetString(1)),
                    PageId = Guid.Parse(r.GetString(2)),
                    Name = r.GetString(3),
                    DisplayName = r.GetString(4),
                    SortOrder = r.GetDouble(5),
                    Color = r.IsDBNull(6) ? null : r.GetString(6),
                    CreatedAt = ParseUtc(r.GetString(7)),
                    UpdatedAt = ParseUtc(r.GetString(8)),
                    DeletedAt = ParseUtcNullable(r.IsDBNull(9) ? null : r.GetValue(9)),
                });
            }
        }
    }

    public void SaveTask(TaskItem t)
    {
        if (t.Id == Guid.Empty) throw new ArgumentException("Task id must be set before saving.", nameof(t));
        var now = _clock.UtcNow;
        if (t.PageId == Guid.Empty) t.PageId = GetDefaultPage().Id;
        if (t.CreatedAt == default) t.CreatedAt = now;
        if (t.State == TaskState.Archived) t.ArchivedAt ??= now;
        t.UpdatedAt = now;
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO TaskItem (id, page_id, heading_id, title, notes, state, sort_order, start_at, due_at, recurrence, link, priority, effort_hours, completed_at, archived_at, created_at, updated_at, deleted_at)
VALUES ($id, $page, $heading, $title, $notes, $state, $sort, $start, $due, $recurrence, $link, $priority, $effort, $completed, $archived, $created, $updated, $deleted)
ON CONFLICT(id) DO UPDATE SET
    page_id=excluded.page_id,
    heading_id=excluded.heading_id,
    title=excluded.title,
    notes=excluded.notes,
    state=excluded.state,
    sort_order=excluded.sort_order,
    start_at=excluded.start_at,
    due_at=excluded.due_at,
    recurrence=excluded.recurrence,
    link=excluded.link,
    priority=excluded.priority,
    effort_hours=excluded.effort_hours,
    completed_at=excluded.completed_at,
    archived_at=excluded.archived_at,
    updated_at=excluded.updated_at,
    deleted_at=excluded.deleted_at;";
        cmd.Parameters.AddWithValue("$id", t.Id.ToString());
        cmd.Parameters.AddWithValue("$page", t.PageId.ToString());
        cmd.Parameters.AddWithValue("$heading", (object?)t.HeadingId?.ToString() ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$title", t.Title);
        cmd.Parameters.AddWithValue("$notes", (object?)t.Notes ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$state", (int)t.State);
        cmd.Parameters.AddWithValue("$sort", t.SortOrder);
        cmd.Parameters.AddWithValue("$start", (object?)IsoNullable(t.StartAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$due", (object?)IsoNullable(t.DueAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$recurrence", (object?)t.Recurrence ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$link", (object?)t.Link ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$priority", t.Priority.HasValue ? (object)(int)t.Priority.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("$effort", t.EffortHours.HasValue ? (object)t.EffortHours.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("$completed", (object?)IsoNullable(t.CompletedAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$archived", (object?)IsoNullable(t.ArchivedAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", Iso(t.CreatedAt));
        cmd.Parameters.AddWithValue("$updated", Iso(t.UpdatedAt));
        cmd.Parameters.AddWithValue("$deleted", (object?)IsoNullable(t.DeletedAt) ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        ReconcileTaskTags(conn, tx, t);
        EnqueueSyncChange(conn, tx, "task", t.Id.ToString(), t.DeletedAt is null ? "upsert" : "delete", JsonPayload(t));
        tx.Commit();
    }

    public void DeleteTask(Guid id)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "UPDATE TaskItem SET deleted_at=$now, updated_at=$now WHERE id=$id";
        var now = _clock.UtcNow;
        cmd.Parameters.AddWithValue("$now", Iso(now));
        cmd.Parameters.AddWithValue("$id", id.ToString());
        cmd.ExecuteNonQuery();
        EnqueueSyncChange(conn, tx, "task", id.ToString(), "delete", JsonPayload(new { id, deleted_at = Iso(now) }));
        tx.Commit();
    }

    public int ArchiveCompletedOlderThan(TimeSpan age)
    {
        var now = _clock.UtcNow;
        var cutoff = now - age;
        using var conn = Open();
        using var select = conn.CreateCommand();
        select.CommandText = @"
SELECT id, page_id, heading_id, title, notes, state, sort_order, start_at, due_at,
recurrence, link, priority, effort_hours, completed_at, archived_at, created_at, updated_at, deleted_at
FROM TaskItem
WHERE deleted_at IS NULL
  AND archived_at IS NULL
  AND state=$done
  AND completed_at IS NOT NULL
  AND completed_at <= $cutoff";
        select.Parameters.AddWithValue("$done", (int)TaskState.Done);
        select.Parameters.AddWithValue("$cutoff", Iso(cutoff));
        var affected = new List<TaskItem>();
        using (var r = select.ExecuteReader())
        {
            while (r.Read())
            {
                affected.Add(new TaskItem
                {
                    Id = Guid.Parse(r.GetString(0)),
                    PageId = Guid.Parse(r.GetString(1)),
                    HeadingId = ParseGuidNullable(r.IsDBNull(2) ? null : r.GetValue(2)),
                    Title = r.GetString(3),
                    Notes = r.IsDBNull(4) ? null : r.GetString(4),
                    State = TaskState.Archived,
                    SortOrder = r.GetDouble(6),
                    StartAt = ParseUtcNullable(r.IsDBNull(7) ? null : r.GetValue(7)),
                    DueAt = ParseUtcNullable(r.IsDBNull(8) ? null : r.GetValue(8)),
                    Recurrence = r.IsDBNull(9) ? null : r.GetString(9),
                    Link = r.IsDBNull(10) ? null : r.GetString(10),
                    Priority = r.IsDBNull(11) ? null : (TaskPriority?)r.GetInt32(11),
                    EffortHours = r.IsDBNull(12) ? null : (int?)r.GetInt32(12),
                    CompletedAt = ParseUtcNullable(r.IsDBNull(13) ? null : r.GetValue(13)),
                    ArchivedAt = now,
                    CreatedAt = ParseUtc(r.GetString(15)),
                    UpdatedAt = now,
                    DeletedAt = ParseUtcNullable(r.IsDBNull(17) ? null : r.GetValue(17)),
                });
            }
        }
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
UPDATE TaskItem
SET state=$archived, archived_at=$now, updated_at=$now
WHERE deleted_at IS NULL
  AND archived_at IS NULL
  AND state=$done
  AND completed_at IS NOT NULL
  AND completed_at <= $cutoff";
        cmd.Parameters.AddWithValue("$archived", (int)TaskState.Archived);
        cmd.Parameters.AddWithValue("$done", (int)TaskState.Done);
        cmd.Parameters.AddWithValue("$now", Iso(now));
        cmd.Parameters.AddWithValue("$cutoff", Iso(cutoff));
        var count = cmd.ExecuteNonQuery();
        foreach (var task in affected)
        {
            EnqueueSyncChange(conn, tx, "task", task.Id.ToString(), "upsert", JsonPayload(task));
        }
        tx.Commit();
        return count;
    }

    // ---------- Tags ----------

    public List<Tag> GetTags(Guid pageId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, page_id, name, display_name, sort_order, color, created_at, updated_at, deleted_at
FROM Tag WHERE deleted_at IS NULL AND page_id=$page ORDER BY display_name COLLATE NOCASE ASC";
        cmd.Parameters.AddWithValue("$page", pageId.ToString());
        using var r = cmd.ExecuteReader();
        var list = new List<Tag>();
        while (r.Read()) list.Add(ReadTag(r));
        return list;
    }

    public Dictionary<Guid, int> GetTagTaskCounts(Guid pageId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT tt.tag_id, COUNT(*)
FROM TaskTag tt
JOIN TaskItem t ON t.id = tt.task_id
JOIN Tag tag ON tag.id = tt.tag_id
WHERE t.deleted_at IS NULL
  AND t.archived_at IS NULL
  AND tag.deleted_at IS NULL
  AND tag.page_id = $page
GROUP BY tt.tag_id";
        cmd.Parameters.AddWithValue("$page", pageId.ToString());
        using var r = cmd.ExecuteReader();
        var counts = new Dictionary<Guid, int>();
        while (r.Read()) counts[Guid.Parse(r.GetString(0))] = r.GetInt32(1);
        return counts;
    }

    private void ReconcileTaskTags(SqliteConnection conn, SqliteTransaction tx, TaskItem task)
    {
        var previousTagIds = new HashSet<Guid>();
        using (var existing = conn.CreateCommand())
        {
            existing.Transaction = tx;
            existing.CommandText = "SELECT tag_id FROM TaskTag WHERE task_id=$task";
            existing.Parameters.AddWithValue("$task", task.Id.ToString());
            using var r = existing.ExecuteReader();
            while (r.Read()) previousTagIds.Add(Guid.Parse(r.GetString(0)));
        }

        using (var delete = conn.CreateCommand())
        {
            delete.Transaction = tx;
            delete.CommandText = "DELETE FROM TaskTag WHERE task_id=$task";
            delete.Parameters.AddWithValue("$task", task.Id.ToString());
            delete.ExecuteNonQuery();
        }

        var currentTagIds = new HashSet<Guid>();
        var tagNames = TagExtractor.ExtractTags(task.Title);
        foreach (var displayName in tagNames)
        {
            var tagId = UpsertTag(conn, tx, task.PageId, displayName);
            currentTagIds.Add(tagId);
            using var link = conn.CreateCommand();
            link.Transaction = tx;
            link.CommandText = "INSERT OR IGNORE INTO TaskTag (task_id, tag_id, created_at) VALUES ($task, $tag, $created)";
            link.Parameters.AddWithValue("$task", task.Id.ToString());
            link.Parameters.AddWithValue("$tag", tagId.ToString());
            link.Parameters.AddWithValue("$created", Iso(_clock.UtcNow));
            link.ExecuteNonQuery();
            EnqueueSyncChange(conn, tx, "task_tag", $"{task.Id}:{tagId}", "upsert", JsonPayload(new
            {
                task_id = task.Id,
                tag_id = tagId,
                created_at = Iso(_clock.UtcNow),
            }));
        }

        foreach (var removedTagId in previousTagIds.Except(currentTagIds))
        {
            EnqueueSyncChange(conn, tx, "task_tag", $"{task.Id}:{removedTagId}", "delete", JsonPayload(new
            {
                task_id = task.Id,
                tag_id = removedTagId,
                deleted_at = Iso(_clock.UtcNow),
            }));
        }
    }

    public void AddTag(Guid pageId, string displayName)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        UpsertTag(conn, tx, pageId, displayName);
        tx.Commit();
    }

    public Dictionary<Guid, (bool HasUrgentDue, bool HasNextActions)> GetPageTaskSummaries()
    {
        var cutoff = _clock.UtcNow.Date.AddDays(2).ToString("O");
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
SELECT page_id,
    MAX(CASE WHEN due_at IS NOT NULL AND due_at < $cutoff THEN 1 ELSE 0 END),
    MAX(CASE WHEN state = $next THEN 1 ELSE 0 END)
FROM TaskItem
WHERE deleted_at IS NULL AND archived_at IS NULL
GROUP BY page_id";
        cmd.Parameters.AddWithValue("$cutoff", cutoff);
        cmd.Parameters.AddWithValue("$next", (int)TaskState.Next);
        using var r = cmd.ExecuteReader();
        var result = new Dictionary<Guid, (bool, bool)>();
        while (r.Read())
        {
            result[Guid.Parse(r.GetString(0))] = (r.GetInt32(1) == 1, r.GetInt32(2) == 1);
        }
        return result;
    }

    private Guid UpsertTag(SqliteConnection conn, SqliteTransaction tx, Guid pageId, string displayName)
    {
        var normalized = TagExtractor.Normalize(displayName);
        using (var select = conn.CreateCommand())
        {
            select.Transaction = tx;
            select.CommandText = "SELECT id FROM Tag WHERE page_id=$page AND name=$name AND deleted_at IS NULL LIMIT 1";
            select.Parameters.AddWithValue("$page", pageId.ToString());
            select.Parameters.AddWithValue("$name", normalized);
            var existing = select.ExecuteScalar() as string;
            if (!string.IsNullOrWhiteSpace(existing)) return Guid.Parse(existing);
        }

        var now = _clock.UtcNow;
        var id = _ids.NewId();
        using var insert = conn.CreateCommand();
        insert.Transaction = tx;
        insert.CommandText = @"
INSERT INTO Tag (id, page_id, name, display_name, sort_order, created_at, updated_at)
VALUES ($id, $page, $name, $display, $sort, $created, $updated)";
        insert.Parameters.AddWithValue("$id", id.ToString());
        insert.Parameters.AddWithValue("$page", pageId.ToString());
        insert.Parameters.AddWithValue("$name", normalized);
        insert.Parameters.AddWithValue("$display", displayName.Trim().TrimStart('@'));
        insert.Parameters.AddWithValue("$sort", EntityFactory.DefaultSortOrder(now));
        insert.Parameters.AddWithValue("$created", Iso(now));
        insert.Parameters.AddWithValue("$updated", Iso(now));
        insert.ExecuteNonQuery();
        EnqueueSyncChange(conn, tx, "tag", id.ToString(), "upsert", JsonPayload(new
        {
            id,
            page_id = pageId,
            name = normalized,
            display_name = displayName.Trim().TrimStart('@'),
            sort_order = EntityFactory.DefaultSortOrder(now),
            color = (string?)null,
            created_at = Iso(now),
            updated_at = Iso(now),
            deleted_at = (string?)null,
        }));
        return id;
    }

    private static Tag ReadTag(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        PageId = Guid.Parse(r.GetString(1)),
        Name = r.GetString(2),
        DisplayName = r.GetString(3),
        SortOrder = r.GetDouble(4),
        Color = r.IsDBNull(5) ? null : r.GetString(5),
        CreatedAt = ParseUtc(r.GetString(6)),
        UpdatedAt = ParseUtc(r.GetString(7)),
        DeletedAt = ParseUtcNullable(r.IsDBNull(8) ? null : r.GetValue(8)),
    };

    // ---------- Reminders ----------

    public List<Reminder> GetActiveReminders()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, task_id, enabled, fire_at, next_fire_at, last_fired_at, last_acknowledged_at,
auto_snooze_enabled, auto_snooze_interval_minutes, status, created_at, updated_at, deleted_at
FROM Reminder WHERE enabled=1 AND status IN (0,1,2) AND deleted_at IS NULL";
        using var r = cmd.ExecuteReader();
        var list = new List<Reminder>();
        while (r.Read()) list.Add(ReadReminder(r));
        return list;
    }

    public Reminder? GetReminderForTask(Guid taskId)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT id, task_id, enabled, fire_at, next_fire_at, last_fired_at, last_acknowledged_at,
auto_snooze_enabled, auto_snooze_interval_minutes, status, created_at, updated_at, deleted_at
FROM Reminder WHERE task_id=$task AND status IN (0,1,2) AND deleted_at IS NULL ORDER BY created_at DESC LIMIT 1";
        cmd.Parameters.AddWithValue("$task", taskId.ToString());
        using var r = cmd.ExecuteReader();
        return r.Read() ? ReadReminder(r) : null;
    }

    public void SaveReminder(Reminder rem)
    {
        if (rem.Id == Guid.Empty) throw new ArgumentException("Reminder id must be set before saving.", nameof(rem));
        if (rem.TaskId == Guid.Empty) throw new ArgumentException("Reminder task id must be set before saving.", nameof(rem));
        var now = _clock.UtcNow;
        if (rem.CreatedAt == default) rem.CreatedAt = now;
        rem.UpdatedAt = now;
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = @"
INSERT INTO Reminder (id, task_id, enabled, fire_at, next_fire_at, last_fired_at, last_acknowledged_at,
                     auto_snooze_enabled, auto_snooze_interval_minutes, status, created_at, updated_at, deleted_at)
VALUES ($id, $task, $en, $fire, $next, $lastFired, $lastAck, $autoEn, $interval, $status, $created, $updated, $deleted)
ON CONFLICT(id) DO UPDATE SET
    enabled=excluded.enabled,
    fire_at=excluded.fire_at,
    next_fire_at=excluded.next_fire_at,
    last_fired_at=excluded.last_fired_at,
    last_acknowledged_at=excluded.last_acknowledged_at,
    auto_snooze_enabled=excluded.auto_snooze_enabled,
    auto_snooze_interval_minutes=excluded.auto_snooze_interval_minutes,
    status=excluded.status,
    updated_at=excluded.updated_at,
    deleted_at=excluded.deleted_at;";
        cmd.Parameters.AddWithValue("$id", rem.Id.ToString());
        cmd.Parameters.AddWithValue("$task", rem.TaskId.ToString());
        cmd.Parameters.AddWithValue("$en", rem.Enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$fire", Iso(rem.FireAt));
        cmd.Parameters.AddWithValue("$next", (object?)IsoNullable(rem.NextFireAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$lastFired", (object?)IsoNullable(rem.LastFiredAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$lastAck", (object?)IsoNullable(rem.LastAcknowledgedAt) ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$autoEn", rem.AutoSnoozeEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$interval", rem.AutoSnoozeIntervalMinutes);
        cmd.Parameters.AddWithValue("$status", (int)rem.Status);
        cmd.Parameters.AddWithValue("$created", Iso(rem.CreatedAt));
        cmd.Parameters.AddWithValue("$updated", Iso(rem.UpdatedAt));
        cmd.Parameters.AddWithValue("$deleted", (object?)IsoNullable(rem.DeletedAt) ?? DBNull.Value);
        cmd.ExecuteNonQuery();
        EnqueueSyncChange(conn, tx, "reminder", rem.Id.ToString(), rem.DeletedAt is null ? "upsert" : "delete", JsonPayload(rem));
        tx.Commit();
    }

    public void DeleteRemindersForTask(Guid taskId)
    {
        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        var now = _clock.UtcNow;
        var reminders = new List<Guid>();
        using (var select = conn.CreateCommand())
        {
            select.Transaction = tx;
            select.CommandText = "SELECT id FROM Reminder WHERE task_id=$task AND deleted_at IS NULL";
            select.Parameters.AddWithValue("$task", taskId.ToString());
            using var r = select.ExecuteReader();
            while (r.Read()) reminders.Add(Guid.Parse(r.GetString(0)));
        }

        cmd.CommandText = @"
UPDATE Reminder
SET deleted_at=$now,
    updated_at=$now,
    enabled=0,
    status=$disabled
WHERE task_id=$task AND deleted_at IS NULL";
        cmd.Parameters.AddWithValue("$now", Iso(now));
        cmd.Parameters.AddWithValue("$disabled", (int)ReminderStatus.Disabled);
        cmd.Parameters.AddWithValue("$task", taskId.ToString());
        cmd.ExecuteNonQuery();
        foreach (var reminderId in reminders)
        {
            EnqueueSyncChange(conn, tx, "reminder", reminderId.ToString(), "delete", JsonPayload(new
            {
                id = reminderId,
                task_id = taskId,
                deleted_at = Iso(now),
            }));
        }
        tx.Commit();
    }

    private static Reminder ReadReminder(SqliteDataReader r) => new()
    {
        Id = Guid.Parse(r.GetString(0)),
        TaskId = Guid.Parse(r.GetString(1)),
        Enabled = r.GetInt32(2) != 0,
        FireAt = ParseUtc(r.GetString(3)),
        NextFireAt = ParseUtcNullable(r.IsDBNull(4) ? null : r.GetValue(4)),
        LastFiredAt = ParseUtcNullable(r.IsDBNull(5) ? null : r.GetValue(5)),
        LastAcknowledgedAt = ParseUtcNullable(r.IsDBNull(6) ? null : r.GetValue(6)),
        AutoSnoozeEnabled = r.GetInt32(7) != 0,
        AutoSnoozeIntervalMinutes = r.GetInt32(8),
        Status = (ReminderStatus)r.GetInt32(9),
        CreatedAt = ParseUtc(r.GetString(10)),
        UpdatedAt = ParseUtc(r.GetString(11)),
        DeletedAt = ParseUtcNullable(r.IsDBNull(12) ? null : r.GetValue(12)),
    };
}
