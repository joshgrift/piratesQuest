namespace PiratesQuest.AI;

using Godot;
using PiratesQuest.AI.pythonNavigation;
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

  private readonly Dictionary<string, int> _targetCountByArchetype = new()
  {
    { "raider", 2 },
    { "trader", 2 },
    { "neural_patrol", 10 }
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
  private bool _pythonAiSessionDisabled;
  private bool _isShuttingDown;
  private PythonAiWorkerClient _pythonAiWorker;
  private RaiderTrainingCsvLogger _raiderTrainingLogger;
  private TraderTrainingCsvLogger _traderTrainingLogger;

  public AiShipControllerServices ControllerServices { get; private set; } = AiShipControllerServices.Empty;

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

    InitializeRaiderTrainingLoggers();
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

    // Close neural ships while the worker is still alive so their controllers
    // can flush a final terminal transition for scene shutdown.
    DespawnNeuralShips("scene_exit");

    if (_pythonAiWorker != null)
    {
      _pythonAiWorker.Unavailable -= OnPythonAiWorkerUnavailable;
      _pythonAiWorker.Shutdown();
      _pythonAiWorker = null;
    }

    _raiderTrainingLogger?.Dispose();
    _raiderTrainingLogger = null;
    _traderTrainingLogger?.Dispose();
    _traderTrainingLogger = null;

    ControllerServices = AiShipControllerServices.Empty;
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

  private void InitializePythonAiIfNeeded()
  {
    _targetCountByArchetype["neural_patrol"] = 1;
    RefreshControllerServices();

    if (!Configuration.PythonAiEnabled || Configuration.PythonAiCount <= 0)
    {
      GD.Print("Python AI disabled; skipping neural patrol archetype.");
      _targetCountByArchetype.Remove("neural_patrol");
      return;
    }

    if (_pythonAiSessionDisabled)
      return;

    string scriptPath = Configuration.GetPythonAiScriptAbsolutePath();
    string rolloutPath = BuildRolloutFilePath();

    _pythonAiWorker = new PythonAiWorkerClient(
      Configuration.PythonAiExecutable,
      scriptPath,
      rolloutPath);

    _pythonAiWorker.Unavailable += OnPythonAiWorkerUnavailable;

    if (!_pythonAiWorker.Start())
    {
      GD.PrintErr("Python AI worker never reached ready handshake. Neural patrol ships will be skipped for this session.");
      _pythonAiWorker.Unavailable -= OnPythonAiWorkerUnavailable;
      _pythonAiWorker.Shutdown();
      _pythonAiWorker = null;
      _pythonAiSessionDisabled = true;
      RefreshControllerServices();
      return;
    }

    _targetCountByArchetype["neural_patrol"] = Configuration.PythonAiCount;
    RefreshControllerServices();
    GD.Print($"Python AI ready. Target neural patrol ships: {Configuration.PythonAiCount}");
  }

  public void LogRaiderTrainingSample(
    AiShip ship,
    AiShipContext context,
    AiShipMemory memory,
    AiShipControlInput control)
  {
    _raiderTrainingLogger?.LogSample(ship, context, memory, control);
  }

  public void LogTraderTrainingSample(
    AiShip ship,
    AiShipContext context,
    AiShipMemory memory,
    AiShipControlInput control)
  {
    _traderTrainingLogger?.LogSample(ship, context, memory, control);
  }

  private void InitializeRaiderTrainingLoggers()
  {
    if (_raiderTrainingLogger != null || _traderTrainingLogger != null)
      return;

    string outputDirectory = ProjectSettings.GlobalizePath("user://ai_training");
    Directory.CreateDirectory(outputDirectory);

    string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    string raiderOutputPath = Path.Combine(outputDirectory, $"raider_movements_{timestamp}.csv");
    string traderOutputPath = Path.Combine(outputDirectory, $"trader_movements_{timestamp}.csv");

    _raiderTrainingLogger = new RaiderTrainingCsvLogger(raiderOutputPath);
    _traderTrainingLogger = new TraderTrainingCsvLogger(traderOutputPath);
    RefreshControllerServices();
    GD.Print($"Raider training CSV logging to {raiderOutputPath}");
    GD.Print($"Trader training CSV logging to {traderOutputPath}");
  }

  private void RefreshControllerServices()
  {
    ControllerServices = new AiShipControllerServices
    {
      PythonAiWorker = _pythonAiWorker,
      RaiderTrainingLogger = _raiderTrainingLogger,
      TraderTrainingLogger = _traderTrainingLogger
    };
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
    _targetCountByArchetype.Remove("neural_patrol");

    if (_pythonAiWorker != null)
    {
      _pythonAiWorker.Unavailable -= OnPythonAiWorkerUnavailable;
      _pythonAiWorker.Shutdown();
      _pythonAiWorker = null;
    }

    RefreshControllerServices();
    DespawnNeuralShips("worker_unavailable");
  }

  private void DespawnNeuralShips(string reason)
  {
    Node aiShipRoot = _aiShipSpawner?.GetParent();
    if (aiShipRoot == null)
      return;

    foreach (Node child in aiShipRoot.GetChildren())
    {
      if (child is not AiShip aiShip)
        continue;
      if (aiShip.ArchetypeId != "neural_patrol")
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
}
