namespace PiratesQuest.AI.traderDeterministic;

using Godot;
using PiratesQuest.AI;

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
  private readonly RandomNumberGenerator _rng = new();
  private string _currentPortId = string.Empty;

  public TraderDeterministicAiShipController()
  {
    _rng.Randomize();
  }

  public AiShipControlInput GetControl(AiShipContext context, double delta)
  {
    var input = new AiShipControlInput();
    float obstacleTurnBias = AiNavigationHelpers.BuildObstacleTurnBias(context);
    bool sideTerrainNearby = AiNavigationHelpers.HasSideTerrainNearby(context);

    if (context.IsEscaping)
    {
      input.Throttle = context.IsEscapeReversing ? -1.0f : 0.65f;
      input.Turn = context.EscapeTurnDirection;
      input.DebugState = context.IsEscapeReversing ? "Escape Reverse" : "Escape Forward";
      return input;
    }

    if (context.FrontBlocked)
    {
      input.Throttle = -0.65f;
      input.Turn = AiNavigationHelpers.PickSaferTurn(context);
      input.DebugState = "Avoid Terrain";
      return input;
    }

    if (context.IsStuck)
    {
      input.Throttle = -0.95f;
      input.Turn = AiNavigationHelpers.PickSaferTurn(context);
      input.DebugState = "Recover Stuck";
      return input;
    }

    Port targetPort = PickOrRefreshTargetPort(context);
    if (targetPort == null)
    {
      input.Throttle = 0.0f;
      input.Turn = 0.0f;
      input.DebugState = "No Port Found";
      return input;
    }

    if (context.HasNearbyThreatShip)
    {
      Vector3 fleeLocal = -context.LocalNearbyThreatShipPosition;
      float fleeAngle = Mathf.Atan2(fleeLocal.X, -fleeLocal.Z);

      input.Throttle = context.DistanceToNearbyThreatShip < 45.0f ? 1.0f : 0.75f;
      input.Turn = Mathf.Clamp(fleeAngle / 0.8f, -1.0f, 1.0f);
      input.DebugState = "Avoid Ship";
      return input;
    }

    Vector3 localPort = context.ShipBasis.Inverse() * (targetPort.GlobalPosition - context.ShipPosition);
    float distanceToPort = context.ShipPosition.DistanceTo(targetPort.GlobalPosition);
    float targetAngle = Mathf.Atan2(localPort.X, -localPort.Z);
    float portTurn = Mathf.Clamp(targetAngle / 0.72f, -1.0f, 1.0f);

    input.Throttle = distanceToPort > context.GoalArrivalDistance * 2.0f ? 0.82f : 0.35f;

    // If the bow is clear, keep moving and let side terrain act like a gentle
    // nudge instead of a full stop/reverse command. This helps traders slide
    // past coastlines instead of freezing in channels.
    float obstacleAssist = sideTerrainNearby ? obstacleTurnBias * 0.14f : 0.0f;
    input.Turn = Mathf.Clamp(portTurn + obstacleAssist, -1.0f, 1.0f);
    input.DebugState = $"Travel {targetPort.PortName}";
    return input;
  }

  private Port PickOrRefreshTargetPort(AiShipContext context)
  {
    if (context.Ports.Length == 0)
      return null;

    Port currentPort = FindPortById(context, _currentPortId);
    if (currentPort == null)
    {
      currentPort = PickRandomPort(context, null);
      _currentPortId = currentPort?.PortId ?? string.Empty;
      return currentPort;
    }

    float distanceToCurrentPort = context.ShipPosition.DistanceTo(currentPort.GlobalPosition);
    if (distanceToCurrentPort > context.GoalArrivalDistance)
      return currentPort;

    Port nextPort = PickRandomPort(context, currentPort.PortId);
    _currentPortId = nextPort?.PortId ?? currentPort.PortId;
    return nextPort ?? currentPort;
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
