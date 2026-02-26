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
  [Export] public Label HealthLabel;
  [Export] public Node3D PlayersContainer;
  [Export] public Tree LeaderboardTree;

  [Export] public VBoxContainer StatusListContainer;

  private Player _player;
  private int _retryCount = 0;
  private const int MaxRetries = 30;
  private Godot.Collections.Dictionary<InventoryItemType, TreeItem> InventoryTreeReferences = [];
  private TreeItem rootInventoryItem = null;

  private System.Collections.Generic.Dictionary<InventoryItemType, (int accumulatedChange, SceneTreeTimer timer)> _pendingChanges = new();

  // ── WebView (port-only panel, right 1/3 of screen) ──────────────
  private Node _webView;
  private bool _webViewCreated;
  // True once the React app has sent the "ready" IPC message
  private bool _webViewReady;
  // If the player docks before the webview is ready, we queue the open
  private bool _pendingOpen;
  // True while the port UI is visible on screen
  private bool _portOpen;
  // Currently docked port data (null when not in port)
  private ShopItemData[] _currentPortItems;
  private string _currentPortName;
  // camelCase JSON for the webview
  private static readonly JsonSerializerOptions _jsonOpts = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
  };

  public override void _Ready()
  {
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

  // ── Port Dock / Depart ───────────────────────────────────────────

  private void OnPlayerEnteredPort(Port port, Player player, Variant payload)
  {
    if (_player == null || player.Name != _player.Name) return;
    GD.Print($"Player {player.Name} entered port {port.PortName}");

    var payloadDict = (Dictionary)payload;
    _currentPortName = (string)payloadDict["PortName"];

    var godotArray = payloadDict["ItemsForSale"].AsGodotArray();
    _currentPortItems = new ShopItemData[godotArray.Count];
    for (int i = 0; i < godotArray.Count; i++)
      _currentPortItems[i] = (ShopItemData)godotArray[i];

    EnsureWebViewCreated();

    if (_webViewReady)
    {
      ShowWebView();
      PushPortState("openPort");
    }
    else
    {
      _pendingOpen = true;
    }
  }

  private void OnPlayerDepartedPort(Port port, Player player)
  {
    if (_player == null || player.Name != _player.Name) return;
    GD.Print($"Player {player.Name} departed port");

    _currentPortItems = null;
    _currentPortName = null;
    _pendingOpen = false;

    if (_webView != null)
    {
      // Return keyboard focus to the game window
      _webView.Call("focus_parent");

      _webView.Call("eval", "window.closePort && window.closePort()");
      // Wait for the CSS slide-out animation (400ms) then hide the overlay
      GetTree().CreateTimer(0.45f).Timeout += HideWebView;
    }
  }

  // ── Find Local Player ────────────────────────────────────────────

  private void FindLocalPlayer()
  {
    if (PlayersContainer == null)
    {
      GD.PrintErr("PlayersContainer is not set in HUD");
      return;
    }

    if (!IsMultiplayerActive())
    {
      GD.Print("HUD stopped player lookup because multiplayer is not active.");
      return;
    }

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

      // Pre-create the webview off-screen so the page is loaded before first dock
      EnsureWebViewCreated();
    }
    else
    {
      _retryCount++;
      if (_retryCount < MaxRetries)
        GetTree().CreateTimer(0.033f).Timeout += FindLocalPlayer;
      else
        GD.PrintErr($"Could not find Player{myPeerId} after {MaxRetries} attempts");
    }
  }

  private bool IsMultiplayerActive()
  {
    var peer = Multiplayer.MultiplayerPeer;
    if (peer == null) return false;
    return peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected;
  }

  // ── Leaderboard ──────────────────────────────────────────────────

  private void UpdateLeaderboard()
  {
    LeaderboardTree.Clear();
    LeaderboardTree.Columns = 2;
    LeaderboardTree.SetColumnCustomMinimumWidth(0, 32);
    LeaderboardTree.SetColumnCustomMinimumWidth(1, 100);

    var rootItem = LeaderboardTree.CreateItem();

    var players = PlayersContainer.GetChildren().OfType<Player>()
      .OrderByDescending(p => p.GetInventoryCount(InventoryItemType.Trophy));

    string localPlayerNickname = _player?.Nickname;

    foreach (var leaderboardPlayer in players)
    {
      if (leaderboardPlayer.Nickname == "" || leaderboardPlayer.Nickname == null)
        continue;
      var item = LeaderboardTree.CreateItem(rootItem);
      item.SetText(0, leaderboardPlayer.TrophyCount.ToString());
      item.SetIcon(0, Icons.GetInventoryIcon(InventoryItemType.Trophy));
      item.SetText(1, leaderboardPlayer.Nickname);

      if (leaderboardPlayer.Nickname == localPlayerNickname)
      {
        Color greenTint = new Color(0.4f, 0.9f, 0.4f);
        item.SetCustomColor(0, greenTint);
        item.SetCustomColor(1, greenTint);
      }
    }

    GetTree().CreateTimer(5.0f).Timeout += UpdateLeaderboard;
  }

  // ── HUD Inventory Display ────────────────────────────────────────

  private void InitializeInventory()
  {
    rootInventoryItem = InventoryList.CreateItem();

    InventoryList.Columns = 2;
    InventoryList.MouseFilter = Control.MouseFilterEnum.Ignore;
    InventoryList.HideRoot = true;

    InventoryList.SetColumnCustomMinimumWidth(0, 32);
    InventoryList.SetColumnCustomMinimumWidth(1, 100);
    InventoryList.CustomMinimumSize = new Vector2(152, 0);
    InventoryList.SizeFlagsHorizontal = Control.SizeFlags.ShrinkBegin;

    InventoryList.AddThemeConstantOverride("draw_relationship_lines", 0);
    InventoryList.AddThemeConstantOverride("draw_guides", 0);
    InventoryList.AddThemeConstantOverride("v_separation", 0);

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
    // Push updated state to port webview when docked
    if (_currentPortItems != null)
      PushPortState("updateState");

    if (_player.isLimitedByCapacity)
      StatusListContainer.GetNode<Control>("Heavy").Visible = true;
    else
      StatusListContainer.GetNode<Control>("Heavy").Visible = false;

    if (InventoryTreeReferences.TryGetValue(itemType, out TreeItem itemEntry))
    {
      int totalChange = change;
      if (_pendingChanges.TryGetValue(itemType, out var pending))
        totalChange = pending.accumulatedChange + change;

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

  private void StartColorResetTimer(InventoryItemType itemType, TreeItem item, int accumulatedChange)
  {
    if (_pendingChanges.TryGetValue(itemType, out var _))
      _pendingChanges.Remove(itemType);

    SceneTreeTimer timer = GetTree().CreateTimer(2.2f);
    _pendingChanges[itemType] = (accumulatedChange, timer);

    timer.Timeout += () =>
    {
      if (_pendingChanges.TryGetValue(itemType, out var current) && current.timer == timer)
      {
        item.ClearCustomColor(1);
        string currentText = item.GetText(1);
        string numberOnly = currentText.Split(' ')[0];
        item.SetText(1, numberOnly);
        _pendingChanges.Remove(itemType);
      }
    };
  }

  // ── WebView (port-only right panel) ──────────────────────────────

  private void EnsureWebViewCreated()
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
    _webView.Set("url", Configuration.WebViewUrl);
    _webView.Set("full_window_size", false);
    _webView.Set("transparent", false);
    _webView.Set("devtools", true);
    _webView.Set("forward_input_events", true);
    _webView.Set("focused_when_created", false);

    AddChild(_webView);

    CallDeferred(MethodName.SyncWebViewSize);
    GetTree().Root.SizeChanged += OnWindowResized;
    _webView.Connect("ipc_message", new Callable(this, MethodName.OnIpcMessage));

    // Start off-screen so the page loads in the background
    HideWebView();

    GD.Print("HUD: WebView created off-screen (preloading)");
  }

  private void OnWindowResized()
  {
    CallDeferred(MethodName.SyncWebViewSize);
  }

  /// <summary>
  /// Positions the native webview overlay as the right 1/3 of the window.
  /// godot_wry uses physical pixels for its native OS overlay, so we
  /// convert from the canvas stretch mode's virtual coordinates.
  /// </summary>
  private void SyncWebViewSize()
  {
    if (_webView is not Control wv) return;

    var windowSize = DisplayServer.WindowGetSize();

    // Always set a valid size so the page can load/render
    wv.Size = new Vector2(windowSize.X / 3f, windowSize.Y);

    if (_portOpen)
    {
      var canvasXform = GetViewport().GetCanvasTransform();
      float scaleX = canvasXform.X.X;
      wv.Position = new Vector2(windowSize.X * 2f / 3f / scaleX, 0);
    }
    else
    {
      wv.Position = new Vector2(99999, 0);
    }
  }

  /// <summary>
  /// Moves the webview overlay on-screen (right 1/3).
  /// </summary>
  private void ShowWebView()
  {
    _portOpen = true;
    CallDeferred(MethodName.SyncWebViewSize);

    // Keep keyboard focus on Godot so movement key-up events
    // are never missed. The webview only needs mouse clicks.
    if (_webView != null)
      _webView.Call("focus_parent");
  }

  /// <summary>
  /// Moves the webview overlay off-screen so it's invisible but still loaded.
  /// </summary>
  private void HideWebView()
  {
    _portOpen = false;
    CallDeferred(MethodName.SyncWebViewSize);
  }

  // ── Build & push full port state to webview ──────────────────────

  private PortStateDto BuildPortState()
  {
    var inventory = new System.Collections.Generic.Dictionary<string, int>();
    foreach (InventoryItemType type in Enum.GetValues(typeof(InventoryItemType)))
      inventory[type.ToString()] = _player.GetInventoryCount(type);

    var shopItems = (_currentPortItems ?? [])
      .Select(si => new ShopItemDto(si.ItemType.ToString(), si.BuyPrice, si.SellPrice))
      .ToArray();

    var components = GameData.Components
      .Select(c => new ComponentDto(
        c.name, c.description, c.icon,
        c.cost.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
        c.statChanges.Select(sc => new StatChangeDto(
          sc.Stat.ToString(), sc.Modifier.ToString(), sc.Value
        )).ToArray()
      ))
      .ToArray();

    var ownedComponents = _player.OwnedComponents
      .Select(oc => new OwnedComponentDto { Name = oc.Component.name, IsEquipped = oc.isEquipped })
      .ToArray();

    var stats = new System.Collections.Generic.Dictionary<string, float>();
    foreach (var kvp in _player.Stats.GetAllStats())
      stats[kvp.Key.ToString()] = kvp.Value;

    // Build vault snapshot (null if player hasn't built one yet)
    VaultStateDto vaultState = null;
    if (_player.VaultPortName != null)
    {
      bool isHere = _player.VaultPortName == (_currentPortName ?? "");
      var vaultItems = new System.Collections.Generic.Dictionary<string, int>();
      foreach (var kvp in _player.VaultItems)
        vaultItems[kvp.Key.ToString()] = kvp.Value;

      vaultState = new VaultStateDto
      {
        PortName = _player.VaultPortName,
        Level = _player.VaultLevel,
        Items = vaultItems,
        IsHere = isHere,
        ItemCapacity = Player.VaultItemCapacity[_player.VaultLevel],
        GoldCapacity = Player.VaultGoldCapacity[_player.VaultLevel],
      };
    }

    return new PortStateDto
    {
      PortName = _currentPortName ?? "",
      ItemsForSale = shopItems,
      Inventory = inventory,
      Components = components,
      OwnedComponents = ownedComponents,
      Stats = stats,
      Health = _player.Health,
      MaxHealth = _player.MaxHealth,
      ComponentCapacity = (int)_player.Stats.GetStat(PlayerStat.ComponentCapacity),
      ShipTier = _player.ShipTier,
      ShipTiers = GameData.ShipTiers
        .Select(t => new ShipTierDto(
          t.Name, t.Description, t.ComponentSlots,
          t.Cost.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value)
        ))
        .ToArray(),
      IsCreative = Configuration.IsCreative,
      Vault = vaultState,
    };
  }

  /// <summary>
  /// Serializes the current port state and pushes it to the webview
  /// by calling the named window function (openPort or updateState).
  /// </summary>
  private void PushPortState(string windowFunction)
  {
    if (_player == null || _webView == null) return;

    var dto = BuildPortState();
    var json = JsonSerializer.Serialize(dto, _jsonOpts);
    _webView.Call("eval",
      $"window.{windowFunction} && window.{windowFunction}({json})");
  }

  // ── Receive typed IPC messages from React ────────────────────────

  private void OnIpcMessage(string message)
  {
    if (_player == null) return;

    IpcMessage msg;
    try
    {
      msg = JsonSerializer.Deserialize<IpcMessage>(message, _jsonOpts);
    }
    catch (Exception ex)
    {
      GD.PrintErr($"HUD: Failed to parse IPC message: {ex.Message}");
      return;
    }

    switch (msg)
    {
      case ReadyMessage:
        HandleWebViewReady();
        return;
      case FocusParentMessage:
        _webView?.Call("focus_parent");
        return;
      case BuyItemsMessage buy:
        HandleBuyItems(buy);
        break;
      case SellItemsMessage sell:
        HandleSellItems(sell);
        break;
      case PurchaseComponentMessage pc:
        HandlePurchaseComponent(pc);
        break;
      case EquipComponentMessage eq:
        HandleEquipComponent(eq);
        break;
      case UnequipComponentMessage ue:
        HandleUnequipComponent(ue);
        break;
      case HealMessage:
        HandleHeal();
        break;
      case SetInventoryMessage si:
        HandleSetInventory(si);
        break;
      case ClearComponentsMessage:
        HandleClearComponents();
        break;
      case SetHealthMessage sh:
        HandleSetHealth(sh);
        break;
      case UpgradeShipMessage:
        HandleUpgradeShip();
        break;
      case BuildVaultMessage:
        HandleBuildVault();
        break;
      case UpgradeVaultMessage:
        HandleUpgradeVault();
        break;
      case VaultDepositMessage vd:
        HandleVaultDeposit(vd);
        break;
      case VaultWithdrawMessage vw:
        HandleVaultWithdraw(vw);
        break;
      case SetShipTierMessage st:
        HandleSetShipTier(st);
        break;
      case SetVaultMessage sv:
        HandleSetVault(sv);
        break;
      case DeleteVaultMessage:
        HandleDeleteVault();
        break;
      default:
        GD.Print($"HUD: Unknown IPC message type");
        break;
    }

    // After every action, push refreshed state to the webview
    if (_currentPortItems != null)
      PushPortState("updateState");
  }

  private void HandleWebViewReady()
  {
    GD.Print("HUD: WebView ready");
    _webViewReady = true;

    // If the player docked before the page finished loading, open now
    if (_pendingOpen && _currentPortItems != null)
    {
      _pendingOpen = false;
      ShowWebView();
      PushPortState("openPort");
    }
  }

  private void HandleBuyItems(BuyItemsMessage msg)
  {
    if (_currentPortItems == null) return;

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = _currentPortItems.FirstOrDefault(si => si.ItemType == type);
      if (shopItem == null || shopItem.BuyPrice <= 0) continue;

      int totalCost = shopItem.BuyPrice * req.Quantity;
      bool ok = _player.UpdateInventory(type, req.Quantity, -totalCost);
      GD.Print(ok
        ? $"HUD: Bought {req.Quantity}x {req.Type} for {totalCost} coin"
        : $"HUD: Buy failed for {req.Type}");
    }
  }

  private void HandleSellItems(SellItemsMessage msg)
  {
    if (_currentPortItems == null) return;

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = _currentPortItems.FirstOrDefault(si => si.ItemType == type);
      if (shopItem == null || shopItem.SellPrice <= 0) continue;

      int totalRevenue = shopItem.SellPrice * req.Quantity;
      bool ok = _player.UpdateInventory(type, -req.Quantity, totalRevenue);
      GD.Print(ok
        ? $"HUD: Sold {req.Quantity}x {req.Type} for {totalRevenue} coin"
        : $"HUD: Sell failed for {req.Type}");
    }
  }

  private void HandlePurchaseComponent(PurchaseComponentMessage msg)
  {
    var component = GameData.Components.FirstOrDefault(c => c.name == msg.Name);
    if (component == null) return;
    _player.PurchaseComponent(component);
  }

  private void HandleEquipComponent(EquipComponentMessage msg)
  {
    var component = GameData.Components.FirstOrDefault(c => c.name == msg.Name);
    if (component == null) return;
    _player.EquipComponent(component);
  }

  private void HandleUnequipComponent(UnequipComponentMessage msg)
  {
    var component = GameData.Components.FirstOrDefault(c => c.name == msg.Name);
    if (component == null) return;
    _player.UnEquipComponent(component);
  }

  private void HandleHeal()
  {
    int healthNeeded = _player.MaxHealth - _player.Health;
    if (healthNeeded <= 0) return;

    int woodAvailable = _player.GetInventoryCount(InventoryItemType.Wood);
    int fishAvailable = _player.GetInventoryCount(InventoryItemType.Fish);

    const int WoodCostPerHealth = 5;
    const int FishCostPerHealth = 1;

    int maxHealFromWood = woodAvailable / WoodCostPerHealth;
    int maxHealFromFish = fishAvailable / FishCostPerHealth;
    int healthToHeal = Math.Min(healthNeeded, Math.Min(maxHealFromWood, maxHealFromFish));

    if (healthToHeal <= 0) return;

    int woodCost = healthToHeal * WoodCostPerHealth;
    int fishCost = healthToHeal * FishCostPerHealth;

    _player.UpdateInventory(InventoryItemType.Wood, -woodCost);
    _player.UpdateInventory(InventoryItemType.Fish, -fishCost);
    _player.Health += healthToHeal;
    _player.EmitSignal(Player.SignalName.HealthUpdate, _player.Health);

    GD.Print($"HUD: Healed {healthToHeal} HP. Cost: {woodCost} wood, {fishCost} fish.");
  }

  /// <summary>
  /// Creative-mode only: sets inventory items to exact quantities.
  /// Checks Configuration.IsCreative server-side to prevent cheating
  /// via a modded webview.
  /// </summary>
  private void HandleSetInventory(SetInventoryMessage msg)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_inventory rejected — creative mode is not enabled");
      return;
    }

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      int current = _player.GetInventoryCount(type);
      int delta = req.Quantity - current;
      _player.UpdateInventory(type, delta);
      GD.Print($"HUD: [Creative] Set {req.Type} to {req.Quantity} (delta {delta})");
    }
  }

  /// <summary>
  /// Creative-mode only: removes all owned components and resets stats.
  /// </summary>
  private void HandleClearComponents()
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: clear_components rejected — creative mode is not enabled");
      return;
    }

    int count = _player.OwnedComponents.Count;
    _player.OwnedComponents.Clear();
    _player.UpdatePlayerStats();
    GD.Print($"HUD: [Creative] Cleared {count} components");
  }

  /// <summary>
  /// Creative-mode only: sets the player's health to an exact value,
  /// clamped between 1 and MaxHealth.
  /// </summary>
  private void HandleSetHealth(SetHealthMessage msg)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_health rejected — creative mode is not enabled");
      return;
    }

    int clamped = Math.Clamp(msg.Health, 1, _player.MaxHealth);
    _player.Health = clamped;
    _player.EmitSignal(Player.SignalName.HealthUpdate, _player.Health);
    GD.Print($"HUD: [Creative] Set health to {clamped}");
  }

  /// <summary>
  /// Creative-mode only: sets the player's ship tier directly,
  /// bypassing costs. Applies the visual/collision swap immediately.
  /// </summary>
  private void HandleSetShipTier(SetShipTierMessage msg)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_ship_tier rejected — creative mode is not enabled");
      return;
    }

    int tier = Math.Clamp(msg.Tier, 0, GameData.ShipTiers.Length - 1);
    _player.ShipTier = tier;
    _player.ApplyShipTier();
    _player.UpdatePlayerStats();
    GD.Print($"HUD: [Creative] Set ship tier to {tier} ({GameData.ShipTiers[tier].Name})");
  }

  // ── Ship upgrade handler ─────────────────────────────────────────

  private void HandleUpgradeShip()
  {
    bool ok = _player.UpgradeShip();
    GD.Print(ok
      ? $"HUD: Upgraded ship to tier {_player.ShipTier}"
      : "HUD: Ship upgrade failed");
  }

  // ── Vault handlers ────────────────────────────────────────────────

  private void HandleBuildVault()
  {
    if (_currentPortName == null) return;
    bool ok = _player.BuildVault(_currentPortName);
    GD.Print(ok
      ? $"HUD: Built vault at {_currentPortName}"
      : "HUD: Build vault failed");
  }

  private void HandleUpgradeVault()
  {
    bool ok = _player.UpgradeVault();
    GD.Print(ok
      ? $"HUD: Upgraded vault to level {_player.VaultLevel}"
      : "HUD: Upgrade vault failed");
  }

  private void HandleVaultDeposit(VaultDepositMessage msg)
  {
    if (_currentPortName == null || _player.VaultPortName != _currentPortName) return;

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;
      bool ok = _player.VaultDeposit(type, req.Quantity);
      GD.Print(ok
        ? $"HUD: Deposited {req.Quantity}x {req.Type} into vault"
        : $"HUD: Vault deposit failed for {req.Type}");
    }
  }

  private void HandleVaultWithdraw(VaultWithdrawMessage msg)
  {
    if (_currentPortName == null || _player.VaultPortName != _currentPortName) return;

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;
      bool ok = _player.VaultWithdraw(type, req.Quantity);
      GD.Print(ok
        ? $"HUD: Withdrew {req.Quantity}x {req.Type} from vault"
        : $"HUD: Vault withdraw failed for {req.Type}");
    }
  }

  private void HandleSetVault(SetVaultMessage msg)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_vault rejected — creative mode is not enabled");
      return;
    }

    int level = Math.Clamp(msg.Level, 1, Player.VaultMaxLevel);
    string portName = string.IsNullOrWhiteSpace(msg.PortName)
      ? (_currentPortName ?? "Creative Port")
      : msg.PortName;

    _player.VaultPortName = portName;
    _player.VaultLevel = level;
    if (_player.VaultItems == null)
      _player.VaultItems = new System.Collections.Generic.Dictionary<InventoryItemType, int>();
    GD.Print($"HUD: [Creative] Set vault at {portName} level {level}");
  }

  private void HandleDeleteVault()
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: delete_vault rejected — creative mode is not enabled");
      return;
    }

    _player.VaultPortName = null;
    _player.VaultLevel = 0;
    _player.VaultItems = new System.Collections.Generic.Dictionary<InventoryItemType, int>();
    GD.Print("HUD: [Creative] Deleted vault");
  }

  public override void _ExitTree()
  {
    if (_webViewCreated)
      GetTree().Root.SizeChanged -= OnWindowResized;
  }
}
