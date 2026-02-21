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

  /// <summary>
  /// Whether this object can currently collect items.
  /// For example, a Player returns false when dead, true when alive.
  /// </summary>
  bool CanCollect { get; }
}