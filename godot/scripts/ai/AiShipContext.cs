namespace PiratesQuest.AI;

using Godot;

/// <summary>
/// Read-only snapshot of the world as seen by an AI ship this frame.
/// 
/// We only include things AI ships actually care about right now:
/// nearby players, nearby ports, the current goal, and obstacle sensing.
/// </summary>
public sealed class AiShipContext
{
  public Vector3 ShipPosition { get; init; }
  public float CurrentSpeed { get; init; }
  public Vector3 GoalPosition { get; init; }
  public Vector3 LocalGoalPosition { get; init; }
  public bool HasTargetPlayer { get; init; }
  public float DistanceToGoal { get; init; }
  public float FireRange { get; init; }
  public float PreferredCombatRange { get; init; }
  public bool FrontBlocked { get; init; }
  public bool LeftBlocked { get; init; }
  public bool RightBlocked { get; init; }
  public bool IsStuck { get; init; }
  public bool IsEscaping { get; init; }
  public bool IsEscapeReversing { get; init; }
  public float EscapeTurnDirection { get; init; }
  public Port NearestPort { get; init; }
}
