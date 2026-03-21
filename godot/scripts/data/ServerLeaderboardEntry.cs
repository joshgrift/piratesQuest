namespace PiratesQuest.Data;

/// <summary>
/// Mirrors the leaderboard rows returned by the backend API.
/// The dedicated server fetches these, then rebroadcasts them to clients via RPC.
/// </summary>
public class ServerLeaderboardEntry
{
  public string CaptainName { get; set; } = string.Empty;
  public int InventoryGold { get; set; }
  public int VaultGold { get; set; }
  public int TotalGold { get; set; }
}
