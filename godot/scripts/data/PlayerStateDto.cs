namespace PiratesQuest.Data;

using System.Collections.Generic;

public class PlayerStateDto
{
  public Dictionary<string, int> Inventory { get; set; } = new();
  public List<OwnedComponentDto> Components { get; set; } = new();
  public int Health { get; set; }
  public float[] Position { get; set; } = [0, 2, 0];
  public bool IsDead { get; set; }
}

public class OwnedComponentDto
{
  public string Name { get; set; } = string.Empty;
  public bool IsEquipped { get; set; }
}
