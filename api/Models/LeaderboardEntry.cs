namespace PiratesQuest.Server.Models;

/// <summary>
/// Cached leaderboard row derived from saved player state.
/// We store the split values too so the API can expose a helpful breakdown.
/// </summary>
public class LeaderboardEntry
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public required string UserId { get; set; }
    public int InventoryGold { get; set; }
    public int VaultGold { get; set; }
    public int TotalGold { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public GameServer Server { get; set; } = null!;
}
