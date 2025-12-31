using Godot;
using Algonquin1;

public partial class CollectionPoint : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Area3D area = GetNode<Area3D>("Area3D");
		area.BodyEntered += OnBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	// The handler method - must match the signal's signature
	private void OnBodyEntered(Node3D body)
	{
		if (body is ICollector collector)
		{
			collector.Collect(InventoryItemType.Wood, 5);
		}
		// body is whatever entered the area
		GD.Print($"Something entered: {body.Name}");
	}
}
