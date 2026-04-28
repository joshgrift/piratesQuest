namespace PiratesQuest.AI;

using System;
using System.Collections.Generic;

/// <summary>
/// Small runtime memory bag for one spawned AI ship.
///
/// This lets controllers remember information from one frame to the next
/// without storing controller-specific state inside AiShip.cs.
/// </summary>
public sealed class AiShipMemory
{
  private readonly Dictionary<string, object> _values = new();

  /// <summary>
  /// Forget everything for this ship.
  /// A fresh spawn should start with a fresh memory bag.
  /// </summary>
  public void Clear()
  {
    _values.Clear();
  }

  /// <summary>
  /// Store or replace one value.
  /// </summary>
  public void Set<T>(string key, T value)
  {
    _values[key] = value;
  }

  /// <summary>
  /// Try to read a typed value back out of memory.
  /// Returns false if the key does not exist or the stored type does not match.
  /// </summary>
  public bool TryGet<T>(string key, out T value)
  {
    if (_values.TryGetValue(key, out object rawValue) && rawValue is T typedValue)
    {
      value = typedValue;
      return true;
    }

    value = default;
    return false;
  }

  /// <summary>
  /// Read an existing typed value or create it once if it does not exist yet.
  /// </summary>
  public T GetOrCreate<T>(string key, Func<T> factory)
  {
    if (TryGet<T>(key, out T existingValue))
      return existingValue;

    T newValue = factory();
    _values[key] = newValue;
    return newValue;
  }
}
