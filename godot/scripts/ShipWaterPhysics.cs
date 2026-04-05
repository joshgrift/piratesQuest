namespace PiratesQuest;

using Godot;

/// <summary>
/// Shared ship-water sampling so player ships and AI ships stay in sync.
/// The physics root follows the average sampled water height, while visuals
/// can apply their own local draft/bob offset on top.
/// </summary>
public static class ShipWaterPhysics
{
  private const float FallbackWaveHeight = 3.0f;
  private const float FallbackWaveSpeed = 0.05f;

  public readonly struct Sample
  {
    public Sample(
      float planeY,
      Vector3 bowWorldPosition,
      Vector3 sternWorldPosition,
      Vector3 centerWorldPosition,
      float bowWaterY,
      float sternWaterY,
      float centerWaterY,
      float averageWaterY,
      float targetPitch
    )
    {
      PlaneY = planeY;
      BowWorldPosition = bowWorldPosition;
      SternWorldPosition = sternWorldPosition;
      CenterWorldPosition = centerWorldPosition;
      BowWaterY = bowWaterY;
      SternWaterY = sternWaterY;
      CenterWaterY = centerWaterY;
      AverageWaterY = averageWaterY;
      TargetPitch = targetPitch;
    }

    public float PlaneY { get; }
    public Vector3 BowWorldPosition { get; }
    public Vector3 SternWorldPosition { get; }
    public Vector3 CenterWorldPosition { get; }
    public float BowWaterY { get; }
    public float SternWaterY { get; }
    public float CenterWaterY { get; }
    public float AverageWaterY { get; }
    public float TargetPitch { get; }
    public float RootWaterlineY => PlaneY + CenterWaterY;
    public float VisualBobOffsetY => CenterWaterY;
  }

  public static bool TrySample(
    MeshInstance3D waterPlane,
    Vector3 worldPosition,
    Vector3 worldForward,
    float shipLength,
    float time,
    out Sample sample
  )
  {
    sample = default;
    if (waterPlane == null)
      return false;

    Vector3 flatForward = worldForward;
    flatForward.Y = 0.0f;

    if (flatForward.LengthSquared() <= Mathf.Epsilon)
      flatForward = Vector3.Forward;
    else
      flatForward = flatForward.Normalized();

    Vector3 bowPos = worldPosition + flatForward * (shipLength / 2.0f);
    Vector3 sternPos = worldPosition - flatForward * (shipLength / 2.0f);

    float bowWaterY = GetWaterHeight(waterPlane, bowPos, time);
    float sternWaterY = GetWaterHeight(waterPlane, sternPos, time);
    float centerWaterY = GetWaterHeight(waterPlane, worldPosition, time);
    float averageWaterY = (bowWaterY + sternWaterY) / 2.0f;
    float targetPitch = -Mathf.Atan2(bowWaterY - sternWaterY, shipLength);

    sample = new Sample(
      waterPlane.GlobalPosition.Y,
      bowPos,
      sternPos,
      worldPosition,
      bowWaterY,
      sternWaterY,
      centerWaterY,
      averageWaterY,
      targetPitch
    );
    return true;
  }

  private static float GetWaterHeight(
    MeshInstance3D waterPlane,
    Vector3 worldPos,
    float time
  )
  {
    if (waterPlane.Mesh is not PlaneMesh waterMesh)
      return 0.0f;

    var shaderMaterial = waterPlane.GetActiveMaterial(0) as ShaderMaterial;

    float shaderWaveHeight = GetShaderFloat(shaderMaterial, "ocean_wave_height", FallbackWaveHeight);
    float shaderWaveSpeed = GetShaderFloat(shaderMaterial, "ocean_wave_speed", FallbackWaveSpeed);
    float shaderWaveFrequency = GetShaderFloat(shaderMaterial, "ocean_wave_frequency", 1.0f);
    float galeStrength = GetShaderFloat(shaderMaterial, "gale_strength", 0.8f);
    Vector3 windDirection3D = GetShaderVector3(shaderMaterial, "wind_direction", Vector3.Right);
    Texture2D waveTexture = GetShaderTexture(shaderMaterial, "ocean_waves_gradient");

    Vector3 localPos = waterPlane.ToLocal(worldPos);
    Vector2 waterSize = waterMesh.Size;
    Vector2 uv = new(
      (localPos.X / waterSize.X) + 0.5f,
      (localPos.Z / waterSize.Y) + 0.5f
    );

    Vector2 windDirection = new(windDirection3D.X, windDirection3D.Z);
    if (windDirection.LengthSquared() <= Mathf.Epsilon)
      windDirection = Vector2.Right;
    else
      windDirection = windDirection.Normalized();

    Vector2 panUv = uv + (windDirection * (time * shaderWaveSpeed));
    float waveSample = GetWaveTextureSample(waveTexture, panUv * shaderWaveFrequency);
    if (waveSample < 0.0f)
      waveSample = 0.5f;

    float displacedHeight = shaderWaveHeight * waveSample * (galeStrength * 1.5f);
    return displacedHeight - (shaderWaveHeight / 2.0f);
  }

  private static float GetShaderFloat(ShaderMaterial material, string parameterName, float fallback)
  {
    if (material == null) return fallback;

    Variant value = material.GetShaderParameter(parameterName);
    return value.VariantType == Variant.Type.Nil ? fallback : value.AsSingle();
  }

  private static Vector3 GetShaderVector3(ShaderMaterial material, string parameterName, Vector3 fallback)
  {
    if (material == null) return fallback;

    Variant value = material.GetShaderParameter(parameterName);
    return value.VariantType == Variant.Type.Nil ? fallback : value.AsVector3();
  }

  private static Texture2D GetShaderTexture(ShaderMaterial material, string parameterName)
  {
    if (material == null) return null;

    Variant value = material.GetShaderParameter(parameterName);
    if (value.VariantType == Variant.Type.Nil)
      return null;

    return value.AsGodotObject() as Texture2D;
  }

  private static float GetWaveTextureSample(Texture2D waveTexture, Vector2 uv)
  {
    if (waveTexture == null)
      return -1.0f;

    Image image = waveTexture.GetImage();
    if (image == null || image.IsEmpty())
      return -1.0f;

    float wrappedX = Mathf.PosMod(uv.X, 1.0f);
    float wrappedY = Mathf.PosMod(uv.Y, 1.0f);
    float imageX = wrappedX * (image.GetWidth() - 1);
    float imageY = wrappedY * (image.GetHeight() - 1);

    int x0 = Mathf.FloorToInt(imageX);
    int y0 = Mathf.FloorToInt(imageY);
    int x1 = Mathf.Min(x0 + 1, image.GetWidth() - 1);
    int y1 = Mathf.Min(y0 + 1, image.GetHeight() - 1);

    float tx = imageX - x0;
    float ty = imageY - y0;

    // Match the shader's filtered texture sampling instead of snapping to the
    // nearest texel. This removes most of the visible stepping/jitter.
    float top = Mathf.Lerp(image.GetPixel(x0, y0).B, image.GetPixel(x1, y0).B, tx);
    float bottom = Mathf.Lerp(image.GetPixel(x0, y1).B, image.GetPixel(x1, y1).B, tx);
    return Mathf.Lerp(top, bottom, ty);
  }
}
