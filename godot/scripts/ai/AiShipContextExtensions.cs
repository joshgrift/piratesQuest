namespace PiratesQuest.AI;

using System;

/// <summary>
/// Query helpers for per-frame AI context data.
/// Keeping these out of AiShipContext.cs makes the main context type easier to scan.
/// </summary>
public static class AiShipContextExtensions
{
  /// <summary>
  /// Find the nearest non-allied ship contact, if one exists.
  /// Hunter AI uses this as its combat target.
  /// </summary>
  public static AiShipContact FindNearestHostileShip(this AiShipContext context)
  {
    AiShipContact best = null;
    float bestDistance = float.MaxValue;

    foreach (AiShipContact ship in context.NearbyShips)
    {
      if (ship == null || ship.IsAllied || ship.Distance >= bestDistance)
        continue;

      best = ship;
      bestDistance = ship.Distance;
    }

    return best;
  }

  /// <summary>
  /// Find the nearest ship contact currently marked as a threat.
  /// Trader AI uses this when deciding whether to flee.
  /// </summary>
  public static AiShipContact FindNearestThreatShip(this AiShipContext context)
  {
    AiShipContact best = null;
    float bestDistance = float.MaxValue;

    foreach (AiShipContact ship in context.NearbyShips)
    {
      if (ship == null || !ship.IsThreat || ship.Distance >= bestDistance)
        continue;

      best = ship;
      bestDistance = ship.Distance;
    }

    return best;
  }

  /// <summary>
  /// Look up one terrain ray by id.
  /// </summary>
  public static bool TryGetTerrainRay(this AiShipContext context, string id, out AiShipTerrainRay ray)
  {
    foreach (AiShipTerrainRay candidate in context.TerrainRays)
    {
      if (candidate != null && candidate.Id == id)
      {
        ray = candidate;
        return true;
      }
    }

    ray = default;
    return false;
  }
}
