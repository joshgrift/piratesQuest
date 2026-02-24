namespace PiratesQuest.Data;

using Godot.Collections;
using PiratesQuest;

public record Component
{
  public string name;
  public string description;
  // Icon filename (e.g. "acceleration.png") served by the portWebView app
  public string icon;
  public Dictionary<InventoryItemType, int> cost;
  public RecordPlayerStatChange[] statChanges;
}