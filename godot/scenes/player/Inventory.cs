namespace PiratesQuest;

using System.Linq;
using Godot;
using Godot.Collections;
using PiratesQuest.Data;

public partial class Inventory
{
  private readonly Dictionary<InventoryItemType, int> _items = [];

  public int GetItemCount(InventoryItemType itemType)
  {
    if (_items.TryGetValue(itemType, out int count))
    {
      return count;
    }
    else
    {
      return 0;
    }
  }

  public bool UpdateItem(InventoryItemType itemType, int amount)
  {
    var foundItem = _items.TryGetValue(itemType, out int count);
    if (!foundItem)
      _items[itemType] = 0;

    if (count + amount >= 0)
    {
      _items[itemType] += amount;
      return true;
    }

    return false;
  }

  public void SetItem(InventoryItemType itemType, int count)
  {
    _items[itemType] = count;
  }

  public Dictionary<InventoryItemType, int> GetAll()
  {
    return new Dictionary<InventoryItemType, int>(_items);

  }

  public int GetTotalItemCount(InventoryItemType[] excludeItems = null)
  {
    return _items.Where(item => !excludeItems?.Contains(item.Key) ?? true).Sum(item => item.Value);
  }
}