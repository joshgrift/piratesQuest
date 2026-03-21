using Microsoft.EntityFrameworkCore;
using PiratesQuest.Server.Data;

namespace PiratesQuest.Server.Services;

/// <summary>
/// Periodically checks for servers that stop heartbeating and alerts Discord once per outage.
/// </summary>
public sealed class ServerOfflineMonitor : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDiscordNotifier _discordNotifier;
    private readonly ILogger<ServerOfflineMonitor> _logger;
    private readonly Dictionary<int, bool> _knownStatuses = [];

    public ServerOfflineMonitor(
        IServiceScopeFactory scopeFactory,
        IDiscordNotifier discordNotifier,
        ILogger<ServerOfflineMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _discordNotifier = discordNotifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await SeedKnownStatusesAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckServersOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Server offline monitor failed during a polling cycle.");
            }
        }
    }

    public async Task CheckServersOnceAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nowUtc = DateTime.UtcNow;

        var servers = await db.GameServers
            .Where(server => server.IsActive)
            .Select(server => new
            {
                server.Id,
                server.Name,
                server.LastSeenUtc
            })
            .ToListAsync(cancellationToken);

        var activeServerIds = servers.Select(server => server.Id).ToHashSet();
        var staleIds = _knownStatuses.Keys
            .Where(serverId => !activeServerIds.Contains(serverId))
            .ToList();

        foreach (var staleId in staleIds)
            _knownStatuses.Remove(staleId);

        foreach (var server in servers)
        {
            var isOnline = ServerLiveness.IsOnline(server.LastSeenUtc, nowUtc);
            if (_knownStatuses.TryGetValue(server.Id, out var wasOnline) && wasOnline && !isOnline)
            {
                await _discordNotifier.NotifyServerOfflineAsync(server.Name, cancellationToken);
            }

            _knownStatuses[server.Id] = isOnline;
        }
    }

    private async Task SeedKnownStatusesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nowUtc = DateTime.UtcNow;

        var servers = await db.GameServers
            .Where(server => server.IsActive)
            .Select(server => new
            {
                server.Id,
                server.LastSeenUtc
            })
            .ToListAsync(cancellationToken);

        foreach (var server in servers)
            _knownStatuses[server.Id] = ServerLiveness.IsOnline(server.LastSeenUtc, nowUtc);
    }
}
