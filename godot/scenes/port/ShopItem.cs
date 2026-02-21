namespace PiratesQuest;

using Godot;
using PiratesQuest.Data;

public partial class ShopItem : HBoxContainer
{
  [Export] public InventoryItemType ItemType { get; set; } = InventoryItemType.Wood;
  [Export] public int BuyPrice { get; set; } = 0;
  [Export] public int SellPrice { get; set; } = 0;

  [Signal] public delegate void TradeItemEventHandler(InventoryItemType itemType, int amount, int price);

  public override void _Ready()
  {
    var itemLabel = GetNode<Label>("Item");
    itemLabel.Text = ItemType.ToString();

    var buyButton = GetNode<Button>("Buy");
    buyButton.Text = $"[Buy: -{BuyPrice:C}G]";
    buyButton.Pressed += () =>
    {
      EmitSignal(SignalName.TradeItem, (int)ItemType, 1, -BuyPrice);
    };

    var sellButton = GetNode<Button>("Sell");
    sellButton.Text = $"[Sell: +{SellPrice:C}G]";
    sellButton.Pressed += () =>
    {
      EmitSignal(SignalName.TradeItem, (int)ItemType, -1, SellPrice);
    };
  }
}
