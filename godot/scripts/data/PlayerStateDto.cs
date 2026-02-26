namespace PiratesQuest.Data;

using System.Collections.Generic;

public class PlayerStateDto
{
  public Dictionary<string, int> Inventory { get; set; } = new();
  public List<OwnedComponentDto> Components { get; set; } = new();
  public int Health { get; set; }
  public float[] Position { get; set; } = [0, 2, 0];
  public bool IsDead { get; set; }
  public int ShipTier { get; set; }

  /// <summary>Null when the player hasn't built a vault yet.</summary>
  public VaultDto Vault { get; set; }
}

public class OwnedComponentDto
{
  public string Name { get; set; } = string.Empty;
  public bool IsEquipped { get; set; }
}

/// <summary>
/// Persisted vault data — stored inside PlayerStateDto so it survives
/// server restarts. Each player may have one vault at a single port.
/// </summary>
public class VaultDto
{
  public string PortName { get; set; } = "";
  public int Level { get; set; } = 1;
  public Dictionary<string, int> Items { get; set; } = new();
}
