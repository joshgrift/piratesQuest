using Algonquin1;

/// <summary>
/// Interface for objects that can collect items from a CollectionPoint or other locations.
/// </summary>
public interface ICollector
{
  bool Collect(InventoryItemType item, int amount);
}