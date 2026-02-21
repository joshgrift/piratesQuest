namespace PiratesQuest;

using Godot;

/// <summary>
/// A simple explosion effect that plays particles and sound, then removes itself.
///
/// How it works:
/// 1. When added to the scene, the particles automatically start emitting (OneShot mode)
/// 2. The sound plays immediately
/// 3. After the particles finish, the node deletes itself
///
/// GPUParticles3D is Godot's way of creating particle effects like fire, smoke, explosions.
/// It uses the GPU to render thousands of small sprites efficiently.
/// </summary>
public partial class Explosion : Node3D
{
  // Reference to the particle system - we'll get this in _Ready()
  private GpuParticles3D _particles;

  // Reference to the sound player
  private AudioStreamPlayer3D _audioPlayer;

  public override void _Ready()
  {
    // Get references to our child nodes
    _particles = GetNode<GpuParticles3D>("GPUParticles3D");
    _audioPlayer = GetNode<AudioStreamPlayer3D>("AudioStreamPlayer3D");

    // Play the explosion sound
    _audioPlayer.Play();

    // The particles are set to "OneShot" in the scene, meaning they emit once and stop.
    // We connect to the "finished" signal to know when all particles have died.
    _particles.Finished += OnParticlesFinished;

    // Make sure particles are emitting
    _particles.Emitting = true;
  }

  /// <summary>
  /// Called when the particle effect finishes.
  /// We clean up by removing this node from the scene tree.
  /// </summary>
  private void OnParticlesFinished()
  {
    // QueueFree() safely removes the node at the end of the current frame.
    // This is the standard way to delete nodes in Godot - never use C#'s Dispose() directly!
    QueueFree();
  }
}

