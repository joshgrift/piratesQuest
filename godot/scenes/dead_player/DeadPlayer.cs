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
  [Export] public Node3D VisualRoot { get; set; }

  // Use the same water-following helper as ships/buoys so wreckage feels anchored
  // to the sea instead of looking glued in place.
  [ExportGroup("Water Physics")]
  [Export] public NodePath WaterPlanePath { get; set; } = new("/root/Play/WaterPlane");
  [Export] public float FloatSampleLength { get; set; } = 6.0f;
  [Export] public float VisualBobStrength { get; set; } = 0.35f;
  [Export] public float WaterSmoothSpeed { get; set; } = 6.0f;

  private FloatingBody3D _floatingBody;

  public override void _Ready()
  {
    _floatingBody = new FloatingBody3D(this);
    BodyEntered += OnBodyEntered;
    GD.Print($"DeadPlayer for {PlayerName} ({Nickname}) is ready with dropped items: {DroppedItems}");
  }

  public override void _Process(double delta)
  {
    if (VisualRoot == null)
      return;

    // The Area3D follows the waterline so pickup range stays aligned with the loot.
    // The child VisualRoot gets the softer bob/tilt motion for presentation.
    _floatingBody.Apply(
      VisualRoot,
      WaterPlanePath,
      FloatSampleLength,
      VisualBobStrength,
      WaterSmoothSpeed,
      false,
      0.0f,
      0.0f,
      (float)delta
    );
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
