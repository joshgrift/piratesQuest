namespace PiratesQuest.Attributes;

using Godot;
using Godot.Collections;
using PiratesQuest.Data;

/// <summary>
/// Interface for objects that can collect items from a Dropper.
/// </summary>
public interface ICanCollect
{
  bool CollectResource(InventoryItemType item, int amount);
  bool BulkCollect(Dictionary<InventoryItemType, int> items);
  public StringName Name { get; set; }
}