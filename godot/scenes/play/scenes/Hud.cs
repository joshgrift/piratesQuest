namespace PiratesQuest;

using Godot;
using System;
using System.Linq;
using System.Text.Json;
using PiratesQuest.Data;

public partial class Hud : Control
{
  [Export] public Node _webView;

  private CanvasLayer _webViewLayer = null;
  private bool _webViewLoaded = false;
  private Player _player = null;
  private Port _currentPort = null;

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

    ConfigureWebViewNode();

    // Get ports
    var ports = GetTree().GetNodesInGroup("ports");
    GD.Print($"HUD found {ports.Count} ports in the scene");

    foreach (Port port in ports.Cast<Port>())
    {
      GD.Print($"HUD subscribing to port {port.PortName} events");
      port.ShipDocked += OnPlayerEnteredPort;
      port.ShipDeparted += OnPlayerDepartedPort;
    }

    // Also re-apply when the game window changes size.
    GetTree().Root.SizeChanged += OnWindowResized;
  }

  public override void _ExitTree()
  {
    if (GetTree() != null && GetTree().Root != null)
      GetTree().Root.SizeChanged -= OnWindowResized;
  }

  public void SetPlayer(Player player)
  {
    _player = player;

    _player.InventoryChanged += OnInventoryChanged;

    _player.CannonReadyToFire += () =>
    {
      OnStateChange();
    };

    _player.CannonFired += () =>
    {
      OnStateChange();
    };

    _player.HealthUpdate += (newHealth) =>
    {
      OnStateChange();
    };

    GD.Print($"HUD connected to Player {_player.Name}");

    OnStateChange();
  }

  private void OnInventoryChanged(InventoryItemType itemType, int newAmount, int change)
  {
    OnStateChange();
  }

  private void OnPlayerEnteredPort(Port port, Player player, Variant payload)
  {
    if (_player == null || player.Name != _player.Name) return;
    GD.Print($"Player {player.Name} entered port {port.PortName}");

    _currentPort = port;
  }

  private void OnPlayerDepartedPort(Port port, Player player)
  {
    if (_player == null || player.Name != _player.Name) return;
    GD.Print($"Player {player.Name} departed port");
    _currentPort = null;
  }

  private void SyncWebViewSize()
  {
    if (_webView is not Control wv) return;

    var viewportSize = GetTree().Root.Size;

    // Godot's root viewport can be inflated by max monitor scale under
    // mixed-DPI setups. We pre-correct so godot_wry receives native bounds.
    float maxScale = 1f;
    for (int i = 0; i < DisplayServer.GetScreenCount(); i++)
      maxScale = Math.Max(maxScale, DisplayServer.ScreenGetScale(i));

    int currentScreen = DisplayServer.WindowGetCurrentScreen();
    float currentScale = DisplayServer.ScreenGetScale(currentScreen);
    float correction = currentScale / maxScale;

    var targetSize = new Vector2(viewportSize.X * correction, viewportSize.Y * correction);

    // One-pixel nudge, then deferred final size. This consistently forces a
    // native bounds refresh on mixed-DPI monitor setups.
    wv.Position = Vector2.Zero;
    wv.Size = new Vector2(targetSize.X + 1f, targetSize.Y + 1f);
    wv.SetDeferred("size", targetSize);
    _webView.CallDeferred("eval", "window.dispatchEvent(new Event('resize'));");
  }

  private void OnWindowResized()
  {
    CallDeferred(MethodName.SyncWebViewSize);
  }

  private void ConfigureWebViewNode()
  {
    // We manually drive bounds because mixed-DPI setups can confuse
    // full-window auto-sizing in native overlays.
    _webView.Set("full_window_size", false);
    _webView.Set("transparent", true);
    _webView.Set("forward_input_events", true);
    _webView.Set("focused_when_created", true);

    _webView.Connect("ipc_message", new Callable(this, MethodName.OnIpcMessage));
    // TODO Disable dev tools in prod builds
    // TODO Make this work: _webView.Set("url", Configuration.WebViewUrl);

    // Keep the webview under a CanvasLayer so canvas stretch does not
    // distort the native coordinates passed to godot_wry.
    if (_webView.GetParent() != null)
      _webView.GetParent().RemoveChild(_webView);

    _webViewLayer = new CanvasLayer();
    AddChild(_webViewLayer);
    _webViewLayer.AddChild(_webView);
  }

  private bool OnStateChange()
  {
    if (!_webViewLoaded || _player == null) return false;

    var dto = BuildHUDState();
    var json = JsonSerializer.Serialize(dto, _jsonOpts);
    _webView.Call("eval",
      $"window.updateState && window.updateState({json})");

    return true;
  }

  private bool IsInPort()
  {
    return _currentPort != null;
  }

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
        _webViewLoaded = true;
        CallDeferred(MethodName.SyncWebViewSize);
        CallDeferred(MethodName.OnStateChange);
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
      case HireCharacterMessage hc:
        HandleHireCharacter(hc);
        break;
      case FireCharacterMessage fc:
        HandleFireCharacter(fc);
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
      case InputKeyMessage ik:
        _player.HandleInputKey(ik.Key, ik.Pressed);
        return;
      case InputCameraRotateMessage icr:
        GetCameraPivot()?.HandleCameraRotate(icr.DeltaX, icr.DeltaY);
        return;
      case InputCameraZoomMessage icz:
        GetCameraPivot()?.HandleCameraZoom(icz.Delta);
        return;
      case InputCameraPanMessage icp:
        GetCameraPivot()?.HandleCameraPan(icp.DeltaX);
        return;
      default:
        GD.PushError($"HUD: Unknown IPC message type");
        break;
    }
  }

  private CameraPivot GetCameraPivot()
  {
    return _player?.GetNodeOrNull<CameraPivot>("CameraPivot");
  }

  // BuildHUDState is a pure composer over child snapshots.
  // Rule: gather data from child export methods only (Player/Port DTO exports),
  // never by reading child internals directly (fields/methods like
  // _player.HiredCrewCharacterIds, TavernData lookups from HUD, etc).
  // If a HUD field needs new data, add it to the owning child export first.
  private HudStateDto BuildHUDState()
  {
    var state = _player.ExportHudState();
    if (!IsInPort())
      return state with
      {
        IsInPort = false,
        PortName = "",
        ItemsForSale = [],
      };

    var portSnapshot = _currentPort.ExportHudSnapshot();
    return state with
    {
      IsInPort = true,
      PortName = portSnapshot.PortName,
      ItemsForSale = portSnapshot.ItemsForSale,
      Tavern = portSnapshot.Tavern ?? new TavernStateDto { Characters = [] },
      Crew = state.Crew ?? new CrewStateDto(),
      Vault = BuildVaultStateForPort(state.Vault, portSnapshot.PortName),
    };
  }

  private static VaultStateDto BuildVaultStateForPort(VaultStateDto baseVault, string currentPortName)
  {
    if (baseVault == null)
      return null;

    return baseVault with
    {
      IsHere = baseVault.PortName == (currentPortName ?? "")
    };
  }

  private void HandleBuyItems(BuyItemsMessage msg)
  {
    if (!IsInPort()) return;
    var shopItems = _currentPort.ExportHudSnapshot().ItemsForSale;

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = shopItems.FirstOrDefault(si => si.Type == req.Type);
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
    if (!IsInPort()) return;
    var shopItems = _currentPort.ExportHudSnapshot().ItemsForSale;

    // Crew/components can add SellPriceBonus where 0.005 == +0.5% sale revenue.
    // We clamp at zero so negative values can never produce negative gold.
    float sellMultiplier = Math.Max(0.0f, 1.0f + _player.Stats.GetStat(PlayerStat.SellPriceBonus));

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = shopItems.FirstOrDefault(si => si.Type == req.Type);
      if (shopItem == null || shopItem.SellPrice <= 0) continue;

      int totalRevenue = (int)MathF.Round(shopItem.SellPrice * req.Quantity * sellMultiplier);
      bool ok = _player.UpdateInventory(type, -req.Quantity, totalRevenue);
      GD.Print(ok
        ? $"HUD: Sold {req.Quantity}x {req.Type} for {totalRevenue} coin"
        : $"HUD: Sell failed for {req.Type}");
    }
  }

  private void HandlePurchaseComponent(PurchaseComponentMessage msg)
  {
    if (!IsInPort()) return;

    var component = GameData.Components.FirstOrDefault(c => c.name == msg.Name);
    if (component == null) return;
    _player.PurchaseComponent(component);
  }

  private void HandleEquipComponent(EquipComponentMessage msg)
  {
    if (!IsInPort()) return;

    var component = GameData.Components.FirstOrDefault(c => c.name == msg.Name);
    if (component == null) return;
    _player.EquipComponent(component);
  }

  private void HandleUnequipComponent(UnequipComponentMessage msg)
  {
    if (!IsInPort()) return;

    var component = GameData.Components.FirstOrDefault(c => c.name == msg.Name);
    if (component == null) return;
    _player.UnEquipComponent(component);
  }

  private void HandleHeal()
  {
    if (!IsInPort()) return;

    int healthNeeded = _player.MaxHealth - _player.Health;
    if (healthNeeded <= 0) return;

    int woodAvailable = _player.GetInventoryCount(InventoryItemType.Wood);
    int fishAvailable = _player.GetInventoryCount(InventoryItemType.Fish);

    int woodCostPerHealth = Player.RepairCostPerHp[InventoryItemType.Wood];
    int fishCostPerHealth = Player.RepairCostPerHp[InventoryItemType.Fish];

    int maxHealFromWood = woodAvailable / woodCostPerHealth;
    int maxHealFromFish = fishAvailable / fishCostPerHealth;
    int healthToHeal = Math.Min(healthNeeded, Math.Min(maxHealFromWood, maxHealFromFish));

    if (healthToHeal <= 0) return;

    int woodCost = healthToHeal * woodCostPerHealth;
    int fishCost = healthToHeal * fishCostPerHealth;

    _player.UpdateInventory(InventoryItemType.Wood, -woodCost);
    _player.UpdateInventory(InventoryItemType.Fish, -fishCost);
    _player.Health += healthToHeal;
    _player.EmitSignal(Player.SignalName.HealthUpdate, _player.Health);

    GD.Print($"HUD: Healed {healthToHeal} HP. Cost: {woodCost} wood, {fishCost} fish.");
  }

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

  private void HandleUpgradeShip()
  {
    if (!IsInPort()) return;

    bool ok = _player.UpgradeShip();
    GD.Print(ok
      ? $"HUD: Upgraded ship to tier {_player.ShipTier}"
      : "HUD: Ship upgrade failed");
  }

  private void HandleHireCharacter(HireCharacterMessage msg)
  {
    if (!IsInPort()) return;

    bool ok = _player.HireCrew(msg.CharacterId, _currentPort.PortName);
    GD.Print(ok
      ? $"HUD: Hired character '{msg.CharacterId}'"
      : $"HUD: Hire failed for '{msg.CharacterId}'");
  }

  private void HandleFireCharacter(FireCharacterMessage msg)
  {
    bool ok = _player.FireCrew(msg.CharacterId);
    GD.Print(ok
      ? $"HUD: Fired character '{msg.CharacterId}'"
      : $"HUD: Fire failed for '{msg.CharacterId}'");
  }

  private void HandleBuildVault()
  {
    if (!IsInPort()) return;

    bool ok = _player.BuildVault(_currentPort.PortName);
    GD.Print(ok
      ? $"HUD: Built vault at {_currentPort.PortName}"
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
    if (!IsInPort()) return;
    if (_player.VaultPortName != _currentPort.PortName) return;

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
    if (!IsInPort()) return;
    if (_player.VaultPortName != _currentPort.PortName) return;

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
      ? (_currentPort.PortName ?? "Creative Port")
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
}
