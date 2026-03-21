namespace PiratesQuest.Server.Services;

public interface IDiscordNotifier
{
    Task NotifyPlayerPresenceAsync(string serverName, string username, bool isOnline, CancellationToken cancellationToken = default);
    Task NotifyServerOfflineAsync(string serverName, CancellationToken cancellationToken = default);
}
