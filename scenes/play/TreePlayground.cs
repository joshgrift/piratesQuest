using Godot;
using System;

public partial class TreePlayground : Tree
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Step 1: Set up the Tree columns
		// Columns = 4 means: item name, price, quantity spinner, and buy button
		Columns = 4;

		// Optional: Hide the root item to make it look cleaner
		HideRoot = true;

		// Optional: Set column titles (only visible if ColumnTitlesVisible = true)
		ColumnTitlesVisible = true;
		SetColumnTitle(0, "Item");
		SetColumnTitle(1, "Price");
		SetColumnTitle(2, "Quantity");
		SetColumnTitle(3, "Action");

		// Step 2: Create the root TreeItem
		// Every Tree needs a root item, even if it's hidden
		TreeItem root = CreateItem();

		// Load the icon texture (you can use icon.svg or icons.png from your art folder)
		// For now, we'll use a simple icon - replace with your actual icon paths
		Texture2D itemIcon = GD.Load<Texture2D>("res://icon.svg");

		// Step 3: Add shop items as children of the root
		// Each CreateItem() call creates a new row

		// Item 1: Health Potion
		TreeItem healthPotion = CreateItem(root);
		healthPotion.SetIcon(0, itemIcon);  // Add icon to column 0
		healthPotion.SetText(0, "Health Potion");  // Column 0 = item name
		healthPotion.SetText(1, "50 Gold");        // Column 1 = price
																							 // Column 2 = SpinBox for quantity
		healthPotion.SetCellMode(2, TreeItem.TreeCellMode.Range);  // Make it a range control (SpinBox)
		healthPotion.SetRange(2, 0);  // Set initial value to 0
		healthPotion.SetRangeConfig(2, 0, 99, 1);  // min=0, max=99, step=1
		healthPotion.SetEditable(2, true);  // Make it interactive
																				// Column 3 = Button as text (clickable)
		healthPotion.SetText(3, "Buy");  // Display "Buy" as text
		healthPotion.SetTextAlignment(3, HorizontalAlignment.Center);  // Center the text
		healthPotion.SetIconModulate(0, new Color(0, 1, 0));  // Tint icon green for health potion

		// Item 2: Sword
		TreeItem sword = CreateItem(root);
		sword.SetIcon(0, itemIcon);
		sword.SetText(0, "Iron Sword");
		sword.SetText(1, "150 Gold");
		sword.SetCellMode(2, TreeItem.TreeCellMode.Range);
		sword.SetRange(2, 0);
		sword.SetRangeConfig(2, 0, 99, 1);
		sword.SetEditable(2, true);
		sword.SetText(3, "Buy");
		sword.SetTextAlignment(3, HorizontalAlignment.Center);
		sword.SetIconModulate(0, new Color(0.8f, 0.8f, 0.8f));  // Tint icon silver for sword

		// Item 3: Shield
		TreeItem shield = CreateItem(root);
		shield.SetIcon(0, itemIcon);
		shield.SetText(0, "Wooden Shield");
		shield.SetText(1, "75 Gold");
		shield.SetCellMode(2, TreeItem.TreeCellMode.Range);
		shield.SetRange(2, 0);
		shield.SetRangeConfig(2, 0, 99, 1);
		shield.SetEditable(2, true);
		shield.SetText(3, "Buy");
		shield.SetTextAlignment(3, HorizontalAlignment.Center);
		shield.SetIconModulate(0, new Color(0.6f, 0.4f, 0.2f));  // Tint icon brown for wood

		// Item 4: Map
		TreeItem map = CreateItem(root);
		map.SetIcon(0, itemIcon);
		map.SetText(0, "Treasure Map");
		map.SetText(1, "200 Gold");
		map.SetCellMode(2, TreeItem.TreeCellMode.Range);
		map.SetRange(2, 0);
		map.SetRangeConfig(2, 0, 99, 1);
		map.SetEditable(2, true);
		map.SetText(3, "Buy");
		map.SetTextAlignment(3, HorizontalAlignment.Center);
		map.SetIconModulate(0, new Color(1, 0.9f, 0.6f));  // Tint icon yellowish for old paper

		// Optional: You can also add custom data to items for later use
		// SetMetadata lets you store any object with the item
		healthPotion.SetMetadata(0, 50);  // Store the actual price as an integer
		sword.SetMetadata(0, 150);
		shield.SetMetadata(0, 75);
		map.SetMetadata(0, 200);

		// Connect to the item_selected signal to handle clicks
		ItemSelected += OnItemSelected;

		// Connect to the item_edited signal to detect when SpinBox values change
		ItemEdited += OnItemEdited;
	}

	// Handle when SpinBox (or any editable cell) is changed
	public void OnItemEdited()
	{
		// GetEdited() returns the TreeItem that was just edited
		TreeItem edited = GetEdited();
		if (edited != null)
		{
			// GetEditedColumn() tells us which column was changed
			int column = GetEditedColumn();

			if (column == 2)  // Quantity column
			{
				string itemName = edited.GetText(0);
				int quantity = (int)edited.GetRange(2);  // Get the SpinBox value

				GD.Print($"Quantity changed: {itemName} = {quantity}");
			}
		}
	}

	// Example: Handle when user clicks an item
	public void OnItemSelected()
	{
		// GetSelected() returns the currently selected TreeItem
		TreeItem selected = GetSelected();
		if (selected != null)
		{
			// Check if they clicked column 3 (the "Buy" column)
			int selectedColumn = GetSelectedColumn();

			if (selectedColumn == 3)
			{
				// Treat clicking column 3 as a "buy" action
				string itemName = selected.GetText(0);
				int quantity = (int)selected.GetRange(2);
				int price = (int)selected.GetMetadata(0);
				int totalCost = price * quantity;

				GD.Print($"Buy clicked! Item: {itemName}, Quantity: {quantity}, Total: {totalCost} Gold");
			}
			else
			{
				// Regular item selection
				string itemName = selected.GetText(0);
				string priceText = selected.GetText(1);
				int price = (int)selected.GetMetadata(0);

				GD.Print($"Selected: {itemName} for {priceText} (value: {price})");
			}
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
