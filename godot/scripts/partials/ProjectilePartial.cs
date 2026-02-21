namespace PiratesQuest.Partials;

using PiratesQuest.Attributes;

using Godot;

/// <summary>
/// Base class for projectiles like cannonballs.
/// Handles movement, collision detection, and spawning explosion effects.
///
/// Key Godot concepts used here:
/// - PackedScene: A "template" for creating nodes. Think of it like a class you can instantiate.
/// - GD.Load<T>(): Loads a resource (scene, texture, sound, etc.) from the file system.
/// - GetTree().Root: The root of the entire scene tree - we add the explosion here so it
///   persists even after the cannonball is destroyed.
/// - Rpc(): Remote Procedure Call - sends a message to other players in multiplayer.
/// </summary>
public partial class ProjectilePartial : RigidBody3D
{
  public string PlayerId { get; private set; }

  [Export] public int Damage = 0;

  [Export] public int Speed = 10;

  [Export] public int TimeToLiveInSeconds = 5;

  private float _timeAlive = 0.0f;

  private Vector3 _targetVelocity = Vector3.Zero;

  private float _currentSpeed = 0.0f;

  // The explosion scene - loaded on first use.
  // We use a property with lazy loading to avoid issues with static initialization in multiplayer.
  private static PackedScene _explosionScene;
  private static PackedScene ExplosionScene
  {
    get
    {
      // Load the scene only when first needed (lazy loading)
      _explosionScene ??= GD.Load<PackedScene>("res://scenes/explosion/explosion.tscn");
      return _explosionScene;
    }
  }

  public override void _Ready()
  {
    Area3D PlayerCollision = GetNode<Area3D>("PlayerCollision");
    PlayerCollision.BodyEntered += OnBodyEntered;
  }

  public void Launch(Vector3 direction, float launcherSpeed, string playerId, int damage)
  {
    PlayerId = playerId;
    direction.Y = 0;
    _targetVelocity = direction.Normalized();
    _currentSpeed = Speed + launcherSpeed;
    Damage = damage;

    _targetVelocity.X *= _currentSpeed;
    _targetVelocity.Z *= _currentSpeed;
    _targetVelocity.Y = 5.0f; // Slight arc effect
    LinearVelocity = _targetVelocity;
  }

  public override void _Process(double delta)
  {
    _timeAlive += (float)delta;
    if (_timeAlive >= TimeToLiveInSeconds)
    {
      CallDeferred(MethodName.QueueFree);
    }
  }

  /// <summary>
  /// Called when the cannonball's Area3D detects a collision with another body.
  /// We spawn an explosion effect, deal damage, then destroy the cannonball.
  /// </summary>
  private void OnBodyEntered(Node body)
  {
    // Only the server should handle damage to keep multiplayer in sync.
    // Without this check, each client would deal damage separately = chaos!
    if (!Multiplayer.IsServer()) return;

    if (body is IDamageable)
    {
      // Deal damage to the target via RPC (so all clients see it)
      body.Rpc(Player.MethodName.TakeDamage, Damage);

      // Spawn the explosion effect at the cannonball's current position.
      // We use Rpc to tell ALL clients to spawn the effect (so everyone sees it).
      Rpc(MethodName.SpawnExplosionEffect, GlobalPosition);

      // Remove the cannonball. CallDeferred waits until it's safe to delete.
      CallDeferred(MethodName.QueueFree);
    }
  }

  /// <summary>
  /// Spawns a visual explosion effect at the given position.
  /// This is called via RPC so all players see the explosion.
  ///
  /// [Rpc] attribute makes this method callable across the network:
  /// - MultiplayerApi.RpcMode.AnyPeer: Any player can call this
  /// - CallLocal = true: Also runs on the machine that called it
  /// </summary>
  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  private void SpawnExplosionEffect(Vector3 position)
  {
    // Instantiate() creates a new instance from the PackedScene template.
    // This is similar to "new MyClass()" but for Godot scenes.
    var explosion = ExplosionScene.Instantiate<Node3D>();

    // IMPORTANT: Add to tree FIRST, then set GlobalPosition!
    // GlobalPosition only works when the node is inside the scene tree.
    // This is because GlobalPosition is calculated relative to parent nodes.
    GetTree().Root.AddChild(explosion);

    // Now we can safely set the global position
    explosion.GlobalPosition = position;
  }
}

