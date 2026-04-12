namespace PiratesQuest.AI;

using Godot;

/// <summary>
/// Small shared helpers for AI-only debug visuals.
///
/// These helpers keep the scene code simple while still letting us reuse the
/// same marker and label setup across different AI ship types later.
/// </summary>
public static class AiDebugVisuals
{
  public static MeshInstance3D CreatePointMarker(string name, Color color, float radius = 1.2f)
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
        Radius = radius,
        Height = radius * 2.0f
      },
      MaterialOverride = material,
      CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
      TopLevel = true,
    };
  }

  public static Label3D CreateStateLabel(string name, Vector3 localOffset)
  {
    return new Label3D
    {
      Name = name,
      Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
      PixelSize = 0.04f,
      FontSize = 28,
      OutlineSize = 6,
      Modulate = new Color(1.0f, 0.96f, 0.78f, 1.0f),
      OutlineModulate = new Color(0, 0, 0, 1),
      NoDepthTest = false,
      Font = GD.Load<FontFile>("res://art/fonts/Texturina/static/Texturina_14pt-Bold.ttf"),
      Position = localOffset,
      Visible = true
    };
  }
}
