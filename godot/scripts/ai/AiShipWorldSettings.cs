namespace PiratesQuest.AI;

/// <summary>
/// Shared tuning values for where AI ships are allowed to spawn and patrol.
///
/// Keeping these in one place helps the play scene and the AI ship scene agree
/// on what "the playable map" means.
/// </summary>
public static class AiShipWorldSettings
{
  public const float MapHalfExtent = 1100.0f;
  public const float SpawnInset = 85.0f;
  public const float SpawnPaddingFromCorners = 180.0f;

  // Patrol points stay a little inside the edge so ships spend more time in
  // useful water and less time scraping the border.
  public const float PatrolInset = 180.0f;
}
