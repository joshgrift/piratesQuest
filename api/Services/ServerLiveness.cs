namespace PiratesQuest.Server.Services;

/// <summary>
/// Shared server liveness rules so the API and background monitor agree.
/// </summary>
public static class ServerLiveness
{
    public const int OnlineWindowSeconds = 150;

    public static bool IsOnline(DateTime? lastSeenUtc, DateTime nowUtc)
    {
        return lastSeenUtc.HasValue
            && (nowUtc - lastSeenUtc.Value).TotalSeconds <= OnlineWindowSeconds;
    }
}
