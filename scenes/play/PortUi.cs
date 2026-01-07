using PiratesQuest;
using Godot;
using System;
using Godot.Collections;

public partial class PortUi : PanelContainer
{
	[Export] public Tree BuyListTree;
	[Export] public Label TotalBuyLabel;
	[Export] public Button BuyButton;

	[Export] public Tree SellListTree;
	[Export] public Label TotalSellLabel;
	[Export] public Button SellButton;

	private TreeItem _buyListRoot;

	private TreeItem _sellListRoot;

	public Player Player;

	public override void _Ready()
	{
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
	}

	public void ChangeName(string name)
	{
		GetNode<Label>("MarginContainer/Port/PortName").Text = name;
	}

	public void SetStock(ShopItemData[] itemsForSale)
	{
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
				item.SetText(1, itemData.BuyPrice.ToString());
				item.SetCellMode(2, TreeItem.TreeCellMode.Range);
				item.SetRange(2, 0);
				item.SetRangeConfig(2, 0, 99, 1);
				item.SetEditable(2, true);
				item.SetText(3, "0");
			}

			if (itemData.SellPrice != 0)
			{
				TreeItem sellItem = SellListTree.CreateItem(_sellListRoot);
				sellItem.SetText(0, itemData.ItemType.ToString());
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
		int buyTotal = 0;
		TreeItem edited = BuyListTree.GetEdited();
		if (edited != null)
		{
			int column = BuyListTree.GetEditedColumn();

			if (column == 2)  // Quantity column
			{
				int quantity = (int)edited.GetRange(2);
				int localTotal = quantity * int.Parse(edited.GetText(1));
				buyTotal += localTotal;
				edited.SetText(3, localTotal.ToString());
			}
		}

		TotalBuyLabel.Text = $"Total: {buyTotal}";
	}

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
	}

	public void OnSellItemEdited()
	{
		int sellTotal = 0;
		TreeItem edited = SellListTree.GetEdited();
		if (edited != null)
		{
			int column = SellListTree.GetEditedColumn();
			if (column == 2)  // Quantity column
			{
				int quantity = (int)edited.GetRange(2);
				int localTotal = quantity * int.Parse(edited.GetText(1));
				sellTotal += localTotal;
				edited.SetText(3, localTotal.ToString());
			}
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
	}
}
