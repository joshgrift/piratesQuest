namespace PiratesQuest;

/// <summary>
/// The AI brain produces one of these each physics frame.
/// 
/// It is intentionally tiny:
/// - Throttle controls forward/backward movement
/// - Turn controls steering
/// - FireLeft / FireRight request a broadside
/// 
/// Keeping this small makes it easy to swap different AI approaches later.
/// </summary>
public sealed class AiShipControlInput
{
  public float Throttle { get; set; }
  public float Turn { get; set; }
  public bool FireLeft { get; set; }
  public bool FireRight { get; set; }
}
