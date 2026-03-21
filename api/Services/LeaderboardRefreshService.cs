using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PiratesQuest.Server.Data;
using PiratesQuest.Server.Models;

namespace PiratesQuest.Server.Services;

/// <summary>
/// Rebuilds the derived leaderboard table from the latest saved player state.
/// The source of truth stays in GameStates, and this table is just a cached view.
/// </summary>
public sealed class LeaderboardRefreshService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LeaderboardRefreshService> _logger;

    public LeaderboardRefreshService(
        IServiceScopeFactory scopeFactory,
        ILogger<LeaderboardRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RefreshLeaderboardAsync(stoppingToken);

        using var timer = new PeriodicTimer(RefreshInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshLeaderboardAsync(stoppingToken);
        }
    }

    public async Task RefreshLeaderboardAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var nowUtc = DateTime.UtcNow;

            var states = await db.GameStates
                .Select(state => new
                {
                    state.ServerId,
                    state.UserId,
                    state.State
                })
                .ToListAsync(cancellationToken);

            var entries = new List<LeaderboardEntry>(states.Count);

            foreach (var state in states)
            {
                if (!TryReadGoldTotals(state.State, out var inventoryGold, out var vaultGold))
                {
                    _logger.LogWarning(
                        "Skipping leaderboard refresh for user '{UserId}' on server {ServerId} because the saved state could not be parsed.",
                        state.UserId,
                        state.ServerId);
                    continue;
                }

                entries.Add(new LeaderboardEntry
                {
                    ServerId = state.ServerId,
                    UserId = state.UserId,
                    InventoryGold = inventoryGold,
                    VaultGold = vaultGold,
                    TotalGold = inventoryGold + vaultGold,
                    UpdatedAt = nowUtc
                });
            }

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            // The table is fully derived, so a replace-in-transaction keeps it accurate
            // without leaving half-written rows behind.
            await db.LeaderboardEntries.ExecuteDeleteAsync(cancellationToken);
            if (entries.Count > 0)
            {
                await db.LeaderboardEntries.AddRangeAsync(entries, cancellationToken);
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Leaderboard refresh failed.");
        }
    }

    private static bool TryReadGoldTotals(string stateJson, out int inventoryGold, out int vaultGold)
    {
        inventoryGold = 0;
        vaultGold = 0;

        try
        {
            using var document = JsonDocument.Parse(stateJson);
            var root = document.RootElement;

            // Current saves store inventory as a dictionary like { "Coin": 123 }.
            // Older payloads may have stored a top-level "gold" field, so we accept both.
            inventoryGold = ReadTopLevelGold(root) ?? ReadNestedCoinAmount(root, "Inventory");
            vaultGold = ReadNestedCoinAmount(root, "Vault", "Items");
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static int? ReadTopLevelGold(JsonElement root)
    {
        if (TryGetPropertyIgnoreCase(root, "gold", out var goldElement) &&
            goldElement.ValueKind == JsonValueKind.Number &&
            goldElement.TryGetInt32(out var gold))
        {
            return Math.Max(0, gold);
        }

        return null;
    }

    private static int ReadNestedCoinAmount(JsonElement root, params string[] path)
    {
        var current = root;
        foreach (var segment in path)
        {
            if (!TryGetPropertyIgnoreCase(current, segment, out current))
                return 0;
        }

        if (current.ValueKind != JsonValueKind.Object)
            return 0;

        if (!TryGetPropertyIgnoreCase(current, "Coin", out var coinElement))
            return 0;

        return coinElement.ValueKind == JsonValueKind.Number && coinElement.TryGetInt32(out var amount)
            ? Math.Max(0, amount)
            : 0;
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
