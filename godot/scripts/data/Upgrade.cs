namespace PiratesQuest.Data;

using Godot;
using Godot.Collections;
using PiratesQuest;

public record Component
{
  public string name;
  public string description;
  public Texture2D icon;
  public Dictionary<InventoryItemType, int> cost;
  public RecordPlayerStatChange[] statChanges;
}