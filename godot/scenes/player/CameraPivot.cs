using Godot;

public partial class CameraPivot : Marker3D
{
  [Export] public float MouseSensitivity { get; set; } = 0.002f;
  [Export] public float MinPitch { get; set; } = -0.8f;
  [Export] public float MaxPitch { get; set; } = 0.3f;
  [Export] public float ZoomSpeed { get; set; } = 2.0f;
  [Export] public float MinZoom { get; set; } = 5.0f;
  [Export] public float MaxZoom { get; set; } = 50.0f;
  [Export] public float DefaultZoom { get; set; } = 20.0f;
  [Export] public float TrackpadRotationSensitivity { get; set; } = 0.01f;

  private float _angleY = 0.0f;
  private float _angleX = 0.0f;
  private float _zoom = 20.0f;
  private Camera3D _camera;
  private Vector3 _initialCameraPosition;

  public override void _Ready()
  {
    _camera = GetNode<Camera3D>("Camera3D");
    _initialCameraPosition = _camera.Position;
    DefaultZoom = _initialCameraPosition.Length();
    _zoom = DefaultZoom;
  }

  public void HandleCameraRotate(float deltaX, float deltaY)
  {
    _angleY -= deltaX * MouseSensitivity;
    _angleX -= deltaY * MouseSensitivity;
    _angleX = Mathf.Clamp(_angleX, MinPitch, MaxPitch);
    Rotation = new Vector3(_angleX, _angleY, Rotation.Z);
  }

  public void HandleCameraZoom(float delta)
  {
    _zoom = Mathf.Clamp(_zoom + delta * ZoomSpeed, MinZoom, MaxZoom);
    _camera.Position = _initialCameraPosition.Normalized() * _zoom;
  }

  public void HandleCameraPan(float deltaX)
  {
    _angleY += deltaX * TrackpadRotationSensitivity;
    Rotation = new Vector3(_angleX, _angleY, Rotation.Z);
  }

  public override void _Process(double delta)
  {
    // Keep global rotation independent of the parent ship's roll/pitch.
    GlobalRotation = new Vector3(_angleX, _angleY, 0);
  }
}
