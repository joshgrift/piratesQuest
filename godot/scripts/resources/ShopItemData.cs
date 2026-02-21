namespace PiratesQuest;

using PiratesQuest.Data;

using Godot;

/// <summary>
/// Represents an item that can be bought or sold at a port.
/// Create instances in the Inspector by adding to the ItemsForSale array.
/// </summary>
[GlobalClass]
public partial class ShopItemData : Resource
{
  /// <summary>
  /// The type of item being sold
  /// </summary>
  [Export] public InventoryItemType ItemType { get; set; } = InventoryItemType.Wood;

  /// <summary>
  /// Price the player pays to buy this item from the port (in Gold)
  /// </summary>
  [Export] public int BuyPrice { get; set; } = 10;

  /// <summary>
  /// Price the port pays when player sells this item (in Gold)
  /// </summary>
  [Export] public int SellPrice { get; set; } = 5;
}
