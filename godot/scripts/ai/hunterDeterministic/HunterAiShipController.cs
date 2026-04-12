namespace PiratesQuest.AI.hunterDeterministic;

using Godot;
using PiratesQuest.AI;

/// <summary>
/// First AI brain: patrol open water, chase nearby players, and try to line up
/// broadsides instead of simply ramming straight at the target.
/// </summary>
public sealed class HunterAiShipController : IAiShipController
{
  public AiShipControlInput GetControl(AiShipContext context, double delta)
  {
    var input = new AiShipControlInput();

    // Once the ship commits to an escape maneuver, keep that decision stable
    // for a short time. This avoids the left/right wiggle that happens when
    // obstacle checks flip every frame near shore.
    if (context.IsEscaping)
    {
      input.Throttle = context.IsEscapeReversing ? -1.0f : 0.55f;
      input.Turn = context.EscapeTurnDirection;
      input.DebugState = context.IsEscapeReversing ? "Escape Reverse" : "Escape Forward";
      return input;
    }

    // Terrain avoidance gets top priority.
    // If the ship sees land dead ahead, surviving matters more than style.
    if (context.FrontBlocked)
    {
      input.Throttle = context.LeftBlocked && context.RightBlocked ? -0.8f : 0.25f;
      input.Turn = PickSaferTurn(context);
      input.DebugState = "Avoid Shore";
      return input;
    }

    // If we're already rubbing against terrain, reverse and pivot away.
    if (context.IsStuck)
    {
      input.Throttle = -0.9f;
      input.Turn = PickSaferTurn(context);
      input.DebugState = "Recover Stuck";
      return input;
    }

    float targetAngle = Mathf.Atan2(context.LocalGoalPosition.X, -context.LocalGoalPosition.Z);
    float distance = context.DistanceToGoal;

    if (!context.HasTargetPlayer)
    {
      input.Throttle = distance > 12.0f ? 0.75f : 0.2f;
      input.Turn = Mathf.Clamp(targetAngle / 0.8f, -1.0f, 1.0f);
      input.DebugState = "Patrol";
      return input;
    }

    // When attacking, try to keep the player off one side of the hull so the
    // cannons have a clear broadside instead of staring at the bow.
    float desiredBroadsideAngle = context.LocalGoalPosition.X >= 0.0f
      ? Mathf.DegToRad(78.0f)
      : Mathf.DegToRad(-78.0f);

    bool inFireRange = distance <= context.FireRange;
    float steeringAngle = inFireRange
      ? NormalizeAngle(targetAngle - desiredBroadsideAngle)
      : targetAngle;

    input.Turn = Mathf.Clamp(steeringAngle / 0.85f, -1.0f, 1.0f);

    if (distance > context.PreferredCombatRange + 12.0f)
    {
      input.Throttle = 1.0f;
    }
    else if (distance < context.PreferredCombatRange * 0.65f)
    {
      input.Throttle = 0.2f;
    }
    else
    {
      input.Throttle = 0.65f;
    }

    if (inFireRange && Mathf.Abs(steeringAngle) < 0.22f)
    {
      input.FireRight = context.LocalGoalPosition.X > 0.0f;
      input.FireLeft = context.LocalGoalPosition.X < 0.0f;
      input.DebugState = input.FireRight ? "Fire Starboard" : "Fire Port";
      return input;
    }

    input.DebugState = inFireRange ? "Broadside Setup" : "Chase";
    return input;
  }

  private static float PickSaferTurn(AiShipContext context)
  {
    if (context.LeftBlocked && !context.RightBlocked) return 1.0f;
    if (context.RightBlocked && !context.LeftBlocked) return -1.0f;

    // If both sides look similar, keep the choice stable by preferring starboard.
    return 1.0f;
  }

  private static float NormalizeAngle(float angle)
  {
    return Mathf.Atan2(Mathf.Sin(angle), Mathf.Cos(angle));
  }
}
