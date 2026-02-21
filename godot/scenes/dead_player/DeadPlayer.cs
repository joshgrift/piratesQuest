namespace PiratesQuest;

using Godot;
using PiratesQuest.Data;
using Godot.Collections;
using PiratesQuest.Attributes;

public partial class DeadPlayer : Area3D
{

  [Export] public Dictionary<InventoryItemType, int> DroppedItems = [];
  [Export] public string Nickname = "";
  [Export] public string PlayerName = "";

  public override void _Ready()
  {
    BodyEntered += OnBodyEntered;
    GD.Print($"DeadPlayer for {PlayerName} ({Nickname}) is ready with dropped items: {DroppedItems}");
  }

  private void OnBodyEntered(Node3D body)
  {
    if (body is ICanCollect collector)
    {
      // Check if collector is allowed to collect right now
      // (e.g., Player returns false when dead, true when alive)
      if (!collector.CanCollect)
      {
        GD.Print($"{collector.Name} cannot collect items right now.");
        return;
      }

      collector.BulkCollect(DroppedItems);
      GD.Print($"{collector.Name} collected items from {PlayerName} ({Nickname})'s dead player.");
      QueueFree();
    }
  }
}
