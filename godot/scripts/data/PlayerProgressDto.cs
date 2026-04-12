namespace PiratesQuest.Data;

using System.Collections.Generic;

/// <summary>
/// Persisted progression counters used by quests.
/// These are long-term player milestones, not ship combat stats.
/// </summary>
public class PlayerProgressSnapshotDto
{
  public Dictionary<string, int> ItemsCollected { get; set; } = new();
  public Dictionary<string, int> ItemsBought { get; set; } = new();
  public Dictionary<string, int> ItemsSold { get; set; } = new();
  public Dictionary<string, int> SoldProfit { get; set; } = new();
  public Dictionary<string, float> AverageAcquisitionCostByItem { get; set; } = new();
  public Dictionary<string, int> QuantityAcquiredForCostByItem { get; set; } = new();
  public int ShipMovementInputs { get; set; }
  public int PortsVisitedCount { get; set; }
  public List<string> PortsVisited { get; set; } = [];
  public int CameraDrags { get; set; }
  public int CannonballsShot { get; set; }
  public int ShipsHit { get; set; }
  public int ShipsSunk { get; set; }
  public List<string> BoughtComponentNames { get; set; } = [];
  public List<string> HiredCrewIds { get; set; } = [];
  public List<string> TalkedToNpcIds { get; set; } = [];
  public int HighestShipTierReached { get; set; }
  public int TotalMoneyEarned { get; set; }
  public int TotalMoneySpent { get; set; }
}

public class PlayerProgressDto
{
  public PlayerProgressSnapshotDto Lifetime { get; set; } = new();
  public PlayerProgressSnapshotDto SinceQuestStart { get; set; } = new();
  public string CurrentQuestId { get; set; }
  public List<string> CompletedQuestIds { get; set; } = [];
  public List<string> UnlockedFeatures { get; set; } = [];
  public string AcceptedQuestNpcId { get; set; }
}
