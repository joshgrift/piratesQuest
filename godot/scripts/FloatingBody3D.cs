namespace PiratesQuest;

using Godot;

/// <summary>
/// Reusable floating controller for any Node3D that should follow the water.
/// The owner/root follows the sampled waterline while an optional visual root
/// gets bob/pitch/roll for nicer presentation.
/// </summary>
public sealed class FloatingBody3D
{
  private readonly Node3D _owner;
  private MeshInstance3D _waterPlane;
  private Node3D _waterDebugRoot;
  private MeshInstance3D _bowWaterDebug;
  private MeshInstance3D _sternWaterDebug;
  private MeshInstance3D _centerWaterDebug;
  private MeshInstance3D _waterlineDebug;

  public FloatingBody3D(Node3D owner)
  {
    _owner = owner;
  }

  public bool Apply(
    Node3D visualRoot,
    NodePath waterPlanePath,
    float shipLength,
    float visualBobStrength,
    float waterSmoothSpeed,
    bool showWaterDebug,
    float extraPitch,
    float targetRoll,
    float delta
  )
  {
    ResolveWaterPlane(waterPlanePath);

    if (!ShipWaterPhysics.TrySample(
      _waterPlane,
      _owner.GlobalPosition,
      _owner.GlobalTransform.Basis.Z,
      shipLength,
      Time.GetTicksMsec() / 1000.0f,
      out ShipWaterPhysics.Sample waterSample
    ))
      return false;

    Vector3 currentPos = _owner.GlobalPosition;
    currentPos.Y = waterSample.RootWaterlineY;
    _owner.GlobalPosition = currentPos;

    Vector3 rootRotation = _owner.Rotation;
    rootRotation.X = 0.0f;
    rootRotation.Z = 0.0f;
    _owner.Rotation = rootRotation;

    ApplyVisualWaterMotion(
      visualRoot,
      waterSample.TargetPitch + extraPitch,
      targetRoll,
      waterSample.VisualBobOffsetY,
      visualBobStrength,
      waterSmoothSpeed,
      delta
    );

    UpdateWaterDebug(waterSample, showWaterDebug);
    return true;
  }

  private void ResolveWaterPlane(NodePath waterPlanePath)
  {
    if (_waterPlane != null)
      return;

    if (waterPlanePath != null && !waterPlanePath.IsEmpty)
      _waterPlane = _owner.GetNodeOrNull<MeshInstance3D>(waterPlanePath);

    if (_waterPlane == null)
      _waterPlane = _owner.GetTree().CurrentScene?.FindChild("WaterPlane", true, false) as MeshInstance3D;
  }

  private void ApplyVisualWaterMotion(
    Node3D visualRoot,
    float targetPitch,
    float targetRoll,
    float bobOffsetY,
    float visualBobStrength,
    float waterSmoothSpeed,
    float delta
  )
  {
    if (visualRoot == null)
      return;

    Vector3 visualPosition = visualRoot.Position;
    visualPosition.Y = Mathf.Lerp(
      visualPosition.Y,
      bobOffsetY * visualBobStrength,
      delta * waterSmoothSpeed
    );
    visualRoot.Position = visualPosition;

    Vector3 visualRotation = visualRoot.Rotation;
    visualRotation.X = Mathf.LerpAngle(visualRotation.X, targetPitch, delta * waterSmoothSpeed);
    visualRotation.Z = Mathf.LerpAngle(visualRotation.Z, targetRoll, delta * waterSmoothSpeed * 0.3f);
    visualRoot.Rotation = visualRotation;
  }

  private void UpdateWaterDebug(ShipWaterPhysics.Sample waterSample, bool showWaterDebug)
  {
    EnsureWaterDebugNodes();
    if (_waterDebugRoot == null)
      return;

    _waterDebugRoot.Visible = showWaterDebug;
    if (!showWaterDebug)
      return;

    SetDebugMarkerPosition(_bowWaterDebug, waterSample.BowWorldPosition, waterSample.PlaneY + waterSample.BowWaterY);
    SetDebugMarkerPosition(_sternWaterDebug, waterSample.SternWorldPosition, waterSample.PlaneY + waterSample.SternWaterY);
    SetDebugMarkerPosition(_centerWaterDebug, waterSample.CenterWorldPosition, waterSample.PlaneY + waterSample.CenterWaterY);
    SetDebugMarkerPosition(_waterlineDebug, waterSample.CenterWorldPosition, waterSample.RootWaterlineY);
  }

  private void EnsureWaterDebugNodes()
  {
    if (_waterDebugRoot != null)
      return;

    _waterDebugRoot = new Node3D
    {
      Name = "WaterDebugRoot",
      TopLevel = true,
      Visible = false
    };
    _owner.AddChild(_waterDebugRoot);

    _bowWaterDebug = CreateWaterDebugMarker("BowWaterDebug", new Color(0.9f, 0.3f, 0.3f));
    _sternWaterDebug = CreateWaterDebugMarker("SternWaterDebug", new Color(0.3f, 0.7f, 1.0f));
    _centerWaterDebug = CreateWaterDebugMarker("CenterWaterDebug", new Color(0.95f, 0.85f, 0.2f));
    _waterlineDebug = CreateWaterDebugMarker("WaterlineDebug", new Color(0.3f, 1.0f, 0.4f));

    _waterDebugRoot.AddChild(_bowWaterDebug);
    _waterDebugRoot.AddChild(_sternWaterDebug);
    _waterDebugRoot.AddChild(_centerWaterDebug);
    _waterDebugRoot.AddChild(_waterlineDebug);
  }

  private static MeshInstance3D CreateWaterDebugMarker(string name, Color color)
  {
    var material = new StandardMaterial3D
    {
      AlbedoColor = color,
      ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
    };

    return new MeshInstance3D
    {
      Name = name,
      Mesh = new SphereMesh
      {
        Radius = 0.35f,
        Height = 0.7f
      },
      MaterialOverride = material,
      CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
    };
  }

  private static void SetDebugMarkerPosition(Node3D marker, Vector3 sampleWorldPosition, float sampleWaterY)
  {
    if (marker == null)
      return;

    marker.GlobalPosition = new Vector3(sampleWorldPosition.X, sampleWaterY, sampleWorldPosition.Z);
  }
}
