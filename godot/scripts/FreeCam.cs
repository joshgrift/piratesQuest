namespace PiratesQuest;

using Godot;

/// <summary>
/// Simple free-flying camera for server/debugging purposes.
/// Hold left mouse button and drag to move and look.
/// WASD to move, Ctrl for down, Space for up.
/// </summary>
public partial class FreeCam : Camera3D
{
  [Export] private float _moveSpeed = 50.0f;
  [Export] private float _lookSensitivity = 0.003f;
  [Export] private float _speedMultiplier = 5.0f;

  private Vector2 _mouseMotion = Vector2.Zero;
  private bool _isDragging = false;

  public override void _Ready()
  {
    // Ensure this camera is active
    Current = true;
  }

  public override void _Process(double delta)
  {
    // Only move when dragging
    if (!_isDragging) return;

    // Handle movement
    Vector3 velocity = Vector3.Zero;

    // Forward/backward (relative to camera direction)
    if (Input.IsKeyPressed(Key.W))
      velocity -= Transform.Basis.Z;
    if (Input.IsKeyPressed(Key.S))
      velocity += Transform.Basis.Z;

    // Left/right (relative to camera direction)
    if (Input.IsKeyPressed(Key.A))
      velocity -= Transform.Basis.X;
    if (Input.IsKeyPressed(Key.D))
      velocity += Transform.Basis.X;

    // Up/down (world space)
    if (Input.IsKeyPressed(Key.Ctrl))
      velocity += Vector3.Down;
    if (Input.IsKeyPressed(Key.Space))
      velocity += Vector3.Up;

    // Apply movement with speed boost when Shift is held
    if (velocity.LengthSquared() > 0)
    {
      float currentSpeed = _moveSpeed;
      if (Input.IsKeyPressed(Key.Shift))
      {
        currentSpeed *= _speedMultiplier;
      }
      Position += velocity.Normalized() * currentSpeed * (float)delta;
    }
  }

  public override void _Input(InputEvent @event)
  {
    // Track left mouse button for dragging
    if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
    {
      _isDragging = mouseButton.Pressed;
    }

    // Handle mouse look only when dragging
    if (@event is InputEventMouseMotion mouseMotion && _isDragging)
    {
      _mouseMotion = mouseMotion.Relative;

      // Rotate left/right (yaw)
      RotateY(-_mouseMotion.X * _lookSensitivity);

      // Rotate up/down (pitch)
      RotateObjectLocal(Vector3.Right, -_mouseMotion.Y * _lookSensitivity);

      // Clamp pitch to avoid flipping
      Vector3 rotation = RotationDegrees;
      rotation.X = Mathf.Clamp(rotation.X, -89, 89);
      RotationDegrees = rotation;
    }
  }
}
