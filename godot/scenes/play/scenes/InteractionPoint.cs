using System;
using Godot;
using PiratesQuest;

[Tool]
public partial class InteractionPoint : Node3D
{
  // These match the original scene setup.
  // We use them as the "reference look" so larger rings keep the same band width.
  private const float DefaultInteractionRadius = 10.0f;
  private const float DefaultRingThickness = 0.028f;
  private const float DefaultPulseStrength = 0.05f;
  private const float DefaultEdgeSoftness = 0.012f;
  private const float DefaultGlowPadding = 0.05f;
  private const float DefaultGlowSoftness = 0.025f;

  // === EXPORTED PROPERTIES ===
  // These appear in the Godot Inspector and can be set per-instance!
  // [Export] attribute exposes the variable to the editor

  // One radius value now controls both gameplay and visuals.
  // That means the collision area and the flashing ring always match.
  private float _interactionRadius = 10.0f;

  [Export(PropertyHint.Range, "0.5,200,0.5,or_greater")]
  public float InteractionRadius
  {
    get => _interactionRadius;
    set
    {
      // Clamp the value so we never end up with a zero-sized interaction point.
      _interactionRadius = Math.Max(0.5f, value);
      UpdateInteractionRadius();
    }
  }

  // The color of the interaction ring - change this for different interaction types
  // Examples: Gold for treasure, Blue for ports, Green for resources
  [Export] public Color RingColor { get; set; } = new Color(1.0f, 0.8f, 0.0f, 0.35f);
  [Export] public Area3D InteractionArea;
  [Export] public Node3D VisualRoot;

  // Point this at the water mesh used by the play scene.
  // We keep it exported so future scenes can override it in the Inspector if needed.
  [Export] public NodePath WaterPlanePath { get; set; } = new("/root/Play/WaterPlane");

  // These are the same kinds of tuning values the ships use, just much smaller.
  // A short sample length keeps the buoy from pitching too aggressively.
  [Export] public float FloatSampleLength { get; set; } = 2.0f;
  [Export] public float VisualBobStrength { get; set; } = 0.45f;
  [Export] public float WaterSmoothSpeed { get; set; } = 6.0f;

  // Reference to the mesh that displays the ring
  // We need this to access the shader material
  private MeshInstance3D _meshInstance;
  private CollisionShape3D _collisionShape;
  private FloatingBody3D _floatingBody;
  private bool _materialWasDuplicated = false;
  private bool _collisionShapeWasDuplicated = false;

  public override void _Ready()
  {
    _floatingBody = new FloatingBody3D(this);
    CacheChildNodes();

    // IMPORTANT: Make the material unique for this instance!
    // Without this, ALL InteractionPoints share the same material.
    // Changing one would change them all (like editing a shared object in JS).
    //
    // Duplicate() creates a deep copy of the resource.
    // Now this instance has its own independent material.
    if (_meshInstance?.MaterialOverride != null && !_materialWasDuplicated)
    {
      _meshInstance.MaterialOverride = (Material)_meshInstance.MaterialOverride.Duplicate();
      _materialWasDuplicated = true;
    }

    EnsureUniqueCollisionShape();

    // Apply the color to the shader
    UpdateRingColor();
    UpdateInteractionRadius();
  }

  public override void _Notification(int what)
  {
    // In the editor, Godot can rebuild the scene tree while you tweak Inspector values.
    // Refresh cached nodes so radius/color updates stay live while editing.
    if (what == NotificationReady)
    {
      UpdateRingColor();
      UpdateInteractionRadius();
    }
  }

  /// <summary>
  /// Updates the shader's ring_color parameter to match our RingColor property.
  /// Call this after changing RingColor at runtime.
  /// </summary>
  public void UpdateRingColor()
  {
    CacheChildNodes();
    if (_meshInstance == null) return;

    // Get the material from the mesh
    // MaterialOverride is a material applied on top of the mesh's default material
    //
    // "is ShaderMaterial material" is C# PATTERN MATCHING:
    // - Checks if MaterialOverride is a ShaderMaterial
    // - If true, also assigns it to a new variable called "material"
    // - This is cleaner than: var material = x as ShaderMaterial; if (material != null)
    if (_meshInstance.MaterialOverride is ShaderMaterial material)
    {
      // SetShaderParameter() sets a uniform value in the shader
      // The string "ring_color" must match the uniform name in the .gdshader file
      material.SetShaderParameter("ring_color", RingColor);
    }
  }

  /// <summary>
  /// Keeps the collision radius and ring size driven by one exported value.
  /// This way designers only need to change a single number in the Inspector.
  /// </summary>
  public void UpdateInteractionRadius()
  {
    CacheChildNodes();
    EnsureUniqueCollisionShape();

    if (_collisionShape?.Shape is CylinderShape3D collisionCylinder)
    {
      collisionCylinder.Radius = InteractionRadius;
    }

    if (_meshInstance != null)
    {
      // The ring uses a 1x1 plane mesh, so the visible diameter is just scale.
      float diameter = InteractionRadius * 2.0f;
      Vector3 currentScale = _meshInstance.Scale;
      _meshInstance.Scale = new Vector3(diameter, currentScale.Y, diameter);

      // The shader thickness is in normalized UV space.
      // When the ring gets larger, we reduce the shader thickness so the
      // visible world-space band stays about the same width as before.
      if (_meshInstance.MaterialOverride is ShaderMaterial material)
      {
        float thicknessScale = DefaultInteractionRadius / InteractionRadius;
        float normalizedThickness = Mathf.Clamp(DefaultRingThickness * thicknessScale, 0.01f, 0.5f);
        float normalizedPulse = Mathf.Clamp(DefaultPulseStrength * thicknessScale, 0.001f, 0.3f);
        float normalizedEdgeSoftness = Mathf.Clamp(DefaultEdgeSoftness * thicknessScale, 0.001f, 0.1f);
        float normalizedGlowPadding = Mathf.Clamp(DefaultGlowPadding * thicknessScale, 0.001f, 0.3f);
        float normalizedGlowSoftness = Mathf.Clamp(DefaultGlowSoftness * thicknessScale, 0.001f, 0.2f);
        material.SetShaderParameter("ring_thickness", normalizedThickness);
        material.SetShaderParameter("pulse_strength", normalizedPulse);
        material.SetShaderParameter("edge_softness", normalizedEdgeSoftness);
        material.SetShaderParameter("glow_padding", normalizedGlowPadding);
        material.SetShaderParameter("glow_softness", normalizedGlowSoftness);
      }
    }
  }

  public override void _Process(double delta)
  {
    // Editor preview only needs the resized ring/collision shape.
    // Skip wave motion there so scene reloads don't spam null errors.
    if (Engine.IsEditorHint())
      return;

    // Only the visible art should bob with the waves.
    // The root node stays stable so the Area3D interaction volume remains predictable.
    if (VisualRoot == null)
      return;

    _floatingBody ??= new FloatingBody3D(this);

    _floatingBody.Apply(
      VisualRoot,
      WaterPlanePath,
      FloatSampleLength,
      VisualBobStrength,
      WaterSmoothSpeed,
      false,
      0.0f,
      0.0f,
      (float)delta
    );
  }

  private void CacheChildNodes()
  {
    // GetNodeOrNull() is safer in tool scripts because the editor may call us
    // while it is still rebuilding part of the scene tree.
    _meshInstance ??= GetNodeOrNull<MeshInstance3D>("VisualRoot/RingMesh");
    _collisionShape ??= GetNodeOrNull<CollisionShape3D>("Area3D/CollisionShape3D");
  }

  private void EnsureUniqueCollisionShape()
  {
    if (_collisionShape?.Shape == null || _collisionShapeWasDuplicated)
      return;

    // Collision shapes are resources, so scene instances can end up sharing one.
    // Duplicating here keeps each interaction point's collider independent, which
    // makes the exported radius behave the same way as the unique ring material.
    _collisionShape.Shape = (Shape3D)_collisionShape.Shape.Duplicate();
    _collisionShapeWasDuplicated = true;
  }
}
