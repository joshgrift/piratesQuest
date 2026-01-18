using Godot;
using System;

public partial class InteractionPoint : Node3D
{
  // === EXPORTED PROPERTIES ===
  // These appear in the Godot Inspector and can be set per-instance!
  // [Export] attribute exposes the variable to the editor

  // The color of the interaction ring - change this for different interaction types
  // Examples: Gold for treasure, Blue for ports, Green for resources
  [Export] public Color RingColor { get; set; } = new Color(1.0f, 0.8f, 0.0f, 0.6f);
  [Export] public Area3D InteractionArea;

  // Reference to the mesh that displays the ring
  // We need this to access the shader material
  private MeshInstance3D _meshInstance;

  public override void _Ready()
  {
    // GetNode<T>() finds a child node by path and casts it to type T
    // This is like document.querySelector() in JS but type-safe
    _meshInstance = GetNode<MeshInstance3D>("MeshInstance3D");

    // IMPORTANT: Make the material unique for this instance!
    // Without this, ALL InteractionPoints share the same material.
    // Changing one would change them all (like editing a shared object in JS).
    //
    // Duplicate() creates a deep copy of the resource.
    // Now this instance has its own independent material.
    if (_meshInstance.MaterialOverride != null)
    {
      _meshInstance.MaterialOverride = (Material)_meshInstance.MaterialOverride.Duplicate();
    }

    // Apply the color to the shader
    UpdateRingColor();
  }

  // Called whenever an [Export] property changes in the editor
  // This lets you see color changes in real-time while editing!
  public override void _ValidateProperty(Godot.Collections.Dictionary property)
  {
    // Note: For runtime changes, call UpdateRingColor() after changing RingColor
  }

  /// <summary>
  /// Updates the shader's ring_color parameter to match our RingColor property.
  /// Call this after changing RingColor at runtime.
  /// </summary>
  public void UpdateRingColor()
  {
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

  public override void _Process(double delta)
  {
  }
}
