namespace PiratesQuest.AI;

using Godot;
using System;
using System.Collections.Generic;
using System.IO;
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

  private readonly Dictionary<string, int> _targetCountByArchetype = [];

  private readonly Dictionary<string, int> _nextSequenceByArchetype = new();
  private readonly RandomNumberGenerator _rng = new();

  private Play _play;
  private PackedScene _aiShipScene;
  private MultiplayerSpawner _aiShipSpawner;
  private MultiplayerSpawner _projectileSpawner;
  private MultiplayerSpawner _deadPlayerSpawner;
  private Timer _respawnTimer;
  private bool _debugAiShips;
  private bool _pythonAiSessionDisabled;
  private bool _isShuttingDown;
  private AiShipPythonWorker _pythonAiWorker;

  public AiShipPythonWorker PythonWorker => _pythonAiWorker;

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

    InitializePythonAiIfNeeded();

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

  public bool CanManuallySpawn(string archetypeId)
  {
    string normalizedArchetypeId = NormalizeArchetypeId(archetypeId);
    if (!AiShips.IsKnownId(normalizedArchetypeId))
      return false;

    return _pythonAiWorker != null && _pythonAiWorker.IsAvailable;
  }

  public bool TrySpawnManualShip(string archetypeId)
  {
    if (_play == null || !_play.Multiplayer.IsServer() || _aiShipSpawner == null)
      return false;

    string normalizedArchetypeId = NormalizeArchetypeId(archetypeId);
    if (!CanManuallySpawn(normalizedArchetypeId))
      return false;

    var (position, yawRadians) = PickEdgeSpawn();
    SpawnAiShip(normalizedArchetypeId, position, yawRadians);
    GD.Print($"Manually spawned {normalizedArchetypeId} AI ship.");
    return true;
  }

  public void Shutdown()
  {
    if (_isShuttingDown)
      return;

    _isShuttingDown = true;

    if (IsInstanceValid(_respawnTimer))
    {
      _respawnTimer.Timeout -= EnsurePopulation;
      _respawnTimer.Stop();
      _respawnTimer.QueueFree();
    }
    _respawnTimer = null;

    // Close AI ships while the worker is still alive so their controllers
    // can flush a final terminal transition for scene shutdown.
    DespawnAllAiShips("scene_exit");

    if (_pythonAiWorker != null)
    {
      _pythonAiWorker.Unavailable -= OnPythonAiWorkerUnavailable;
      _pythonAiWorker.Shutdown();
      _pythonAiWorker = null;
    }

    _targetCountByArchetype.Clear();
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

      string archetypeId = string.IsNullOrWhiteSpace(aiShip.ArchetypeId) ? AiShips.Default.Id : aiShip.ArchetypeId;
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
    var definition = AiShips.FromId(archetypeId);
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

  private void InitializePythonAiIfNeeded()
  {
    _targetCountByArchetype.Clear();

    if (!Configuration.PythonAiEnabled)
    {
      GD.Print("Python AI disabled; skipping AI ships.");
      return;
    }

    if (_pythonAiSessionDisabled)
      return;

    string scriptPath = Configuration.GetPythonAiScriptAbsolutePath();
    string rolloutPath = BuildRolloutFilePath();

    _pythonAiWorker = new AiShipPythonWorker(
      Configuration.PythonAiExecutable,
      scriptPath,
      rolloutPath);

    _pythonAiWorker.Unavailable += OnPythonAiWorkerUnavailable;

    if (!_pythonAiWorker.Start())
    {
      GD.PrintErr("Python AI worker never reached ready handshake. AI ships will be skipped for this session.");
      _pythonAiWorker.Unavailable -= OnPythonAiWorkerUnavailable;
      _pythonAiWorker.Shutdown();
      _pythonAiWorker = null;
      _pythonAiSessionDisabled = true;
      return;
    }

    foreach (var entry in AiShips.BuildSpawnTargetCounts(Configuration.PythonAiCount))
    {
      _targetCountByArchetype[entry.Key] = entry.Value;
    }

    GD.Print($"Python AI ready. Loaded {AiShips.All.Count} AI ship type(s).");
  }

  private string BuildRolloutFilePath()
  {
    string rolloutDirectory = ProjectSettings.GlobalizePath("user://ai_rollouts");
    Directory.CreateDirectory(rolloutDirectory);

    string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    return Path.Combine(rolloutDirectory, $"rollout_{timestamp}.jsonl");
  }

  private void OnPythonAiWorkerUnavailable(string reason)
  {
    if (_isShuttingDown)
      return;

    GD.PrintErr($"Python AI worker became unavailable: {reason}");
    Callable.From(HandlePythonWorkerUnavailable).CallDeferred();
  }

  private void HandlePythonWorkerUnavailable()
  {
    if (_isShuttingDown || _pythonAiSessionDisabled)
      return;

    _pythonAiSessionDisabled = true;
    _targetCountByArchetype.Clear();

    if (_pythonAiWorker != null)
    {
      _pythonAiWorker.Unavailable -= OnPythonAiWorkerUnavailable;
      _pythonAiWorker.Shutdown();
      _pythonAiWorker = null;
    }

    DespawnAllAiShips("worker_unavailable");
  }

  private void DespawnAllAiShips(string reason)
  {
    Node aiShipRoot = _aiShipSpawner?.GetParent();
    if (aiShipRoot == null)
      return;

    foreach (Node child in aiShipRoot.GetChildren())
    {
      if (child is not AiShip aiShip)
        continue;

      aiShip.ForceRemoval(reason);
    }
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

  private static string NormalizeArchetypeId(string archetypeId)
  {
    return (archetypeId ?? string.Empty).Trim().ToLowerInvariant();
  }
}
