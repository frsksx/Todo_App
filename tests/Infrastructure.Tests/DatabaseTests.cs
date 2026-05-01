using Microsoft.Data.Sqlite;
using WindowsTrayTasks.Domain;
using WindowsTrayTasks.Infrastructure.Persistence;
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
}
