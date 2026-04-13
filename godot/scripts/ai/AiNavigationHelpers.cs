namespace PiratesQuest.AI;

using Godot;

/// <summary>
/// Small shared math helpers for AI steering decisions.
/// 
/// Keeping them here avoids each AI re-implementing the same obstacle math.
/// </summary>
public static class AiNavigationHelpers
{
  public static float NormalizeAngle(float angle)
  {
    return Mathf.Atan2(Mathf.Sin(angle), Mathf.Cos(angle));
  }

  public static bool HasSideTerrainNearby(AiShipContext context)
  {
    return context.LeftBlocked ||
      context.RightBlocked ||
      context.LeftObstacleStrength > 0.0f ||
      context.RightObstacleStrength > 0.0f;
  }

  public static bool HasHighTerrainPressure(AiShipContext context)
  {
    return context.LeftObstacleStrength >= 0.75f ||
      context.RightObstacleStrength >= 0.75f;
  }

  public static bool HasSevereTerrainPressure(AiShipContext context)
  {
    return context.LeftObstacleStrength >= 1.5f ||
      context.RightObstacleStrength >= 1.5f;
  }

  public static float PickSaferTurn(AiShipContext context)
  {
    if (context.LeftObstacleStrength < context.RightObstacleStrength)
      return -1.0f;

    if (context.RightObstacleStrength < context.LeftObstacleStrength)
      return 1.0f;

    if (context.LeftBlocked && !context.RightBlocked) return 1.0f;
    if (context.RightBlocked && !context.LeftBlocked) return -1.0f;

    // A stable default keeps ships from vibrating when both sides look equal.
    return 1.0f;
  }

  public static float BuildObstacleTurnBias(AiShipContext context)
  {
    // Positive means "turn right", negative means "turn left".
    // More pressure on one side pushes the ship away from that shore.
    float pressureDelta = context.LeftObstacleStrength - context.RightObstacleStrength;

    if (Mathf.Abs(pressureDelta) > 0.01f)
      return Mathf.Clamp(pressureDelta, -0.45f, 0.45f);

    if (context.LeftBlocked && !context.RightBlocked)
      return 0.35f;

    if (context.RightBlocked && !context.LeftBlocked)
      return -0.35f;

    return 0.0f;
  }
}
