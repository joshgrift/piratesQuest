namespace PiratesQuest;

using Godot;
using Godot.Collections;

using PiratesQuest.Data;

public class Icons
{
  private static bool loadedIcons = false;

  private static Dictionary<InventoryItemType, Texture2D> InventoryItemIconMap = [];

  private static void LoadIcons()
  {
    InventoryItemIconMap = new Dictionary<InventoryItemType, Texture2D>
      {
        { InventoryItemType.Wood, GD.Load<Texture2D>("res://art/inventory/wood.png") },
        { InventoryItemType.Iron, GD.Load<Texture2D>("res://art/inventory/iron.png") },
        { InventoryItemType.Coin, GD.Load<Texture2D>("res://art/inventory/coin.png") },
        { InventoryItemType.CannonBall, GD.Load<Texture2D>("res://art/inventory/cannon_ball.png") },
        { InventoryItemType.Fish, GD.Load<Texture2D>("res://art/inventory/fish.png") },
        { InventoryItemType.Tea, GD.Load<Texture2D>("res://art/inventory/tea.png") },
        { InventoryItemType.Trophy, GD.Load<Texture2D>("res://art/inventory/trophy.png") }
      };

    loadedIcons = true;
  }

  public static Texture2D GetInventoryIcon(InventoryItemType itemType)
  {
    if (!loadedIcons)
      LoadIcons();

    return InventoryItemIconMap[itemType];
  }
}