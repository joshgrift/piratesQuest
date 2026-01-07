using Godot;
using Algonquin1;
using System.Linq;
using System;
using Godot.Collections;

public partial class Hud : CanvasLayer
{
	[Export] public Tree InventoryList;
	[Export] public CanvasItem ReadyToFireContainer;
	[Export] public PortUi PortUIContainer;
	[Export] public Label HealthLabel;
	[Export] public Node3D PlayersContainer;

	private Player _player;
	private int _retryCount = 0;
	private const int MaxRetries = 30;
	private Dictionary<InventoryItemType, TreeItem> InventoryTreeReferences = [];
	private TreeItem rootInventoryItem = null;

	public override void _Ready()
	{
		//PortUIContainer.Visible = false;
		if (Multiplayer.IsServer())
		{
			GD.Print("Skipping HUD, acting as server");
			QueueFree();
			return;
		}

		var ports = GetTree().GetNodesInGroup("ports");

		GD.Print($"HUD found {ports.Count} ports in the scene");

		foreach (Port port in ports.Cast<Port>())
		{
			GD.Print($"HUD subscribing to port {port.PortName} events");
			port.ShipDocked += OnPlayerEnteredPort;
			port.ShipDeparted += OnPlayerDepartedPort;
		}

		FindLocalPlayer();
	}

	private void OnPlayerEnteredPort(Port port, Player player, Variant payload)
	{
		GD.Print($"Player {player.Name} entered port {port.PortName}");
		if (player.Name == _player.Name)
		{
			PortUIContainer.Player = _player;
			PortUIContainer.Visible = true;
			var payloadDict = (Dictionary)payload;
			PortUIContainer.ChangeName((string)payloadDict["PortName"]);

			// Convert Godot Array to ShopItemData[]
			var godotArray = payloadDict["ItemsForSale"].AsGodotArray();
			var shopItems = new ShopItemData[godotArray.Count];
			for (int i = 0; i < godotArray.Count; i++)
			{
				shopItems[i] = (ShopItemData)godotArray[i];
			}

			GD.Print($"Setting port UI stock with {shopItems.Length} items");
			PortUIContainer.SetStock(shopItems);
		}
	}

	private void OnPlayerDepartedPort(Port port, Player player)
	{
		if (player.Name == _player.Name)
		{
			PortUIContainer.Visible = false;
		}
	}

	private void FindLocalPlayer()
	{
		// Find the player that we control
		var myPeerId = Multiplayer.GetUniqueId();
		_player = PlayersContainer.GetNodeOrNull<Player>($"player_{myPeerId}");

		if (_player != null)
		{
			_player.InventoryChanged += OnInventoryChanged;
			InitializeInventory();

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

	private void InitializeInventory()
	{
		rootInventoryItem = InventoryList.CreateItem();

		InventoryList.Columns = 2;
		InventoryList.MouseFilter = Control.MouseFilterEnum.Ignore;
		InventoryList.HideRoot = true; // Hide the root item and its line

		InventoryList.SetColumnCustomMinimumWidth(0, 32);
		InventoryList.SetColumnCustomMinimumWidth(1, 100);
		InventoryList.CustomMinimumSize = new Vector2(152, 0);
		InventoryList.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;

		InventoryList.AddThemeConstantOverride("draw_relationship_lines", 0);
		InventoryList.AddThemeConstantOverride("draw_guides", 0);
		InventoryList.AddThemeConstantOverride("v_separation", 0); // Remove vertical spacing between items

		var emptyStylebox = new StyleBoxEmpty();
		InventoryList.AddThemeStyleboxOverride("panel", emptyStylebox);
		InventoryList.AddThemeStyleboxOverride("bg", emptyStylebox);

		var inventory = _player.GetInventory();
		foreach (var kvp in inventory)
		{
			OnInventoryChanged(kvp.Key, kvp.Value);
		}
	}

	private void OnInventoryChanged(InventoryItemType itemType, int newAmount)
	{
		if (InventoryTreeReferences.TryGetValue(itemType, out TreeItem itemEntry))
		{
			itemEntry.SetText(1, newAmount.ToString());
			return;
		}
		else
		{
			TreeItem item = InventoryList.CreateItem(rootInventoryItem);
			item.SetIcon(0, InventoryIcons.GetIcon(itemType));
			item.SetText(1, newAmount.ToString());
			InventoryTreeReferences.Add(itemType, item);
		}
	}
}
