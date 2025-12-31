using Godot;
using Algonquin1;

public partial class Hud : CanvasLayer
{
	[Export] public VBoxContainer InventoryList;

	public override void _Ready()
	{
		var player = GetNode<Player>("/root/main/Player");
		player.InventoryChanged += OnInventoryChanged;
	}

	private void OnInventoryChanged(InventoryItemType itemType, int newAmount)
	{
		var itemLabel = InventoryList.GetNodeOrNull<Label>($"{itemType}Label");

		if (itemLabel != null)
		{
			itemLabel.Text = $"{itemType}: {newAmount}";
		}
		else
		{
			itemLabel = new Label
			{
				Name = $"{itemType}Label",
				Text = $"{itemType}: {newAmount}"
			};
			InventoryList.AddChild(itemLabel);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
