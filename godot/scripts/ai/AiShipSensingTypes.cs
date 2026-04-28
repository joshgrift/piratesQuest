namespace PiratesQuest.AI;

using Godot;

/// <summary>
/// One nearby ship contact seen this frame.
/// </summary>
public sealed class AiShipContact
{
  public bool IsPlayer { get; init; }
  public bool IsAllied { get; init; }
  public bool IsThreat { get; init; }
  public float Distance { get; init; } = float.MaxValue;
  public Vector3 Position { get; init; }
}

/// <summary>
/// One terrain-only ray reading for this frame.
/// </summary>
public sealed class AiShipTerrainRay
{
  public string Id { get; init; } = string.Empty;
  public bool IsBlocked { get; init; }

  /// <summary>
  /// Distance from the ray origin to the collision point.
  /// If nothing was hit, this stays at <see cref="MaxDistance"/>.
  /// </summary>
  public float Distance { get; init; } = float.MaxValue;

  /// <summary>
  /// Full length of the configured ray.
  /// </summary>
  public float MaxDistance { get; init; } = float.MaxValue;
}

/// <summary>
/// Stable ids for the built-in AI terrain rays.
/// Add new ids here as new ray sensors are introduced.
/// </summary>
public static class AiShipRayIds
{
  public const string Forward = "forward";
  public const string ForwardLeft = "forward_left";
  public const string ForwardRight = "forward_right";
  public const string WideLeft = "wide_left";
  public const string WideRight = "wide_right";
}
