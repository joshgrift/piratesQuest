namespace Algonquin1;

using Godot;
using Godot.Collections;

public class InventoryIcons
{
  private static Texture2D _iconAtlas;

  private static Dictionary<InventoryItemType, int[]> InventoryItemIconMap = new Dictionary<InventoryItemType, int[]>
  {
    { InventoryItemType.Wood, [0, 17] },
    { InventoryItemType.Stone, [1, 17] },
    { InventoryItemType.G, [7, 12] },
    { InventoryItemType.Ammo, [12, 10] }
  };

  public static AtlasTexture GetIcon(InventoryItemType itemType)
  {
    _iconAtlas ??= GD.Load<Texture2D>("res://art/icons.png");

    int iconSize = 32;
    int col = InventoryItemIconMap[itemType][0];
    int row = InventoryItemIconMap[itemType][1];

    GD.Print(col, ", ", row);

    var atlasTexture = new AtlasTexture
    {
      Atlas = _iconAtlas,
      Region = new Rect2(col * iconSize, row * iconSize, iconSize, iconSize)
    };

    return atlasTexture;
  }
}