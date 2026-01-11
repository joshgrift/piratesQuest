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
      if (collector.Name != PlayerName)
      {
        collector.BulkCollect(DroppedItems);
        GD.Print($"{collector.Name} collected items from {PlayerName} (${Nickname})'s dead player.");
        QueueFree();
      }
    }
  }
}
