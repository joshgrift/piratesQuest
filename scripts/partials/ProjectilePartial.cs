namespace PiratesQuest.Partials;

using PiratesQuest.Attributes;

using Godot;

/// <summary>
/// Interface for objects that can collect items from a CollectionPoint or other locations.
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

  private void OnBodyEntered(Node body)
  {
    if (!Multiplayer.IsServer()) return;

    if (body is IDamageable)
    {
      body.Rpc(Player.MethodName.TakeDamage, Damage);
      CallDeferred(MethodName.QueueFree);
    }
  }
}

