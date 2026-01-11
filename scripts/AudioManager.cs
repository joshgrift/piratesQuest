namespace PiratesQuest;

using Godot;
using System.Collections.Generic;

/// <summary>
/// AudioManager is an autoload singleton that manages all game sounds.
/// Use this for global sounds like UI clicks, music, and ambient loops.
/// 
/// For positional sounds (like cannon fire from a specific ship), use AudioStreamPlayer3D
/// directly on the object instead.
/// </summary>
public partial class AudioManager : Node
{
  // Dictionary to store preloaded sound resources
  // This avoids loading sounds from disk every time we play them
  private Dictionary<string, AudioStream> _soundCache = new();

  // Pool of AudioStreamPlayer nodes for playing multiple sounds simultaneously
  // This is more efficient than creating/destroying players constantly
  private List<AudioStreamPlayer> _playerPool = new();
  private const int MaxPoolSize = 10; // Maximum number of simultaneous sounds

  // Currently playing ambient/loop sounds (so we can stop them later)
  private Dictionary<string, AudioStreamPlayer> _activeLoops = new();

  public override void _Ready()
  {
    GD.Print("AudioManager ready");
    // Pre-populate the player pool
    for (int i = 0; i < MaxPoolSize; i++)
    {
      var player = new AudioStreamPlayer();
      AddChild(player);
      _playerPool.Add(player);
    }
  }

  /// <summary>
  /// Plays a one-shot sound effect (like UI clicks, pickups, etc.)
  /// This is for non-positional sounds that should be heard globally.
  /// </summary>
  /// <param name="soundPath">Path to the sound file (e.g., "res://art/sounds/jcsounds/Misc Sfx/coin_collect.wav")</param>
  /// <param name="volumeDb">Volume in decibels (0 = normal, negative = quieter, positive = louder)</param>
  public void PlaySound(string soundPath, float volumeDb = 0.0f)
  {
    // Get or load the sound resource
    AudioStream stream = GetOrLoadSound(soundPath);
    if (stream == null)
    {
      GD.PrintErr($"AudioManager: Could not load sound at {soundPath}");
      return;
    }

    // Get an available player from the pool
    AudioStreamPlayer player = GetAvailablePlayer();
    if (player == null)
    {
      GD.PrintErr("AudioManager: No available players in pool (too many sounds playing?)");
      return;
    }

    // Configure and play the sound
    player.Stream = stream;
    player.VolumeDb = volumeDb;
    player.Play();
  }

  /// <summary>
  /// Plays a looping sound (like ambient ocean, music, etc.)
  /// Returns the player so you can stop it later with StopLoop()
  /// </summary>
  /// <param name="loopName">Unique name for this loop (so you can stop it later)</param>
  /// <param name="soundPath">Path to the sound file</param>
  /// <param name="volumeDb">Volume in decibels</param>
  public void PlayLoop(string loopName, string soundPath, float volumeDb = 0.0f)
  {
    // Stop existing loop with same name if it's playing
    if (_activeLoops.ContainsKey(loopName))
    {
      StopLoop(loopName);
    }

    AudioStream stream = GetOrLoadSound(soundPath);
    if (stream == null)
    {
      GD.PrintErr($"AudioManager: Could not load sound at {soundPath}");
      return;
    }

    AudioStreamPlayer player = GetAvailablePlayer();
    if (player == null)
    {
      GD.PrintErr("AudioManager: No available players in pool");
      return;
    }

    player.Stream = stream;
    player.VolumeDb = volumeDb;
    player.Autoplay = false; // We'll start it manually
    player.Play(); // Start playing

    // Mark this as a loop (so it doesn't get reused)
    _activeLoops[loopName] = player;
  }

  /// <summary>
  /// Stops a currently playing loop by name
  /// </summary>
  public void StopLoop(string loopName)
  {
    if (_activeLoops.TryGetValue(loopName, out AudioStreamPlayer player))
    {
      player.Stop();
      _activeLoops.Remove(loopName);
    }
  }

  /// <summary>
  /// Gets an available AudioStreamPlayer from the pool, or null if all are busy
  /// </summary>
  private AudioStreamPlayer GetAvailablePlayer()
  {
    foreach (var player in _playerPool)
    {
      // A player is available if it's not playing and not in active loops
      if (!player.Playing && !_activeLoops.ContainsValue(player))
      {
        return player;
      }
    }
    return null; // All players are busy
  }

  /// <summary>
  /// Loads a sound resource, using cache if available
  /// This is a performance optimization - loading from disk is slow
  /// </summary>
  private AudioStream GetOrLoadSound(string soundPath)
  {
    // Check cache first
    if (_soundCache.TryGetValue(soundPath, out AudioStream cached))
    {
      return cached;
    }

    // Load from disk
    AudioStream stream = GD.Load<AudioStream>(soundPath);
    if (stream != null)
    {
      // Cache it for future use
      _soundCache[soundPath] = stream;
    }

    return stream;
  }
}
