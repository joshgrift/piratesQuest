using Godot;

public partial class CameraPivot : Marker3D
{
  [Export]
  public float MouseSensitivity { get; set; } = 0.002f;

  // Clamp vertical rotation to prevent camera from going below the world
  // 0 = looking straight ahead (horizon)
  // Negative values = looking up, Positive values = looking down
  [Export]
  public float MinPitch { get; set; } = -1.2f; // Limit upward rotation to ~69 degrees to prevent flipping

  [Export]
  public float MaxPitch { get; set; } = 0.3f; // Can look down a bit, but not too far below the world

  [Export]
  public float ZoomSpeed { get; set; } = 2.0f; // How fast the camera zooms in/out

  [Export]
  public float MinZoom { get; set; } = 5.0f; // Closest to the ship

  [Export]
  public float MaxZoom { get; set; } = 50.0f; // Furthest from the ship

  [Export]
  public float DefaultZoom { get; set; } = 20.0f; // Starting zoom distance

  [Export]
  public float TrackpadRotationSensitivity { get; set; } = 0.01f; // Sensitivity for trackpad horizontal scrolling rotation

  private float _cameraTargetAngleY = 0.0f; // Yaw (horizontal rotation)
  private float _cameraTargetAngleX = 0.0f; // Pitch (vertical rotation)
  private bool _isDragging = false;
  private float _currentZoom = 20.0f; // Current zoom distance from pivot
  private Camera3D _camera; // Reference to the camera child node
  private Vector3 _initialCameraPosition; // Store the camera's initial position from the scene
  private Control _portUI; // Reference to the Port UI to check if mouse is over it

  public override void _Ready()
  {
    // Get the Camera3D child node
    _camera = GetNode<Camera3D>("Camera3D");

    // Store the initial position so we can preserve Y offset and any other offsets
    _initialCameraPosition = _camera.Position;

    // Calculate the initial distance from pivot (this is our reference zoom level)
    DefaultZoom = _initialCameraPosition.Length();

    // Initialize zoom to default value
    _currentZoom = DefaultZoom;

    // Get reference to PortUI from the HUD
    // Navigate up to Player, then to HUD, then to PortUI
    CallDeferred(MethodName.FindPortUI);
  }

  private void FindPortUI()
  {
    // Try to find the PortUI control in the scene tree
    var hud = GetTree().Root.FindChild("HUD", true, false);
    if (hud != null)
    {
      _portUI = hud.FindChild("PortUI", true, false) as Control;
    }
  }

  // Use _Input to ensure we always receive mouse events, even if UI is present
  public override void _Input(InputEvent @event)
  {
    // If mouse is over the PortUI, don't handle camera controls
    if (_portUI != null && _portUI.Visible && _portUI.GetGlobalRect().HasPoint(_portUI.GetGlobalMousePosition()))
    {
      return;
    }
    // Start dragging when left mouse button is pressed
    if (@event is InputEventMouseButton mouseButton)
    {
      if (mouseButton.ButtonIndex == MouseButton.Left)
      {
        _isDragging = mouseButton.Pressed;
        GD.Print($"[CameraPivot] Dragging: {_isDragging}");
      }
      // Handle scroll wheel for zooming (works for both mouse wheel and trackpad scroll)
      else if (mouseButton.Pressed)
      {
        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
          // Scroll up = zoom in (get closer)
          _currentZoom -= ZoomSpeed;
          _currentZoom = Mathf.Clamp(_currentZoom, MinZoom, MaxZoom);
          UpdateCameraPosition();
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
          // Scroll down = zoom out (get further)
          _currentZoom += ZoomSpeed;
          _currentZoom = Mathf.Clamp(_currentZoom, MinZoom, MaxZoom);
          UpdateCameraPosition();
        }
      }
    }

    // Handle trackpad pinch-to-zoom gesture (macOS/touchpad specific)
    if (@event is InputEventMagnifyGesture magnifyGesture)
    {
      // Pinch in = zoom out, Pinch out = zoom in
      // Factor > 1.0 means spreading fingers (zoom in), < 1.0 means pinching (zoom out)
      float zoomChange = (1.0f - magnifyGesture.Factor) * ZoomSpeed * 5.0f;
      _currentZoom += zoomChange;
      _currentZoom = Mathf.Clamp(_currentZoom, MinZoom, MaxZoom);
      UpdateCameraPosition();
    }

    // Handle two-finger pan on trackpad
    if (@event is InputEventPanGesture panGesture)
    {
      // Vertical panning on trackpad = zoom
      // Pan up = zoom in, Pan down = zoom out
      _currentZoom += panGesture.Delta.Y * ZoomSpeed * 0.5f;
      _currentZoom = Mathf.Clamp(_currentZoom, MinZoom, MaxZoom);
      UpdateCameraPosition();

      // Horizontal panning on trackpad = rotate camera around the ship
      // Pan left = rotate right (clockwise), Pan right = rotate left (counterclockwise)
      _cameraTargetAngleY += panGesture.Delta.X * TrackpadRotationSensitivity;

      // Apply the rotation (keep current pitch, update yaw)
      Rotation = new Vector3(_cameraTargetAngleX, _cameraTargetAngleY, Rotation.Z);
    }

    // Only rotate the camera when dragging
    if (@event is InputEventMouseMotion mouseMotion && _isDragging)
    {
      // Horizontal rotation (yaw) - rotate around Y axis
      _cameraTargetAngleY -= mouseMotion.Relative.X * MouseSensitivity;

      // Vertical rotation (pitch) - rotate around X axis
      _cameraTargetAngleX -= mouseMotion.Relative.Y * MouseSensitivity;

      // Clamp the vertical rotation to prevent camera from going below the world
      _cameraTargetAngleX = Mathf.Clamp(_cameraTargetAngleX, MinPitch, MaxPitch);

      // Apply the rotation (X = pitch, Y = yaw, Z = roll)
      Rotation = new Vector3(_cameraTargetAngleX, _cameraTargetAngleY, Rotation.Z);
    }
  }

  /// <summary>
  /// Updates the camera's position based on the current zoom level.
  /// Scales the camera's position vector to maintain its direction while adjusting distance.
  /// </summary>
  private void UpdateCameraPosition()
  {
    if (_camera != null && _initialCameraPosition.Length() > 0)
    {
      // Get the normalized direction of the initial camera position
      Vector3 direction = _initialCameraPosition.Normalized();

      // Position the camera at the zoom distance along that direction
      // This maintains the camera's angle while zooming in/out
      _camera.Position = direction * _currentZoom;
    }
  }

  // Override _Process to prevent the camera from inheriting the player's rotation
  // This keeps the camera focused on the ship but prevents it from tilting/rolling with the ship
  public override void _Process(double delta)
  {
    // Reset the global rotation to only use our camera rotation values
    // This prevents inheriting the parent's (player ship's) rotation
    GlobalRotation = new Vector3(_cameraTargetAngleX, _cameraTargetAngleY, 0);
  }
}
