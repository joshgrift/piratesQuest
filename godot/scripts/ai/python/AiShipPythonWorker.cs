namespace PiratesQuest.AI;

using Godot;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Runs the shared Python worker process for AI ships.
///
/// Godot sends newline-delimited JSON requests and Python replies with actions.
/// This class only owns process and message transport. AiShip and its controller
/// still own sensing, rewards, and per-ship runtime memory.
/// </summary>
public sealed class AiShipPythonWorker
{
  public const int ProtocolVersion = 1;

  private static readonly JsonSerializerOptions JsonOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };

  private readonly string _pythonExecutable;
  private readonly string _scriptPath;
  private readonly string _rolloutPath;
  private readonly object _writeLock = new();
  private readonly ManualResetEventSlim _readyEvent = new(false);
  private readonly ConcurrentDictionary<string, ConcurrentDictionary<int, AiShipPythonActionResult>> _actionsByShip = new();

  private Process _process;
  private StreamWriter _stdin;
  private bool _isReady;
  private bool _isShutdown;
  private int _unavailableNotified;

  public AiShipPythonWorker(string pythonExecutable, string scriptPath, string rolloutPath)
  {
    _pythonExecutable = pythonExecutable ?? "python3";
    _scriptPath = scriptPath ?? string.Empty;
    _rolloutPath = rolloutPath ?? string.Empty;
  }

  public event Action<string> Unavailable;

  public bool IsAvailable => _isReady && !_isShutdown && _process != null && !SafeHasExited(_process);

  public bool Start(int readyTimeoutMs = 3000)
  {
    if (_isShutdown)
      return false;

    if (string.IsNullOrWhiteSpace(_scriptPath) || !File.Exists(_scriptPath))
    {
      GD.PrintErr($"Python AI script not found: {_scriptPath}");
      return false;
    }

    if (string.IsNullOrWhiteSpace(_rolloutPath))
    {
      GD.PrintErr("Python AI rollout path is empty.");
      return false;
    }

    Directory.CreateDirectory(Path.GetDirectoryName(_rolloutPath) ?? ".");

    var startInfo = new ProcessStartInfo
    {
      FileName = _pythonExecutable,
      UseShellExecute = false,
      RedirectStandardInput = true,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
    };

    startInfo.ArgumentList.Add("-u");
    startInfo.ArgumentList.Add(_scriptPath);
    startInfo.ArgumentList.Add("--rollout-path");
    startInfo.ArgumentList.Add(_rolloutPath);

    try
    {
      _process = new Process
      {
        StartInfo = startInfo,
        EnableRaisingEvents = true
      };
      _process.Exited += OnProcessExited;

      if (!_process.Start())
      {
        GD.PrintErr("Python AI worker process failed to start.");
        return false;
      }

      _stdin = _process.StandardInput;
      _stdin.AutoFlush = true;

      _ = Task.Run(ReadStdoutLoopAsync);
      _ = Task.Run(ReadStderrLoopAsync);
    }
    catch (Exception exception)
    {
      GD.PrintErr($"Failed to start Python AI worker: {exception.Message}");
      Shutdown();
      return false;
    }

    bool ready = _readyEvent.Wait(readyTimeoutMs);
    if (!ready || !_isReady)
    {
      Shutdown();
      return false;
    }

    GD.Print($"Python AI worker ready. Rollouts: {_rolloutPath}");
    return true;
  }

  public bool TryRequestDecision(string shipId, int sequence, string episodeId, AiShipPythonObservation observation)
  {
    if (!IsAvailable || string.IsNullOrWhiteSpace(shipId) || observation == null)
      return false;

    return TryWriteMessage(new AiShipPythonDecisionMessage
    {
      Type = "decide",
      ProtocolVersion = ProtocolVersion,
      ShipId = shipId,
      Sequence = sequence,
      EpisodeId = episodeId,
      Observation = observation
    });
  }

  public bool TrySendTransition(
    string shipId,
    string episodeId,
    AiShipPythonObservation observation,
    AiShipPythonAction action,
    float reward,
    AiShipPythonObservation nextObservation,
    bool done,
    string doneReason)
  {
    if (!IsAvailable || string.IsNullOrWhiteSpace(shipId) || observation == null || action == null || nextObservation == null)
      return false;

    return TryWriteMessage(new AiShipPythonTransitionMessage
    {
      Type = "transition",
      ProtocolVersion = ProtocolVersion,
      ShipId = shipId,
      EpisodeId = episodeId,
      Observation = observation,
      Action = action,
      Reward = reward,
      NextObservation = nextObservation,
      Done = done,
      DoneReason = doneReason
    });
  }

  public bool TryTakeAction(string shipId, int sequence, out AiShipPythonActionResult action)
  {
    action = null;

    if (string.IsNullOrWhiteSpace(shipId))
      return false;

    if (!_actionsByShip.TryGetValue(shipId, out var bySequence))
      return false;

    return bySequence.TryRemove(sequence, out action);
  }

  public void Shutdown()
  {
    if (_isShutdown)
      return;

    _isShutdown = true;
    _readyEvent.Set();

    try
    {
      _stdin?.Close();
    }
    catch
    {
    }

    try
    {
      if (_process != null)
      {
        _process.Exited -= OnProcessExited;
        if (!SafeHasExited(_process))
        {
          _process.Kill(entireProcessTree: true);
          _process.WaitForExit(1000);
        }

        _process.Dispose();
      }
    }
    catch (Exception exception)
    {
      GD.PrintErr($"Error while shutting down Python AI worker: {exception.Message}");
    }
    finally
    {
      _stdin = null;
      _process = null;
      _isReady = false;
    }
  }

  private bool TryWriteMessage<T>(T message)
  {
    if (!IsAvailable)
      return false;

    string json = JsonSerializer.Serialize(message, JsonOptions);

    lock (_writeLock)
    {
      if (!IsAvailable || _stdin == null)
        return false;

      try
      {
        _stdin.WriteLine(json);
        return true;
      }
      catch (Exception exception)
      {
        GD.PrintErr($"Python AI write failed: {exception.Message}");
        NotifyUnavailableOnce("write_failed");
        return false;
      }
    }
  }

  private async Task ReadStdoutLoopAsync()
  {
    try
    {
      while (_process?.StandardOutput != null)
      {
        string line = await _process.StandardOutput.ReadLineAsync();
        if (line == null)
          break;

        HandleStdoutLine(line);
      }
    }
    catch (Exception exception)
    {
      if (!_isShutdown)
        GD.PrintErr($"Python AI stdout reader failed: {exception.Message}");
    }

    NotifyUnavailableOnce("stdout_closed");
  }

  private async Task ReadStderrLoopAsync()
  {
    try
    {
      while (_process?.StandardError != null)
      {
        string line = await _process.StandardError.ReadLineAsync();
        if (line == null)
          break;

        if (!string.IsNullOrWhiteSpace(line))
          GD.PrintErr($"Python AI stderr: {line}");
      }
    }
    catch (Exception exception)
    {
      if (!_isShutdown)
        GD.PrintErr($"Python AI stderr reader failed: {exception.Message}");
    }
  }

  private void HandleStdoutLine(string line)
  {
    try
    {
      using JsonDocument document = JsonDocument.Parse(line);
      JsonElement root = document.RootElement;
      if (!root.TryGetProperty("type", out JsonElement typeElement))
        return;

      switch (typeElement.GetString())
      {
        case "ready":
          _isReady = true;
          _readyEvent.Set();
          break;
        case "action":
          StoreAction(root);
          break;
      }
    }
    catch (Exception exception)
    {
      GD.PrintErr($"Python AI stdout parse failed: {exception.Message}");
    }
  }

  private void StoreAction(JsonElement root)
  {
    string shipId = root.GetProperty("shipId").GetString() ?? string.Empty;
    int sequence = root.GetProperty("sequence").GetInt32();

    var action = new AiShipPythonActionResult
    {
      ShipId = shipId,
      Sequence = sequence,
      Throttle = Mathf.Clamp(root.GetProperty("throttle").GetSingle(), -1.0f, 1.0f),
      Turn = Mathf.Clamp(root.GetProperty("turn").GetSingle(), -1.0f, 1.0f),
      FireLeft = root.TryGetProperty("fireLeft", out JsonElement fireLeftElement) && fireLeftElement.GetBoolean(),
      FireRight = root.TryGetProperty("fireRight", out JsonElement fireRightElement) && fireRightElement.GetBoolean(),
      DebugState = root.TryGetProperty("debugState", out JsonElement debugStateElement)
        ? debugStateElement.GetString() ?? string.Empty
        : string.Empty
    };

    var bySequence = _actionsByShip.GetOrAdd(shipId, _ => new ConcurrentDictionary<int, AiShipPythonActionResult>());
    bySequence[sequence] = action;
  }

  private void OnProcessExited(object sender, EventArgs args)
  {
    NotifyUnavailableOnce("process_exited");
  }

  private void NotifyUnavailableOnce(string reason)
  {
    _readyEvent.Set();

    if (_isShutdown)
      return;

    if (Interlocked.Exchange(ref _unavailableNotified, 1) != 0)
      return;

    _isReady = false;
    Unavailable?.Invoke(reason);
  }

  private static bool SafeHasExited(Process process)
  {
    try
    {
      return process.HasExited;
    }
    catch
    {
      return true;
    }
  }
}

public sealed class AiShipPythonObservation
{
  public string AiType { get; init; } = string.Empty;
  public float GoalLocalX { get; init; }
  public float GoalLocalZ { get; init; }
  public float DistanceToGoal { get; init; }
  public bool HasTargetShip { get; init; }
  public bool TargetShipIsPlayer { get; init; }
  public float TargetShipDistance { get; init; }
  public float TargetShipLocalX { get; init; }
  public float TargetShipLocalZ { get; init; }
  public bool HasThreatShip { get; init; }
  public float ThreatShipDistance { get; init; }
  public float ThreatShipLocalX { get; init; }
  public float ThreatShipLocalZ { get; init; }
  public bool HasNearestPort { get; init; }
  public float NearestPortDistance { get; init; }
  public float NearestPortLocalX { get; init; }
  public float NearestPortLocalZ { get; init; }
  public float SpeedFraction { get; init; }
  public float ForwardDistanceFraction { get; init; }
  public float ForwardLeftDistanceFraction { get; init; }
  public float ForwardRightDistanceFraction { get; init; }
  public float WideLeftDistanceFraction { get; init; }
  public float WideRightDistanceFraction { get; init; }
  public float LeftPressure { get; init; }
  public float RightPressure { get; init; }
  public bool FrontBlocked { get; init; }
  public bool IsStuck { get; init; }
  public bool IsEscaping { get; init; }
}

public class AiShipPythonAction
{
  public float Throttle { get; init; }
  public float Turn { get; init; }
  public bool FireLeft { get; init; }
  public bool FireRight { get; init; }
  public string DebugState { get; init; } = string.Empty;
}

public sealed class AiShipPythonActionResult : AiShipPythonAction
{
  public string ShipId { get; init; } = string.Empty;
  public int Sequence { get; init; }
}

internal sealed class AiShipPythonDecisionMessage
{
  public string Type { get; init; } = string.Empty;
  public int ProtocolVersion { get; init; }
  public string ShipId { get; init; } = string.Empty;
  public int Sequence { get; init; }
  public string EpisodeId { get; init; } = string.Empty;
  public AiShipPythonObservation Observation { get; init; }
}

internal sealed class AiShipPythonTransitionMessage
{
  public string Type { get; init; } = string.Empty;
  public int ProtocolVersion { get; init; }
  public string ShipId { get; init; } = string.Empty;
  public string EpisodeId { get; init; } = string.Empty;
  public AiShipPythonObservation Observation { get; init; }
  public AiShipPythonAction Action { get; init; }
  public float Reward { get; init; }
  public AiShipPythonObservation NextObservation { get; init; }
  public bool Done { get; init; }
  public string DoneReason { get; init; } = string.Empty;
}
