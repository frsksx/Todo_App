using Microsoft.Data.Sqlite;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;
using WindowsTrayTasks.Infrastructure.Sync;
using WindowsTrayTasks.TestSupport;

namespace WindowsTrayTasks.Infrastructure.Tests;

public sealed class DatabaseTests
{
    [Fact]
    public void CreateTemp_DisposeDeletesDatabaseFiles()
    {
        string dbPath;
        using (var temp = new TempDatabase())
        {
            dbPath = temp.Path;
            Assert.True(File.Exists(dbPath));
        }

        Assert.False(File.Exists(dbPath));
        Assert.False(File.Exists(dbPath + "-wal"));
        Assert.False(File.Exists(dbPath + "-shm"));
    }

    [Fact]
    public void SaveHeading_RoundTrips()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock);
        var heading = factory.CreateHeading("Work");

        temp.Database.SaveHeading(heading);

        var loaded = temp.Database.GetHeadings().Single();
        Assert.Equal(heading.Id, loaded.Id);
        Assert.Equal("Work", loaded.Title);
        Assert.Equal(temp.Clock.UtcNow, loaded.CreatedAt);
    }

    [Fact]
    public void SaveTask_RoundTripsThroughReopenedConnection()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock);
        var heading = factory.CreateHeading("Work");
        temp.Database.SaveHeading(heading);
        var task = factory.CreateTask("Prepare review", heading.Id, TaskState.Next);
        task.Notes = "Bring agenda";

        temp.Database.SaveTask(task);

        var reopened = new Database(temp.Clock, temp.Path);
        var loaded = reopened.GetTasks(includeArchived: true).Single();
        Assert.Equal(task.Id, loaded.Id);
        Assert.Equal(heading.Id, loaded.HeadingId);
        Assert.Equal(TaskState.Next, loaded.State);
        Assert.Equal("Bring agenda", loaded.Notes);
    }

    [Fact]
    public void SaveTask_LinkPriorityAndEffort_RoundTrip()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock);
        var task = factory.CreateTask("Prepare review");
        task.Link = "https://example.com/review";
        task.Priority = TaskPriority.High;
        task.EffortHours = 3;

        temp.Database.SaveTask(task);

        var loaded = temp.Database.GetTasks(includeArchived: true).Single();
        Assert.Equal("https://example.com/review", loaded.Link);
        Assert.Equal(TaskPriority.High, loaded.Priority);
        Assert.Equal(3, loaded.EffortHours);
    }

    [Fact]
    public void SaveTask_EnqueuesTaskAndDerivedTagChanges()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock, new SequentialIdGenerator(1000));
        var task = factory.CreateTask("Prepare review @work");

        temp.Database.SaveTask(task);

        var changes = temp.Database.GetSyncChanges();
        Assert.Contains(changes, c => c.EntityType == "task" && c.EntityId == task.Id.ToString() && c.Operation == "upsert");
        Assert.Contains(changes, c => c.EntityType == "tag" && c.Operation == "upsert");
        Assert.Contains(changes, c => c.EntityType == "task_tag" && c.Operation == "upsert");
    }

    [Fact]
    public void SaveReminder_RoundTrips()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock);
        var task = factory.CreateTask("Take medicine");
        temp.Database.SaveTask(task);
        var fireAt = temp.Clock.UtcNow.AddMinutes(5);
        var reminder = factory.CreateReminder(task.Id, fireAt, autoSnoozeIntervalMinutes: 10);

        temp.Database.SaveReminder(reminder);

        var loaded = temp.Database.GetReminderForTask(task.Id);
        Assert.NotNull(loaded);
        Assert.Equal(reminder.Id, loaded.Id);
        Assert.Equal(fireAt, loaded.NextFireAt);
        Assert.Equal(10, loaded.AutoSnoozeIntervalMinutes);
    }

    [Fact]
    public void DeleteRemindersForTask_SoftDeletesAndEnqueuesTombstone()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock, new SequentialIdGenerator(1100));
        var task = factory.CreateTask("Take medicine");
        temp.Database.SaveTask(task);
        var reminder = factory.CreateReminder(task.Id, temp.Clock.UtcNow.AddMinutes(5));
        temp.Database.SaveReminder(reminder);
        ClearOutbox(temp.Database);

        temp.Database.DeleteRemindersForTask(task.Id);

        Assert.Null(temp.Database.GetReminderForTask(task.Id));
        Assert.Empty(temp.Database.GetActiveReminders());
        var change = Assert.Single(temp.Database.GetSyncChanges());
        Assert.Equal("reminder", change.EntityType);
        Assert.Equal(reminder.Id.ToString(), change.EntityId);
        Assert.Equal("delete", change.Operation);
    }

    [Fact]
    public void SyncCursor_RoundTrips()
    {
        using var temp = new TempDatabase();

        temp.Database.SaveSyncCursor("remote_updated_at", "2026-05-03T12:00:00Z");

        Assert.Equal("2026-05-03T12:00:00Z", temp.Database.GetSyncCursor("remote_updated_at"));
    }

    [Fact]
    public void MarkSyncChangeComplete_RemovesOutboxEntry()
    {
        using var temp = new TempDatabase();
        temp.Database.EnqueueSyncChange("task", Guid.NewGuid().ToString(), "upsert", "{}");
        var change = Assert.Single(temp.Database.GetSyncChanges());

        temp.Database.MarkSyncChangeComplete(change.Id);

        Assert.Empty(temp.Database.GetSyncChanges());
        Assert.Equal(0, temp.Database.GetPendingSyncChangeCount());
    }

    [Fact]
    public void SupabaseSyncOptions_FromDatabase_GeneratesStableDeviceId()
    {
        using var temp = new TempDatabase();
        temp.Database.SaveSetting("sync_enabled", "1");
        temp.Database.SaveSetting("supabase_url", "https://example.supabase.co");
        temp.Database.SaveSetting("supabase_publishable_key", "publishable");

        var first = SupabaseSyncOptions.FromDatabase(temp.Database);
        var second = SupabaseSyncOptions.FromDatabase(temp.Database);

        Assert.True(first.IsConfigured);
        Assert.Equal(first.DeviceId, second.DeviceId);
    }

    [Fact]
    public void SaveReminder_SecondActiveReminderForSameTask_IsRejected()
    {
        using var temp = new TempDatabase();
        var factory = TestEntities.Factory(temp.Clock);
        var task = factory.CreateTask("One active reminder");
        temp.Database.SaveTask(task);
        temp.Database.SaveReminder(factory.CreateReminder(task.Id, temp.Clock.UtcNow.AddMinutes(5)));

        var second = factory.CreateReminder(task.Id, temp.Clock.UtcNow.AddMinutes(10));

        Assert.Throws<SqliteException>(() => temp.Database.SaveReminder(second));
    }

    [Fact]
    public void SaveTask_EmptyId_IsRejected()
    {
        using var temp = new TempDatabase();
        var task = new TaskItem { Title = "Broken" };

        Assert.Throws<ArgumentException>(() => temp.Database.SaveTask(task));
    }

    [Fact]
    public void Initialize_MigratesLegacyTaskStates()
    {
        var path = Path.Combine(Path.GetTempPath(), $"WindowsTrayTasksLegacyState-{Guid.NewGuid():N}.db");
        var updated = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);

        try
        {
            SeedLegacyTaskStates(path, updated);
            var clock = new FakeClock(new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc));

            using var db = new Database(clock, path, new SequentialIdGenerator(100));
            var tasks = db.GetTasks(includeArchived: true).ToDictionary(t => t.Title);

            Assert.Equal(TaskState.Action, tasks["Legacy inbox"].State);
            Assert.Equal(TaskState.Waiting, tasks["Legacy waiting"].State);
            Assert.Equal(TaskState.Next, tasks["Legacy scheduled"].State);
            Assert.Equal(TaskState.Archived, tasks["Legacy archived"].State);
            Assert.Equal(updated, tasks["Legacy archived"].ArchivedAt);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            TryDelete(path);
            TryDelete(path + "-wal");
            TryDelete(path + "-shm");
        }
    }

    private static void SeedLegacyTaskStates(string path, DateTime updated)
    {
        using var conn = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        conn.Open();
        using (var create = conn.CreateCommand())
        {
            create.CommandText = @"
CREATE TABLE TaskItem (
    id TEXT PRIMARY KEY,
    heading_id TEXT,
    title TEXT NOT NULL,
    notes TEXT,
    state INTEGER NOT NULL,
    sort_order REAL NOT NULL,
    start_at TEXT,
    due_at TEXT,
    completed_at TEXT,
    archived_at TEXT,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    deleted_at TEXT
);";
            create.ExecuteNonQuery();
        }

        InsertLegacyTask(conn, "00000000-0000-0000-0000-000000000201", "Legacy inbox", 1, updated);
        InsertLegacyTask(conn, "00000000-0000-0000-0000-000000000202", "Legacy waiting", 3, updated);
        InsertLegacyTask(conn, "00000000-0000-0000-0000-000000000203", "Legacy scheduled", 4, updated);
        InsertLegacyTask(conn, "00000000-0000-0000-0000-000000000204", "Legacy archived", 7, updated);
    }

    private static void InsertLegacyTask(SqliteConnection conn, string id, string title, int state, DateTime updated)
    {
        using var insert = conn.CreateCommand();
        insert.CommandText = @"
INSERT INTO TaskItem (id, title, state, sort_order, created_at, updated_at)
VALUES ($id, $title, $state, $sort, $created, $updated)";
        insert.Parameters.AddWithValue("$id", id);
        insert.Parameters.AddWithValue("$title", title);
        insert.Parameters.AddWithValue("$state", state);
        insert.Parameters.AddWithValue("$sort", state);
        insert.Parameters.AddWithValue("$created", updated.ToString("O"));
        insert.Parameters.AddWithValue("$updated", updated.ToString("O"));
        insert.ExecuteNonQuery();
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }

    private static void ClearOutbox(Database database)
    {
        foreach (var change in database.GetSyncChanges(1000))
            database.MarkSyncChangeComplete(change.Id);
    }

    [Fact]
    public void ArchiveCompletedOlderThan_MovesDoneTasksToArchivedState()
    {
        var now = new DateTime(2026, 5, 10, 10, 0, 0, DateTimeKind.Utc);
        using var temp = new TempDatabase(new FakeClock(now));
        var factory = TestEntities.Factory(temp.Clock);
        var oldDone = factory.CreateTask("Old done", state: TaskState.Done);
        oldDone.CompletedAt = now.AddDays(-8);
        var freshDone = factory.CreateTask("Fresh done", state: TaskState.Done);
        freshDone.CompletedAt = now.AddDays(-2);
        temp.Database.SaveTask(oldDone);
        temp.Database.SaveTask(freshDone);

        var count = temp.Database.ArchiveCompletedOlderThan(TimeSpan.FromDays(7));

        var tasks = temp.Database.GetTasks(includeArchived: true).ToDictionary(t => t.Title);
        Assert.Equal(1, count);
        Assert.Equal(TaskState.Archived, tasks["Old done"].State);
        Assert.Equal(now, tasks["Old done"].ArchivedAt);
        Assert.Equal(TaskState.Done, tasks["Fresh done"].State);
        Assert.Null(tasks["Fresh done"].ArchivedAt);
    }
}
