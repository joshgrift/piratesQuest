namespace PiratesQuest.AI;

using Godot;
using System;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Writes one CSV row per authoritative trader AI decision.
///
/// Traders care about their chosen destination port more than combat, so this
/// logger captures route state plus the movement command the controller picked.
/// That makes the resulting dataset a better fit for later imitation learning.
/// </summary>
public sealed class TraderTrainingCsvLogger : IDisposable
{
  private const long MaxFileBytes = 1024L * 1024L * 1024L;
  private const string CurrentPortIdKey = "trader.current_port_id";
  private const string IsStuckKey = "trader.is_stuck";
  private const string IsEscapingKey = "trader.is_escaping";
  private const string IsEscapeReversingKey = "trader.is_escape_reversing";
  private const string EscapeTurnDirectionKey = "trader.escape_turn_direction";

  private readonly StreamWriter _writer;
  private bool _isDisposed;
  private bool _hasReachedSizeLimit;

  public string OutputPath { get; }

  public TraderTrainingCsvLogger(string outputPath)
  {
    OutputPath = outputPath ?? throw new ArgumentNullException(nameof(outputPath));

    Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? ".");
    _writer = new StreamWriter(OutputPath, append: false, Encoding.UTF8);
    WriteHeader();
  }

  public void LogSample(
    AiShip ship,
    AiShipContext context,
    AiShipMemory memory,
    AiShipControlInput control)
  {
    if (_isDisposed || ship == null || context == null || control == null)
      return;

    AiShipContact threatShip = context.FindNearestThreatShip();
    Vector3 localThreatShip = threatShip != null
      ? context.ShipBasis.Inverse() * (threatShip.Position - context.ShipPosition)
      : Vector3.Zero;

    Port targetPort = ResolveTargetPort(context, memory);
    Vector3 localTargetPort = targetPort != null
      ? context.ShipBasis.Inverse() * (targetPort.GlobalPosition - context.ShipPosition)
      : Vector3.Zero;
    float targetPortDistance = targetPort != null
      ? context.ShipPosition.DistanceTo(targetPort.GlobalPosition)
      : 0.0f;

    bool isStuck = memory.GetOrDefault(IsStuckKey, false);
    bool isEscaping = memory.GetOrDefault(IsEscapingKey, false);
    bool isEscapeReversing = memory.GetOrDefault(IsEscapeReversingKey, false);
    float escapeTurnDirection = memory.GetOrDefault(EscapeTurnDirectionKey, 0.0f);
    float yawRadians = ship.Rotation.Y;

    string[] columns =
    [
      DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
      ship.Name,
      ship.ArchetypeId,
      FormatFloat(context.ShipPosition.X),
      FormatFloat(context.ShipPosition.Z),
      FormatFloat(yawRadians),
      FormatFloat(context.CurrentSpeed),
      targetPort != null ? "1" : "0",
      EscapeCsv(targetPort?.PortId ?? string.Empty),
      FormatFloat(targetPort?.GlobalPosition.X ?? 0.0f),
      FormatFloat(targetPort?.GlobalPosition.Z ?? 0.0f),
      FormatFloat(targetPortDistance),
      FormatFloat(localTargetPort.X),
      FormatFloat(localTargetPort.Z),
      threatShip != null ? "1" : "0",
      threatShip?.IsPlayer == true ? "1" : "0",
      FormatFloat(threatShip?.Distance ?? 0.0f),
      FormatFloat(localThreatShip.X),
      FormatFloat(localThreatShip.Z),
      AiNavigationHelpers.IsFrontBlocked(context) ? "1" : "0",
      AiNavigationHelpers.IsLeftBlocked(context) ? "1" : "0",
      AiNavigationHelpers.IsRightBlocked(context) ? "1" : "0",
      FormatFloat(GetRayDistanceFraction(context, AiShipRayIds.Forward)),
      FormatFloat(GetRayDistanceFraction(context, AiShipRayIds.ForwardLeft)),
      FormatFloat(GetRayDistanceFraction(context, AiShipRayIds.ForwardRight)),
      FormatFloat(GetRayDistanceFraction(context, AiShipRayIds.WideLeft)),
      FormatFloat(GetRayDistanceFraction(context, AiShipRayIds.WideRight)),
      isStuck ? "1" : "0",
      isEscaping ? "1" : "0",
      isEscapeReversing ? "1" : "0",
      FormatFloat(escapeTurnDirection),
      FormatFloat(control.Throttle),
      FormatFloat(control.Turn),
      control.FireLeft ? "1" : "0",
      control.FireRight ? "1" : "0",
      EscapeCsv(control.DebugState ?? string.Empty)
    ];

    WriteLineWithCap(string.Join(",", columns));
  }

  public void Dispose()
  {
    if (_isDisposed)
      return;

    _isDisposed = true;
    _writer.Dispose();
  }

  private void WriteHeader()
  {
    WriteLineWithCap(string.Join(",", [
      "timestamp_utc",
      "ship_name",
      "archetype_id",
      "ship_x",
      "ship_z",
      "yaw_radians",
      "speed",
      "has_target_port",
      "target_port_id",
      "target_port_x",
      "target_port_z",
      "target_port_distance",
      "target_port_local_x",
      "target_port_local_z",
      "has_threat_ship",
      "threat_is_player",
      "threat_distance",
      "threat_local_x",
      "threat_local_z",
      "front_blocked",
      "left_blocked",
      "right_blocked",
      "forward_distance_fraction",
      "forward_left_distance_fraction",
      "forward_right_distance_fraction",
      "wide_left_distance_fraction",
      "wide_right_distance_fraction",
      "is_stuck",
      "is_escaping",
      "is_escape_reversing",
      "escape_turn_direction",
      "throttle",
      "turn",
      "fire_left",
      "fire_right",
      "debug_state"
    ]));
  }

  private void WriteLineWithCap(string line)
  {
    if (_hasReachedSizeLimit)
      return;

    long currentSize = _writer.BaseStream.Position;
    int lineBytes = _writer.Encoding.GetByteCount(line) + _writer.Encoding.GetByteCount(_writer.NewLine);
    if (currentSize + lineBytes > MaxFileBytes)
    {
      _hasReachedSizeLimit = true;
      GD.Print($"Trader training CSV hit the 1 GB safety limit. Logging stopped for {OutputPath}");
      _writer.Flush();
      return;
    }

    _writer.WriteLine(line);
    _writer.Flush();
  }

  private static Port ResolveTargetPort(AiShipContext context, AiShipMemory memory)
  {
    string targetPortId = memory.GetOrDefault(CurrentPortIdKey, string.Empty);
    if (string.IsNullOrWhiteSpace(targetPortId))
      return null;

    foreach (Port port in context.Ports)
    {
      if (port?.PortId == targetPortId)
        return port;
    }

    return null;
  }

  private static string FormatFloat(float value)
  {
    return value.ToString("0.######", CultureInfo.InvariantCulture);
  }

  private static float GetRayDistanceFraction(AiShipContext context, string rayId)
  {
    if (!context.TryGetTerrainRay(rayId, out AiShipTerrainRay ray))
      return 1.0f;

    if (ray.MaxDistance <= 0.001f)
      return 1.0f;

    return Mathf.Clamp(ray.Distance / ray.MaxDistance, 0.0f, 1.0f);
  }

  private static string EscapeCsv(string value)
  {
    string escaped = value.Replace("\"", "\"\"");
    return $"\"{escaped}\"";
  }
}
