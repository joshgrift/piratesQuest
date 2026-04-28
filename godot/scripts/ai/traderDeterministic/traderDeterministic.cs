namespace PiratesQuest.AI.traderDeterministic;

using Godot;
using PiratesQuest.AI;
using System;

/// <summary>
/// A simple deterministic trader:
/// pick a port, sail there, then pick another port.
/// 
/// Priorities:
/// 1. Avoid terrain
/// 2. Avoid non-trader ships
/// 3. Continue toward the chosen port
/// </summary>
public sealed class TraderDeterministicAiShipController : IAiShipController
{
  private const string CurrentPortIdKey = "trader.current_port_id";
  private const string IsStuckKey = "trader.is_stuck";
  private const string IsEscapingKey = "trader.is_escaping";
  private const string IsEscapeReversingKey = "trader.is_escape_reversing";
  private const string EscapeTurnDirectionKey = "trader.escape_turn_direction";

  private readonly TraderDeterministicAiShipControllerConfig _config;
  private readonly RandomNumberGenerator _rng = new();

  public TraderDeterministicAiShipController(TraderDeterministicAiShipControllerConfig config)
  {
    ArgumentNullException.ThrowIfNull(config);
    _config = config;
    _rng.Randomize();
  }

  public void SyncSceneMemory(
    AiShipMemory memory,
    bool isStuck,
    bool isEscaping,
    bool isEscapeReversing,
    float escapeTurnDirection)
  {
    memory.Set(IsStuckKey, isStuck);
    memory.Set(IsEscapingKey, isEscaping);
    memory.Set(IsEscapeReversingKey, isEscapeReversing);
    memory.Set(EscapeTurnDirectionKey, escapeTurnDirection);
  }

  public AiShipControlInput GetControl(AiShipContext context, AiShipMemory memory, double delta)
  {
    var input = new AiShipControlInput();
    bool isStuck = GetIsStuck(memory);
    bool isEscaping = GetIsEscaping(memory);
    bool isEscapeReversing = GetIsEscapeReversing(memory);
    float escapeTurnDirection = GetEscapeTurnDirection(memory);
    AiShipContact threatShip = context.FindNearestThreatShip();
    float obstacleTurnBias = AiNavigationHelpers.BuildObstacleTurnBias(context);
    bool sideTerrainNearby = AiNavigationHelpers.HasSideTerrainNearby(context);

    if (isEscaping)
    {
      input.Throttle = isEscapeReversing ? -1.0f : 0.65f;
      input.Turn = escapeTurnDirection;
      input.DebugState = isEscapeReversing ? "Escape Reverse" : "Escape Forward";
      return input;
    }

    if (AiNavigationHelpers.IsFrontBlocked(context))
    {
      input.Throttle = -0.65f;
      input.Turn = AiNavigationHelpers.PickSaferTurn(context);
      input.DebugState = "Avoid Terrain";
      return input;
    }

    if (isStuck)
    {
      input.Throttle = -0.95f;
      input.Turn = AiNavigationHelpers.PickSaferTurn(context);
      input.DebugState = "Recover Stuck";
      return input;
    }

    Port targetPort = PickOrRefreshTargetPort(context, memory);
    if (targetPort == null)
    {
      input.Throttle = 0.0f;
      input.Turn = 0.0f;
      input.DebugState = "No Port Found";
      return input;
    }

    if (threatShip != null)
    {
      Vector3 localThreatShipPosition = context.ShipBasis.Inverse() * (threatShip.Position - context.ShipPosition);
      Vector3 fleeLocal = -localThreatShipPosition;
      float fleeAngle = Mathf.Atan2(fleeLocal.X, -fleeLocal.Z);

      input.Throttle = threatShip.Distance < 45.0f ? 1.0f : 0.75f;
      input.Turn = Mathf.Clamp(fleeAngle / 0.8f, -1.0f, 1.0f);
      input.DebugState = "Avoid Ship";
      return input;
    }

    Vector3 localPort = context.ShipBasis.Inverse() * (targetPort.GlobalPosition - context.ShipPosition);
    float distanceToPort = context.ShipPosition.DistanceTo(targetPort.GlobalPosition);
    float targetAngle = Mathf.Atan2(localPort.X, -localPort.Z);
    float portTurn = Mathf.Clamp(targetAngle / 0.72f, -1.0f, 1.0f);

    input.Throttle = distanceToPort > _config.GoalArrivalDistance * 2.0f ? 0.82f : 0.35f;

    // If the bow is clear, keep moving and let side terrain act like a gentle
    // nudge instead of a full stop/reverse command. This helps traders slide
    // past coastlines instead of freezing in channels.
    float obstacleAssist = sideTerrainNearby ? obstacleTurnBias * 0.14f : 0.0f;
    input.Turn = Mathf.Clamp(portTurn + obstacleAssist, -1.0f, 1.0f);
    input.DebugState = $"Travel {targetPort.PortName}";
    return input;
  }

  private Port PickOrRefreshTargetPort(AiShipContext context, AiShipMemory memory)
  {
    if (context.Ports.Length == 0)
      return null;

    string currentPortId = memory.TryGet<string>(CurrentPortIdKey, out string storedPortId)
      ? storedPortId
      : string.Empty;

    Port currentPort = FindPortById(context, currentPortId);
    if (currentPort == null)
    {
      currentPort = PickRandomPort(context, null);
      memory.Set(CurrentPortIdKey, currentPort?.PortId ?? string.Empty);
      return currentPort;
    }

    float distanceToCurrentPort = context.ShipPosition.DistanceTo(currentPort.GlobalPosition);
    if (distanceToCurrentPort > _config.GoalArrivalDistance)
      return currentPort;

    Port nextPort = PickRandomPort(context, currentPort.PortId);
    memory.Set(CurrentPortIdKey, nextPort?.PortId ?? currentPort.PortId);
    return nextPort ?? currentPort;
  }

  public void OnRemoved(AiShipMemory memory, string reason)
  {
  }

  private static bool GetIsStuck(AiShipMemory memory)
  {
    return memory.TryGet(IsStuckKey, out bool value) && value;
  }

  private static bool GetIsEscaping(AiShipMemory memory)
  {
    return memory.TryGet(IsEscapingKey, out bool value) && value;
  }

  private static bool GetIsEscapeReversing(AiShipMemory memory)
  {
    return memory.TryGet(IsEscapeReversingKey, out bool value) && value;
  }

  private static float GetEscapeTurnDirection(AiShipMemory memory)
  {
    return memory.TryGet(EscapeTurnDirectionKey, out float value) ? value : 1.0f;
  }

  private static Port FindPortById(AiShipContext context, string portId)
  {
    if (string.IsNullOrWhiteSpace(portId))
      return null;

    foreach (Port port in context.Ports)
    {
      if (port?.PortId == portId)
        return port;
    }

    return null;
  }

  private Port PickRandomPort(AiShipContext context, string excludedPortId)
  {
    var candidates = new Godot.Collections.Array<Port>();
    foreach (Port port in context.Ports)
    {
      if (port == null)
        continue;
      if (!string.IsNullOrWhiteSpace(excludedPortId) && port.PortId == excludedPortId)
        continue;

      candidates.Add(port);
    }

    if (candidates.Count == 0)
      return FindPortById(context, excludedPortId);

    int index = _rng.RandiRange(0, candidates.Count - 1);
    return candidates[index];
  }
}
