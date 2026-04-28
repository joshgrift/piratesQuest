namespace PiratesQuest.AI;

using Godot;

/// <summary>
/// Small shared math helpers for AI steering decisions.
/// 
/// Keeping them here avoids each AI re-implementing the same terrain-ray math.
/// </summary>
public static class AiNavigationHelpers
{
  public static float NormalizeAngle(float angle)
  {
    return Mathf.Atan2(Mathf.Sin(angle), Mathf.Cos(angle));
  }

  public static bool IsFrontBlocked(AiShipContext context)
  {
    return IsRayBlocked(context, AiShipRayIds.Forward);
  }

  public static bool IsLeftBlocked(AiShipContext context)
  {
    return IsRayBlocked(context, AiShipRayIds.ForwardLeft) ||
      IsRayBlocked(context, AiShipRayIds.WideLeft);
  }

  public static bool IsRightBlocked(AiShipContext context)
  {
    return IsRayBlocked(context, AiShipRayIds.ForwardRight) ||
      IsRayBlocked(context, AiShipRayIds.WideRight);
  }

  public static float GetRayDistance(AiShipContext context, string rayId, float fallback = float.MaxValue)
  {
    return context.TryGetTerrainRay(rayId, out AiShipTerrainRay ray) ? ray.Distance : fallback;
  }

  public static bool HasSideTerrainNearby(AiShipContext context)
  {
    return IsLeftBlocked(context) ||
      IsRightBlocked(context) ||
      GetLeftObstacleStrength(context) > 0.0f ||
      GetRightObstacleStrength(context) > 0.0f;
  }

  public static bool HasHighTerrainPressure(AiShipContext context)
  {
    return GetLeftObstacleStrength(context) >= 0.75f ||
      GetRightObstacleStrength(context) >= 0.75f;
  }

  public static bool HasSevereTerrainPressure(AiShipContext context)
  {
    return GetLeftObstacleStrength(context) >= 1.5f ||
      GetRightObstacleStrength(context) >= 1.5f;
  }

  public static float GetLeftObstacleStrength(AiShipContext context)
  {
    float strength = 0.0f;
    if (IsRayBlocked(context, AiShipRayIds.ForwardLeft))
      strength += 1.0f;
    if (IsRayBlocked(context, AiShipRayIds.WideLeft))
      strength += 0.75f;
    return strength;
  }

  public static float GetRightObstacleStrength(AiShipContext context)
  {
    float strength = 0.0f;
    if (IsRayBlocked(context, AiShipRayIds.ForwardRight))
      strength += 1.0f;
    if (IsRayBlocked(context, AiShipRayIds.WideRight))
      strength += 0.75f;
    return strength;
  }

  public static float PickSaferTurn(AiShipContext context)
  {
    float leftStrength = GetLeftObstacleStrength(context);
    float rightStrength = GetRightObstacleStrength(context);

    if (leftStrength < rightStrength)
      return -1.0f;

    if (rightStrength < leftStrength)
      return 1.0f;

    if (IsLeftBlocked(context) && !IsRightBlocked(context)) return 1.0f;
    if (IsRightBlocked(context) && !IsLeftBlocked(context)) return -1.0f;

    // A stable default keeps ships from vibrating when both sides look equal.
    return 1.0f;
  }

  public static float BuildObstacleTurnBias(AiShipContext context)
  {
    // Positive means "turn right", negative means "turn left".
    // More pressure on one side pushes the ship away from that shore.
    float pressureDelta = GetLeftObstacleStrength(context) - GetRightObstacleStrength(context);

    if (Mathf.Abs(pressureDelta) > 0.01f)
      return Mathf.Clamp(pressureDelta, -0.45f, 0.45f);

    if (IsLeftBlocked(context) && !IsRightBlocked(context))
      return 0.35f;

    if (IsRightBlocked(context) && !IsLeftBlocked(context))
      return -0.35f;

    return 0.0f;
  }

  private static bool IsRayBlocked(AiShipContext context, string rayId)
  {
    return context.TryGetTerrainRay(rayId, out AiShipTerrainRay ray) && ray.IsBlocked;
  }
}
