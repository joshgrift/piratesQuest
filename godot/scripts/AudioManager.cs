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

  // When true, background loops (like ocean) won't play
  // This is separate from the Master bus mute - other sounds still work!
  private bool _backgroundMuted = false;

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
  /// Checks if we're running as a dedicated server (no audio output).
  /// Servers don't have speakers, so we skip all audio playback.
  /// </summary>
  private bool IsServer()
  {
    // Multiplayer is a property on Node (which AudioManager inherits from)
    // IsServer() returns true if we're hosting the game
    return Multiplayer.IsServer();
  }

  /// <summary>
  /// Plays a one-shot sound effect (like UI clicks, pickups, etc.)
  /// This is for non-positional sounds that should be heard globally.
  /// </summary>
  /// <param name="soundPath">Path to the sound file (e.g., "res://art/sounds/jcsounds/Misc Sfx/coin_collect.wav")</param>
  /// <param name="volumeDb">Volume in decibels (0 = normal, negative = quieter, positive = louder)</param>
  public void PlaySound(string soundPath, float volumeDb = 0.0f)
  {
    // Don't play sounds on the server - it has no audio output
    if (IsServer()) return;

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
  /// The sound will automatically restart when it finishes.
  /// </summary>
  /// <param name="loopName">Unique name for this loop (so you can stop it later)</param>
  /// <param name="soundPath">Path to the sound file</param>
  /// <param name="volumeDb">Volume in decibels</param>
  public void PlayLoop(string loopName, string soundPath, float volumeDb = 0.0f)
  {
    // Don't play sounds on the server - it has no audio output
    if (IsServer()) return;

    // Don't play background loops if they've been muted
    if (_backgroundMuted) return;

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

    // Connect to the Finished signal to restart when the sound ends
    // This creates a true loop! We disconnect first in case it was already connected.
    // The Callable stores a reference to the player so each loop restarts itself.
    if (player.IsConnected(AudioStreamPlayer.SignalName.Finished, Callable.From(() => OnLoopFinished(player))))
    {
      player.Disconnect(AudioStreamPlayer.SignalName.Finished, Callable.From(() => OnLoopFinished(player)));
    }
    player.Finished += () => OnLoopFinished(player);

    player.Play(); // Start playing

    // Mark this as a loop (so it doesn't get reused)
    _activeLoops[loopName] = player;
  }

  /// <summary>
  /// Called when a looping sound finishes - restarts it to create a seamless loop.
  /// </summary>
  private void OnLoopFinished(AudioStreamPlayer player)
  {
    // Only restart if this player is still in our active loops
    // (If it was stopped via StopLoop, it won't be in the dictionary)
    if (_activeLoops.ContainsValue(player))
    {
      player.Play();
    }
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
  /// Mutes or unmutes background sounds (like ocean waves).
  /// Other sounds like cannon fire and UI still play normally.
  ///
  /// When muted, this stops any currently playing loops and prevents
  /// new loops from starting until unmuted.
  /// </summary>
  /// <param name="mute">True to mute background sounds, false to allow them</param>
  public void SetBackgroundMuted(bool mute)
  {
    _backgroundMuted = mute;
    GD.Print($"Background audio muted: {mute}");

    if (mute)
    {
      // Stop all currently playing loops when muting
      // We need to copy the keys because StopLoop modifies the dictionary
      var loopNames = new List<string>(_activeLoops.Keys);
      foreach (var loopName in loopNames)
      {
        StopLoop(loopName);
      }
    }
    // Note: When unmuting, loops will start again when the code that
    // originally called PlayLoop runs again (e.g., when entering Play scene)
  }

  /// <summary>
  /// Checks if background sounds are currently muted.
  /// </summary>
  /// <returns>True if muted, false if not</returns>
  public bool IsBackgroundMuted()
  {
    return _backgroundMuted;
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
