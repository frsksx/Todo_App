using WindowsTrayTasks.Infrastructure.Persistence;

namespace WindowsTrayTasks.Infrastructure.Sync;

public sealed record SupabaseSyncOptions(
    bool Enabled,
    string? Url,
    string? PublishableKey,
    string DeviceId,
    TimeSpan SyncInterval)
{
    public bool IsConfigured =>
        Enabled
        && !string.IsNullOrWhiteSpace(Url)
        && !string.IsNullOrWhiteSpace(PublishableKey);

    public static SupabaseSyncOptions FromDatabase(Database database)
    {
        var deviceId = database.GetSetting("device_id");
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            deviceId = Guid.NewGuid().ToString("N");
            database.SaveSetting("device_id", deviceId);
        }

        return new SupabaseSyncOptions(
            database.GetSetting("sync_enabled") == "1",
            database.GetSetting("supabase_url"),
            database.GetSetting("supabase_publishable_key"),
            deviceId,
            TimeSpan.FromMinutes(5));
    }
}
