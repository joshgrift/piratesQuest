namespace PiratesQuest.AI;

using Godot;
using System;
using System.Globalization;
using System.IO;
using System.Text;

/// <summary>
/// Writes one CSV row per authoritative raider AI decision.
///
/// This is intended as simple local training-data capture for later model work.
/// We log both the sensed state and the chosen control output so a future model
/// can learn "given this state, the raider moved like this".
/// </summary>
public sealed class RaiderTrainingCsvLogger : IDisposable
{
  private const long MaxFileBytes = 1024L * 1024L * 1024L;

  private readonly StreamWriter _writer;
  private bool _isDisposed;
  private bool _hasReachedSizeLimit;

  public string OutputPath { get; }

  public RaiderTrainingCsvLogger(string outputPath)
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

    AiShipContact targetShip = context.FindNearestHostileShip();
    Vector3 localTargetShip = targetShip != null
      ? context.ShipBasis.Inverse() * (targetShip.Position - context.ShipPosition)
      : Vector3.Zero;

    Vector3 patrolPoint = memory.GetOrDefault("hunter.patrol_point", Vector3.Zero);
    bool hasPatrolPoint = patrolPoint != Vector3.Zero;
    float patrolDistance = hasPatrolPoint
      ? context.ShipPosition.DistanceTo(patrolPoint)
      : 0.0f;

    bool isStuck = memory.GetOrDefault("hunter.is_stuck", false);
    bool isEscaping = memory.GetOrDefault("hunter.is_escaping", false);
    bool isEscapeReversing = memory.GetOrDefault("hunter.is_escape_reversing", false);
    float escapeTurnDirection = memory.GetOrDefault("hunter.escape_turn_direction", 0.0f);

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
      targetShip != null ? "1" : "0",
      targetShip?.IsPlayer == true ? "1" : "0",
      FormatFloat(targetShip?.Distance ?? 0.0f),
      FormatFloat(localTargetShip.X),
      FormatFloat(localTargetShip.Z),
      hasPatrolPoint ? "1" : "0",
      FormatFloat(patrolPoint.X),
      FormatFloat(patrolPoint.Z),
      FormatFloat(patrolDistance),
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
      "has_target_ship",
      "target_is_player",
      "target_distance",
      "target_local_x",
      "target_local_z",
      "has_patrol_point",
      "patrol_point_x",
      "patrol_point_z",
      "patrol_distance",
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
      GD.Print($"Raider training CSV hit the 1 GB safety limit. Logging stopped for {OutputPath}");
      _writer.Flush();
      return;
    }

    _writer.WriteLine(line);
    _writer.Flush();
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
