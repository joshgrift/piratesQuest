using Godot;
using Algonquin1;

public partial class Hud : CanvasLayer
{
	[Export] public Container InventoryList;
	[Export] public CanvasItem ReadyToFireContainer;
	[Export] public Label HealthLabel;
	[Export] public Node3D PlayersContainer;

	private Player _player;
	private int _retryCount = 0;
	private const int MaxRetries = 30; // Try for ~1 second

	public override void _Ready()
	{
		if (Multiplayer.IsServer())
		{
			GD.Print("Skipping HUD, acting as server");
			QueueFree();
			return;
		}

		FindLocalPlayer();
	}

	private void FindLocalPlayer()
	{
		// Find the player that we control
		var myPeerId = Multiplayer.GetUniqueId();
		_player = PlayersContainer.GetNodeOrNull<Player>($"player_{myPeerId}");

		if (_player != null)
		{
			_player.InventoryChanged += OnInventoryChanged;

			_player.CannonReadyToFire += () =>
			{
				ReadyToFireContainer.Visible = true;
			};

			_player.CannonFired += () =>
			{
				ReadyToFireContainer.Visible = false;
			};

			_player.HealthUpdate += (newHealth) =>
			{
				HealthLabel.Text = $"Health: {newHealth}";
			};

			GD.Print($"HUD connected to Player{myPeerId}");
		}
		else
		{
			_retryCount++;
			if (_retryCount < MaxRetries)
			{
				// Retry in the next frame
				GetTree().CreateTimer(0.033f).Timeout += FindLocalPlayer;
			}
			else
			{
				GD.PrintErr($"Could not find Player{myPeerId} after {MaxRetries} attempts");
			}
		}
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
}
