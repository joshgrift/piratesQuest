namespace PiratesQuest.AI;

using PiratesQuest.AI.pythonNavigation;

/// <summary>
/// Optional shared dependencies that AI controllers may need at construction time.
///
/// <para>
/// Most archetypes do not need anything from this object. It exists so a single
/// shared resource (right now: the Python worker process) can be injected into the
/// few controllers that actually want it without adding it to every constructor.
/// </para>
/// </summary>
public sealed class AiShipControllerServices
{
  /// <summary>
  /// Shared Python AI worker for neural controllers.
  /// Null when Python AI is disabled or the worker never reached a ready handshake.
  /// </summary>
  public PythonAiWorkerClient PythonAiWorker { get; init; }

  public static readonly AiShipControllerServices Empty = new();
}
