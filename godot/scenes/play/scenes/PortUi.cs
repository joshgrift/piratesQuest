using PiratesQuest;
using Godot;
using System;
using PiratesQuest.Data;
using System.Linq;

public partial class PortUi : PanelContainer
{
  [Export] public Tree BuyListTree;
  [Export] public Label TotalBuyLabel;
  [Export] public Button BuyButton;

  [Export] public Tree SellListTree;
  [Export] public Label TotalSellLabel;
  [Export] public Button SellButton;

  [Export] public Tree ShipStatsTree;

  [Export] public Button HealButton;

  [Export] public Control BuyComponentsContainer;
  [Export] public Control ShipComponentsContainer;
  [Export] public Control ActiveShipComponentsContainer;

  [Export] public Label ActiveShipComponentsLabel;

  private TreeItem _buyListRoot;
  private TreeItem _sellListRoot;

  private TreeItem _shipStatsRoot;

  public Player Player;

  private PackedScene _componentScene = GD.Load<PackedScene>("res://scenes/play/scenes/hud_ship_component.tscn");

  private ShopItemData[] _currentItemsForSale;

  public override void _Ready()
  {
    // Buy
    BuyListTree.Columns = 4;
    BuyListTree.HideRoot = true;
    BuyListTree.ColumnTitlesVisible = true;
    BuyListTree.SetColumnTitle(0, "Item");
    BuyListTree.SetColumnTitle(1, "Price");
    BuyListTree.SetColumnTitle(2, "Quantity");
    BuyListTree.SetColumnTitle(3, "Total");
    _buyListRoot = BuyListTree.CreateItem();

    BuyListTree.ItemEdited += OnBuyItemEdited;
    BuyButton.Pressed += OnBuyButtonPressed;

    // Sell
    SellListTree.Columns = 4;
    SellListTree.HideRoot = true;
    SellListTree.ColumnTitlesVisible = true;
    SellListTree.SetColumnTitle(0, "Item");
    SellListTree.SetColumnTitle(1, "Price");
    SellListTree.SetColumnTitle(2, "Quantity");
    SellListTree.SetColumnTitle(3, "Total");
    _sellListRoot = SellListTree.CreateItem();

    SellListTree.ItemEdited += OnSellItemEdited;
    SellButton.Pressed += OnSellButtonPressed;

    // Heal button - repairs the ship for 5 wood + 1 fish per health point
    HealButton.Pressed += OnHealButtonPressed;

    // ShipMenu
    ShipStatsTree.Columns = 4;
    ShipStatsTree.HideRoot = true;
    _shipStatsRoot = ShipStatsTree.CreateItem();
  }

  public void ChangeName(string name)
  {
    GetNode<Label>("MarginContainer/Port/PortName").Text = name;
  }

  /// <summary>
  /// Sets up the buy/sell UI with the given items for sale.
  /// Stores the items so we can refresh after transactions.
  /// </summary>
  public void SetStock(ShopItemData[] itemsForSale)
  {
    _currentItemsForSale = itemsForSale;

    _buyListRoot.CallRecursive("free");
    _buyListRoot = BuyListTree.CreateItem();

    _sellListRoot.CallRecursive("free");
    _sellListRoot = SellListTree.CreateItem();

    foreach (ShopItemData itemData in itemsForSale)
    {
      if (itemData.BuyPrice != 0)
      {
        TreeItem item = BuyListTree.CreateItem(_buyListRoot);
        item.SetText(0, itemData.ItemType.ToString());
        item.SetIcon(0, Icons.GetInventoryIcon(itemData.ItemType));
        item.SetText(1, itemData.BuyPrice.ToString());
        item.SetCellMode(2, TreeItem.TreeCellMode.Range);
        item.SetRange(2, 0);

        // Calculate max affordable quantity based on player's current money
        int playerMoney = Player.GetInventoryCount(InventoryItemType.Coin);
        int maxAffordable = playerMoney / itemData.BuyPrice;
        item.SetRangeConfig(2, 0, maxAffordable, 1);

        item.SetEditable(2, true);
        item.SetText(3, "0");
      }

      if (itemData.SellPrice != 0)
      {
        TreeItem sellItem = SellListTree.CreateItem(_sellListRoot);
        sellItem.SetText(0, itemData.ItemType.ToString());
        sellItem.SetIcon(0, Icons.GetInventoryIcon(itemData.ItemType));
        sellItem.SetText(1, itemData.SellPrice.ToString());
        sellItem.SetCellMode(2, TreeItem.TreeCellMode.Range);
        sellItem.SetRange(2, 0);
        sellItem.SetRangeConfig(2, 0, Player.GetInventoryCount((InventoryItemType)Enum.Parse(typeof(InventoryItemType), sellItem.GetText(0))), 1);
        sellItem.SetEditable(2, true);
        sellItem.SetText(3, "0");
      }

    }
  }

  public void OnBuyItemEdited()
  {
    TreeItem edited = BuyListTree.GetEdited();
    if (edited != null)
    {
      int column = BuyListTree.GetEditedColumn();

      if (column == 2)  // Quantity column
      {
        int quantity = (int)edited.GetRange(2);
        int localTotal = quantity * int.Parse(edited.GetText(1));
        edited.SetText(3, localTotal.ToString());
      }
    }

    // Calculate total across all items
    int buyTotal = 0;
    foreach (TreeItem item in _buyListRoot.GetChildren())
    {
      buyTotal += int.Parse(item.GetText(3));
    }

    TotalBuyLabel.Text = $"Total: {buyTotal}";
  }

  /// <summary>
  /// Called when the player clicks the Buy button.
  /// Purchases all selected items and refreshes the UI to update max quantities.
  /// </summary>
  public void OnBuyButtonPressed()
  {
    foreach (TreeItem item in _buyListRoot.GetChildren())
    {
      int quantity = (int)item.GetRange(2);
      if (quantity > 0)
      {
        Player.UpdateInventory((InventoryItemType)Enum.Parse(typeof(InventoryItemType), item.GetText(0)), quantity, -int.Parse(item.GetText(3)));
        item.SetRange(2, 0);
        item.SetText(3, "0");
      }
    }

    TotalBuyLabel.Text = $"Total: 0";
    RefreshPortUi();
  }

  public void OnSellItemEdited()
  {
    TreeItem edited = SellListTree.GetEdited();
    if (edited != null)
    {
      int column = SellListTree.GetEditedColumn();
      if (column == 2)  // Quantity column
      {
        int quantity = (int)edited.GetRange(2);
        int localTotal = quantity * int.Parse(edited.GetText(1));
        edited.SetText(3, localTotal.ToString());
      }
    }

    // Calculate total across all items
    int sellTotal = 0;
    foreach (TreeItem item in _sellListRoot.GetChildren())
    {
      sellTotal += int.Parse(item.GetText(3));
    }

    TotalSellLabel.Text = $"Total: {sellTotal}";
  }

  public void OnSellButtonPressed()
  {
    foreach (TreeItem item in _sellListRoot.GetChildren())
    {
      int quantity = (int)item.GetRange(2);
      if (quantity > 0)
      {
        Player.UpdateInventory((InventoryItemType)Enum.Parse(typeof(InventoryItemType), item.GetText(0)), -quantity, int.Parse(item.GetText(3)));
        item.SetRange(2, 0);
        item.SetText(3, "0");
      }
    }

    TotalSellLabel.Text = $"Total: 0";
    RefreshPortUi();
  }

  /// <summary>
  /// Called when the player clicks the Heal button.
  /// Heals the ship to full health (or as much as resources allow).
  /// Cost: 5 wood + 1 fish per health point healed.
  /// </summary>
  public void OnHealButtonPressed()
  {
    // Calculate how much health we need to restore
    int healthNeeded = Player.MaxHealth - Player.Health;

    // If already at full health, nothing to do
    if (healthNeeded <= 0)
    {
      GD.Print("Ship is already at full health!");
      return;
    }

    // Check how many resources the player has
    int woodAvailable = Player.GetInventoryCount(InventoryItemType.Wood);
    int fishAvailable = Player.GetInventoryCount(InventoryItemType.Fish);

    // Cost per health point: 5 wood and 1 fish
    const int WoodCostPerHealth = 5;
    const int FishCostPerHealth = 1;

    // Calculate max health we can heal based on each resource
    // (integer division automatically rounds down)
    int maxHealFromWood = woodAvailable / WoodCostPerHealth;
    int maxHealFromFish = fishAvailable / FishCostPerHealth;

    // The actual amount we can heal is limited by:
    // 1. How much health we need
    // 2. How much wood we have (divided by cost)
    // 3. How much fish we have (divided by cost)
    int healthToHeal = Math.Min(healthNeeded, Math.Min(maxHealFromWood, maxHealFromFish));

    // If we can't afford any healing, do nothing
    if (healthToHeal <= 0)
    {
      GD.Print("Not enough resources to heal! Need 5 wood + 1 fish per health point.");
      return;
    }

    // Calculate total resource cost
    int woodCost = healthToHeal * WoodCostPerHealth;
    int fishCost = healthToHeal * FishCostPerHealth;

    // Deduct resources from inventory (negative amount = remove)
    Player.UpdateInventory(InventoryItemType.Wood, -woodCost);
    Player.UpdateInventory(InventoryItemType.Fish, -fishCost);

    // Heal the ship
    Player.Health += healthToHeal;

    // Emit the health update signal so the HUD updates
    Player.EmitSignal(Player.SignalName.HealthUpdate, Player.Health);

    GD.Print($"Healed ship by {healthToHeal} HP. Cost: {woodCost} wood, {fishCost} fish. New health: {Player.Health}/{Player.MaxHealth}");

    // Refresh the UI to show updated inventory
    RefreshPortUi();
  }

  private void RefreshPortUi()
  {
    // Only refresh if we have stored items (i.e., SetStock was called)
    if (_currentItemsForSale != null)
    {
      SetStock(_currentItemsForSale);
    }
    UpdateShipMenu();
  }

  public void UpdateShipMenu()
  {
    // Components
    LoadComponents(BuyComponentsContainer, [.. GameData.Components]);

    LoadComponents(ActiveShipComponentsContainer, [
      .. Player.OwnedComponents.Where(oc => oc.isEquipped).Select(oc => oc.Component)
    ], HudShipComponentStatus.Equipped);

    LoadComponents(ShipComponentsContainer, [
      .. Player.OwnedComponents.Where(oc => !oc.isEquipped).Select(oc => oc.Component)
    ], HudShipComponentStatus.Owned);

    ActiveShipComponentsLabel.Text = $"Active Components ({Player.OwnedComponents.Count(oc => oc.isEquipped)}/{Player.Stats.GetStat(PlayerStat.ComponentCapacity)})";

    // Stats
    _shipStatsRoot.CallRecursive("free");
    _shipStatsRoot = ShipStatsTree.CreateItem();
    foreach (var stat in Player.Stats.GetAllStats())
    {
      TreeItem item = ShipStatsTree.CreateItem(_shipStatsRoot);
      item.SetText(0, stat.Key.ToString());
      item.SetText(1, stat.Value.ToString());
    }

    ShipStatsTree.CustomMinimumSize = new Vector2(0, Player.Stats.GetAllStats().Count * 36);
  }

  private void LoadComponents(Control control, Component[] components, HudShipComponentStatus status = HudShipComponentStatus.ForSale)
  {
    foreach (Node child in control.GetChildren())
    {
      child.QueueFree();
    }

    foreach (var component in components)
    {
      var componentUi = _componentScene.Instantiate<HudShipComponent>();
      componentUi.Player = Player;
      componentUi.SetComponent(component, status);
      componentUi.ButtonClicked += () =>
      {
        switch (status)
        {
          case HudShipComponentStatus.ForSale:
            Player.PurchaseComponent(component);
            break;
          case HudShipComponentStatus.Owned:
            Player.EquipComponent(component);
            break;
          case HudShipComponentStatus.Equipped:
            Player.UnEquipComponent(component);
            break;
        }

        RefreshPortUi();
      };
      control.AddChild(componentUi);
    }
  }
}
