namespace PiratesQuest.AI;

using Godot;
using System.Collections.Generic;
using GodotDictionary = Godot.Collections.Dictionary;

/// <summary>
/// Shared tuning values for where AI ships are allowed to spawn and patrol.
/// 
/// Keeping these in one place helps the play scene and the AI ship scene agree
/// on what "the playable map" means.
/// </summary>
public static class AiShipWorldSettings
{
  public const float MapHalfExtent = 1100.0f;
  public const float SpawnInset = 85.0f;
  public const float SpawnPaddingFromCorners = 180.0f;
  public const float ShipDiscoveryRange = 160.0f;

  // Patrol points stay a little inside the edge so ships spend more time in
  // useful water and less time scraping the border.
  public const float PatrolInset = 180.0f;
}

/// <summary>
/// Owns AI ship population management for the play scene.
/// 
/// Play.cs still owns the rest of the world, but this class keeps the AI ship
/// refill logic in one focused place.
/// </summary>
public partial class AiShipManager : RefCounted
{
  private const double RespawnCheckIntervalSeconds = 300.0;
  private const float SpawnHeight = 2.0f;

  private readonly Dictionary<string, int> _targetCountByArchetype = new()
  {
    { "raider", 5 },
    { "trader", 5 },
  };

  private readonly Dictionary<string, int> _nextSequenceByArchetype = new();
  private readonly RandomNumberGenerator _rng = new();

  private Play _play;
  private PackedScene _aiShipScene;
  private MultiplayerSpawner _aiShipSpawner;
  private MultiplayerSpawner _projectileSpawner;
  private MultiplayerSpawner _deadPlayerSpawner;
  private Timer _respawnTimer;
  private bool _debugAiShips;

  public void Initialize(
    Play play,
    PackedScene aiShipScene,
    MultiplayerSpawner aiShipSpawner,
    MultiplayerSpawner projectileSpawner,
    MultiplayerSpawner deadPlayerSpawner,
    bool debugAiShips)
  {
    _play = play;
    _aiShipScene = aiShipScene;
    _aiShipSpawner = aiShipSpawner;
    _projectileSpawner = projectileSpawner;
    _deadPlayerSpawner = deadPlayerSpawner;
    _debugAiShips = debugAiShips;

    _rng.Randomize();
    _aiShipSpawner.SpawnFunction = new Callable(this, MethodName.SpawnHandler);
  }

  public void StartRespawnLoop()
  {
    if (_play == null || !_play.Multiplayer.IsServer())
      return;

    _respawnTimer = new Timer
    {
      WaitTime = RespawnCheckIntervalSeconds,
      Autostart = true
    };

    _respawnTimer.Timeout += EnsurePopulation;
    _play.AddChild(_respawnTimer);

    EnsurePopulation();
    GD.Print($"AI ship refill timer started (every {RespawnCheckIntervalSeconds:0.#}s)");
  }

  public AiShip SpawnHandler(Variant data)
  {
    var dict = data.AsGodotDictionary();
    var aiShip = _aiShipScene.Instantiate<AiShip>();

    aiShip.ProjectileSpawner = _projectileSpawner;
    aiShip.DeadPlayerSpawner = _deadPlayerSpawner;
    aiShip.Manager = this;
    aiShip.SetMultiplayerAuthority(1);
    aiShip.Synchronizer?.SetMultiplayerAuthority(1);
    aiShip.ConfigureFromSpawnData(dict);

    return aiShip;
  }

  public void RequestImmediateRefill()
  {
    if (_play == null || !_play.Multiplayer.IsServer())
      return;

    Callable.From(EnsurePopulation).CallDeferred();
  }

  private void EnsurePopulation()
  {
    if (_play == null || !_play.Multiplayer.IsServer() || _aiShipSpawner == null)
      return;

    var aiShipRoot = _aiShipSpawner.GetParent();
    if (aiShipRoot == null)
      return;

    var activeCountByArchetype = new Dictionary<string, int>();
    foreach (Node child in aiShipRoot.GetChildren())
    {
      if (child is not AiShip aiShip)
        continue;

      string archetypeId = string.IsNullOrWhiteSpace(aiShip.ArchetypeId) ? "raider" : aiShip.ArchetypeId;
      activeCountByArchetype[archetypeId] = activeCountByArchetype.GetValueOrDefault(archetypeId) + 1;
    }

    foreach (var target in _targetCountByArchetype)
    {
      int livingCount = activeCountByArchetype.GetValueOrDefault(target.Key);
      int missingCount = target.Value - livingCount;
      if (missingCount <= 0)
        continue;

      for (int i = 0; i < missingCount; i++)
      {
        var (position, yawRadians) = PickEdgeSpawn();
        SpawnAiShip(target.Key, position, yawRadians);
      }

      GD.Print($"Spawned {missingCount} {target.Key} AI ship(s). Active: {livingCount + missingCount}/{target.Value}");
    }
  }

  private void SpawnAiShip(string archetypeId, Vector3 position, float yawRadians)
  {
    var definition = AiShipDefinition.FromId(archetypeId);
    int sequence = _nextSequenceByArchetype.GetValueOrDefault(archetypeId, 1);
    _nextSequenceByArchetype[archetypeId] = sequence + 1;

    var spawnData = new GodotDictionary
    {
      ["name"] = $"ai_ship_{archetypeId}_{sequence}",
      ["definitionId"] = definition.Id,
      ["displayName"] = definition.DisplayName,
      ["debug"] = _debugAiShips,
      ["position"] = position,
      ["rotation"] = new Vector3(0.0f, yawRadians, 0.0f)
    };

    _aiShipSpawner.Spawn(spawnData);
  }

  private (Vector3 Position, float YawRadians) PickEdgeSpawn()
  {
    float halfExtent = AiShipWorldSettings.MapHalfExtent;
    float edgeCoordinate = halfExtent - AiShipWorldSettings.SpawnInset;
    float spanMin = -halfExtent + AiShipWorldSettings.SpawnPaddingFromCorners;
    float spanMax = halfExtent - AiShipWorldSettings.SpawnPaddingFromCorners;
    float lane = _rng.RandfRange(spanMin, spanMax);
    int side = _rng.RandiRange(0, 3);

    Vector3 position = side switch
    {
      0 => new Vector3(-edgeCoordinate, SpawnHeight, lane),
      1 => new Vector3(edgeCoordinate, SpawnHeight, lane),
      2 => new Vector3(lane, SpawnHeight, -edgeCoordinate),
      _ => new Vector3(lane, SpawnHeight, edgeCoordinate),
    };

    Vector3 toCenter = (Vector3.Zero - position).Normalized();
    float yawRadians = Mathf.Atan2(-toCenter.X, -toCenter.Z);
    return (position, yawRadians);
  }
}
