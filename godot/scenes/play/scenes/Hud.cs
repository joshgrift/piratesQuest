using Godot;
using PiratesQuest;
using System;
using System.Linq;
using System.Text.Json;
using PiratesQuest.Data;
using Godot.Collections;
using System.Collections.Generic;

public partial class Hud : Control
{
  [Export] public Tree InventoryList;
  [Export] public PortUi PortUIContainer;
  [Export] public Label HealthLabel;
  [Export] public Node3D PlayersContainer;
  [Export] public Tree LeaderboardTree;

  [Export] public VBoxContainer StatusListContainer;

  private Player _player;
  private int _retryCount = 0;
  private const int MaxRetries = 30;
  // Using Godot Dictionary for tree references (needed for Godot interop)
  private Godot.Collections.Dictionary<InventoryItemType, TreeItem> InventoryTreeReferences = [];
  private TreeItem rootInventoryItem = null;

  // Track pending changes for each item type to handle rapid inventory updates
  // Key: item type, Value: tuple of (accumulated change, timer reference)
  // Using System.Collections.Generic.Dictionary because Godot.Dictionary doesn't support C# tuples
  private System.Collections.Generic.Dictionary<InventoryItemType, (int accumulatedChange, SceneTreeTimer timer)> _pendingChanges = new();

  // WebView (godot_wry) — always-visible panel on the right third of the screen.
  // It's a native OS overlay, so we manually size it in physical window pixels.
  private Node _webView;
  private bool _webViewCreated;

  private static readonly System.Collections.Generic.Dictionary<InventoryItemType, int> _webViewShopPrices = new()
  {
    { InventoryItemType.CannonBall, 2 },
    { InventoryItemType.Wood, 4 },
    { InventoryItemType.Fish, 5 },
  };

  public override void _Ready()
  {
    PortUIContainer.Visible = false;
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

    CallDeferred(MethodName.FindLocalPlayer);

    var emptyStylebox = new StyleBoxEmpty();
    LeaderboardTree.AddThemeStyleboxOverride("panel", emptyStylebox);
    LeaderboardTree.AddThemeStyleboxOverride("bg", emptyStylebox);
    LeaderboardTree.AddThemeConstantOverride("draw_relationship_lines", 0);
    LeaderboardTree.AddThemeConstantOverride("draw_guides", 0);
    LeaderboardTree.AddThemeConstantOverride("v_separation", 0);
    LeaderboardTree.MouseFilter = Control.MouseFilterEnum.Ignore;
    LeaderboardTree.HideRoot = true;

    UpdateLeaderboard();
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
      PortUIContainer.UpdateShipMenu();
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
    if (PlayersContainer == null)
    {
      GD.PrintErr("PlayersContainer is not set in HUD");
      return;
    }

    // If the client has been disconnected (for example join rejected),
    // Multiplayer.GetUniqueId() will throw engine errors. Stop retries safely.
    if (!IsMultiplayerActive())
    {
      GD.Print("HUD stopped player lookup because multiplayer is not active.");
      return;
    }

    // Find the player that we control
    var myPeerId = Multiplayer.GetUniqueId();
    GD.Print($"{PlayersContainer.GetChildCount()}");
    _player = PlayersContainer.GetNodeOrNull<Player>($"player_{myPeerId}");

    if (_player != null)
    {
      _player.InventoryChanged += OnInventoryChanged;
      InitializeInventory();

      _player.CannonReadyToFire += () =>
      {
        StatusListContainer.GetNode<Control>("ReadyToFire").Visible = true;
      };

      _player.CannonFired += () =>
      {
        StatusListContainer.GetNode<Control>("ReadyToFire").Visible = false;
      };

      _player.HealthUpdate += (newHealth) =>
      {
        HealthLabel.Text = $"{newHealth}";
      };

      GD.Print($"HUD connected to Player{myPeerId}");

      CreateWebView();
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

  private bool IsMultiplayerActive()
  {
    var peer = Multiplayer.MultiplayerPeer;
    if (peer == null)
    {
      return false;
    }

    return peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
  }

  private void UpdateLeaderboard()
  {
    LeaderboardTree.Clear();
    LeaderboardTree.Columns = 2;
    LeaderboardTree.SetColumnCustomMinimumWidth(0, 32);
    LeaderboardTree.SetColumnCustomMinimumWidth(1, 100);

    var rootItem = LeaderboardTree.CreateItem();

    var players = PlayersContainer.GetChildren().OfType<Player>()
      .OrderByDescending(p => p.GetInventoryCount(InventoryItemType.Trophy));

    // Get the local player's nickname to compare against
    // _player is the reference to our local player (the one we control)
    string localPlayerNickname = _player?.Nickname;

    foreach (var leaderboardPlayer in players)
    {
      if (leaderboardPlayer.Nickname == "" || leaderboardPlayer.Nickname == null)
        continue;
      var item = LeaderboardTree.CreateItem(rootItem);
      item.SetText(0, leaderboardPlayer.TrophyCount.ToString());
      item.SetIcon(0, Icons.GetInventoryIcon(InventoryItemType.Trophy));
      item.SetText(1, leaderboardPlayer.Nickname);

      // Highlight the current player's row with a green tint
      // TreeItem.SetCustomColor sets the text color for a specific column
      // We check if this player is us by comparing nicknames
      if (leaderboardPlayer.Nickname == localPlayerNickname)
      {
        // Color is created with RGB values from 0-1 (not 0-255)
        // This is a nice green color: slightly muted so it's readable
        Color greenTint = new Color(0.4f, 0.9f, 0.4f);
        item.SetCustomColor(0, greenTint);  // Trophy count column
        item.SetCustomColor(1, greenTint);  // Name column
      }
    }

    GetTree().CreateTimer(5.0f).Timeout += UpdateLeaderboard;
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
      OnInventoryChanged(kvp.Key, kvp.Value, 0);
    }
  }

  private void OnInventoryChanged(InventoryItemType itemType, int newAmount, int change)
  {
    PushInventoryToWebView();

    if (_player.isLimitedByCapacity)
    {
      StatusListContainer.GetNode<Control>("Heavy").Visible = true;
    }
    else
    {
      StatusListContainer.GetNode<Control>("Heavy").Visible = false;
    }

    if (InventoryTreeReferences.TryGetValue(itemType, out TreeItem itemEntry))
    {
      // Calculate the accumulated change (handles rapid inventory updates)
      int totalChange = change;
      if (_pendingChanges.TryGetValue(itemType, out var pending))
      {
        // Add to existing accumulated change
        totalChange = pending.accumulatedChange + change;
      }

      string increase = "";

      if (totalChange > 0)
      {
        increase = $"    (+{totalChange})";
        itemEntry.SetCustomColor(1, new Color(0.4f, 0.9f, 0.4f));
      }
      else if (totalChange < 0)
      {
        increase = $"    ({totalChange})";
        itemEntry.SetCustomColor(1, new Color(0.9f, 0.4f, 0.4f));
      }

      // Start or reset the timer for this item type
      StartColorResetTimer(itemType, itemEntry, totalChange);

      itemEntry.SetText(1, newAmount.ToString() + increase);
      return;
    }
    else
    {
      TreeItem item = InventoryList.CreateItem(rootInventoryItem);
      item.SetIcon(0, Icons.GetInventoryIcon(itemType));
      item.SetText(1, newAmount.ToString());
      InventoryTreeReferences.Add(itemType, item);
    }
  }

  /// <summary>
  /// Creates or resets a timer that clears the color/change indicator after 2 seconds.
  /// If a timer already exists for this item type, we disconnect the old callback
  /// and create a new timer, effectively "resetting" the countdown.
  /// </summary>
  /// <param name="itemType">The inventory item type (used as key to track timers)</param>
  /// <param name="item">The TreeItem to reset</param>
  /// <param name="accumulatedChange">The total accumulated change to track</param>
  private void StartColorResetTimer(InventoryItemType itemType, TreeItem item, int accumulatedChange)
  {
    // If there's already a pending timer for this item, we need to "cancel" it
    // SceneTreeTimer can't be truly cancelled, but we can disconnect its callback
    // and let it expire harmlessly
    if (_pendingChanges.TryGetValue(itemType, out var existing))
    {
      // Remove from tracking - the old timer will fire but do nothing
      // because ResetItemDisplay checks if the timer is still the active one
      _pendingChanges.Remove(itemType);
    }

    // Create a new timer
    SceneTreeTimer timer = GetTree().CreateTimer(2.2f);

    // Store the pending change info
    _pendingChanges[itemType] = (accumulatedChange, timer);

    // Connect the timeout - capture the timer reference to verify it's still active
    timer.Timeout += () =>
    {
      // Only reset if this timer is still the active one for this item type
      // (prevents stale timers from resetting after a newer change came in)
      if (_pendingChanges.TryGetValue(itemType, out var current) && current.timer == timer)
      {
        item.ClearCustomColor(1);
        string currentText = item.GetText(1);
        string numberOnly = currentText.Split(' ')[0];
        item.SetText(1, numberOnly);

        // Clean up the tracking entry
        _pendingChanges.Remove(itemType);
      }
    };
  }

  // ── WebView (always-visible right panel) ────────────────────────────

  private void CreateWebView()
  {
    if (_webViewCreated) return;
    _webViewCreated = true;

    if (!ClassDB.ClassExists("WebView"))
    {
      GD.Print("WebView (godot_wry) plugin not available — skipping");
      return;
    }

    var scene = GD.Load<PackedScene>("res://scenes/play/scenes/webview_node.tscn");
    if (scene == null) return;

    var instance = scene.Instantiate();
    if (instance is not Control control)
    {
      instance.QueueFree();
      return;
    }

    _webView = control;
    _webView.Set("url", $"{Configuration.ApiBaseUrl}/fragments/info-panel/");
    _webView.Set("full_window_size", false);
    _webView.Set("transparent", false);
    _webView.Set("devtools", true);
    _webView.Set("forward_input_events", true);

    AddChild(_webView);

    // Position/size the native overlay and keep it in sync on resize.
    // Deferred so the engine finishes updating viewport/canvas first.
    CallDeferred(MethodName.SyncWebViewSize);
    GetTree().Root.SizeChanged += OnWindowResized;

    _webView.Connect("ipc_message", new Callable(this, MethodName.OnIpcMessage));

    // Send initial inventory
    PushInventoryToWebView();
    GD.Print("HUD: WebView created (right 1/3)");
  }

  private void OnWindowResized()
  {
    CallDeferred(MethodName.SyncWebViewSize);
  }

  /// <summary>
  /// Positions the native webview overlay as the right 1/3 of the window.
  /// godot_wry reads get_screen_position() (= canvas_scale * Position) for
  /// the overlay's physical position, and get_size() for its physical size.
  /// We derive the virtual position from the canvas transform so this works
  /// regardless of stretch mode or aspect ratio.
  /// </summary>
  private void SyncWebViewSize()
  {
    if (_webView is not Control wv) return;

    var windowSize = DisplayServer.WindowGetSize();

    // The canvas transform maps virtual coords → physical window coords.
    // Invert the scale to go from physical → virtual.
    var canvasXform = GetViewport().GetCanvasTransform();
    float scaleX = canvasXform.X.X;
    float scaleY = canvasXform.Y.Y;

    // Virtual position so that get_screen_position() returns the correct
    // physical position (right 1/3 boundary).
    wv.Position = new Vector2(windowSize.X * 2f / 3f / scaleX, 0);

    // Physical pixel size — godot_wry passes get_size() straight to the OS.
    wv.Size = new Vector2(windowSize.X / 3f, windowSize.Y);
  }

  private void OnIpcMessage(string message)
  {
    var parsed = Json.ParseString(message);
    if (parsed.VariantType != Variant.Type.Dictionary) return;

    var dict = parsed.AsGodotDictionary();
    string action = dict.ContainsKey("action") ? dict["action"].AsString() : "";

    if (action == "purchase")
    {
      string itemType = dict["itemType"].AsString();
      int quantity = dict["quantity"].AsInt32();
      HandleWebViewPurchase(itemType, quantity);
    }
  }

  private void PushInventoryToWebView()
  {
    if (_player == null || _webView == null) return;

    var items = new System.Collections.Generic.Dictionary<string, int>();
    foreach (InventoryItemType type in Enum.GetValues(typeof(InventoryItemType)))
    {
      items[type.ToString()] = _player.GetInventoryCount(type);
    }

    var json = JsonSerializer.Serialize(items);
    _webView.Call("eval",
      $"window.updateInventory && window.updateInventory({json})");
  }

  private void HandleWebViewPurchase(string itemType, int quantity)
  {
    if (_player == null) return;

    if (!Enum.TryParse<InventoryItemType>(itemType, out var type)) return;
    if (!_webViewShopPrices.TryGetValue(type, out int unitPrice)) return;

    int totalCost = unitPrice * quantity;
    bool success = _player.UpdateInventory(type, quantity, -totalCost);

    GD.Print(success
      ? $"HUD: Purchased {quantity}x {itemType} for {totalCost} coin"
      : $"HUD: Purchase failed — not enough coin");
  }

  public override void _ExitTree()
  {
    if (_webViewCreated)
      GetTree().Root.SizeChanged -= OnWindowResized;
  }
}
