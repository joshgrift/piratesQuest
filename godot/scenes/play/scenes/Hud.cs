namespace PiratesQuest;

using Godot;
using System;
using System.Linq;
using System.Text.Json;
using PiratesQuest.Data;

public partial class Hud : Control
{
  [Export] public Node _webView;

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

    // TODO Make this work
    //_webView.Set("url", Configuration.WebViewUrl);
    _webView.Connect("ipc_message", new Callable(this, MethodName.OnIpcMessage));
    // TODO Disable dev tools in prod builds

    // Get ports
    var ports = GetTree().GetNodesInGroup("ports");
    GD.Print($"HUD found {ports.Count} ports in the scene");

    foreach (Port port in ports.Cast<Port>())
    {
      GD.Print($"HUD subscribing to port {port.PortName} events");
      port.ShipDocked += OnPlayerEnteredPort;
      port.ShipDeparted += OnPlayerDepartedPort;
    }
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
      default:
        GD.PushError($"HUD: Unknown IPC message type");
        break;
    }
  }

  private PortStateDto BuildHUDState()
  {
    var inventory = new System.Collections.Generic.Dictionary<string, int>();
    foreach (InventoryItemType type in Enum.GetValues(typeof(InventoryItemType)))
      inventory[type.ToString()] = _player.GetInventoryCount(type);

    ShopItemDto[] shopItems = [];
    if (IsInPort())
    {
      shopItems = (_currentPort.ItemsForSale ?? [])
      .Select(si => new ShopItemDto(si.ItemType.ToString(), si.BuyPrice, si.SellPrice))
      .ToArray();
    }

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

    // Show tavern locals for this port first.
    // Then append any currently hired crew that are from other ports,
    // so hired crewmates are always visible/manageable in every tavern.
    var tavernCharactersById = TavernData
      .GetCharactersForPort(_currentPort.PortName ?? "")
      .ToDictionary(c => c.Id, c => c, StringComparer.Ordinal);

    foreach (var hiredId in _player.HiredCrewCharacterIds)
    {
      if (tavernCharactersById.ContainsKey(hiredId)) continue;
      var hiredCharacter = TavernData.GetCharacterById(hiredId);
      if (hiredCharacter != null)
        tavernCharactersById[hiredCharacter.Id] = hiredCharacter;
    }

    var tavernCharacters = tavernCharactersById.Values
      .Select(c => new TavernCharacterDto(
        c.Id,
        c.Name,
        c.Role,
        c.Portrait,
        c.Hireable,
        c.StatChanges.Select(sc => new StatChangeDto(
          sc.Stat.ToString(),
          sc.Modifier.ToString(),
          sc.Value
        )).ToArray()
      ))
      .ToArray();

    // Build vault snapshot (null if player hasn't built one yet)
    VaultStateDto vaultState = null;
    if (_player.VaultPortName != null)
    {
      bool isHere = _player.VaultPortName == (_currentPort.PortName ?? "");
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

    // Convert C# enum-keyed costs into string-keyed costs for JSON payloads.
    var vaultBuildCost = Player.VaultBuildCost.ToDictionary(
      kvp => kvp.Key.ToString(),
      kvp => kvp.Value
    );

    System.Collections.Generic.Dictionary<string, int> vaultUpgradeCost = null;
    if (_player.VaultPortName != null && _player.VaultLevel < Player.VaultMaxLevel)
    {
      vaultUpgradeCost = Player.GetVaultUpgradeCost(_player.VaultLevel).ToDictionary(
        kvp => kvp.Key.ToString(),
        kvp => kvp.Value
      );
    }

    int woodPerHp = Player.RepairCostPerHp.TryGetValue(InventoryItemType.Wood, out var w) ? w : 0;
    int fishPerHp = Player.RepairCostPerHp.TryGetValue(InventoryItemType.Fish, out var f) ? f : 0;

    return new PortStateDto
    {
      IsInPort = IsInPort(),
      PortName = _currentPort.PortName ?? "",
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
      Costs = new PortCostsDto
      {
        VaultBuild = vaultBuildCost,
        VaultUpgrade = vaultUpgradeCost,
        Repair = new RepairCostDto
        {
          WoodPerHp = woodPerHp,
          FishPerHp = fishPerHp,
        },
      },
      Tavern = new TavernStateDto
      {
        CrewSlots = _player.GetCrewSlotCapacity(),
        HiredCharacterIds = _player.HiredCrewCharacterIds.ToArray(),
        Characters = tavernCharacters,
      },
      Leaderboard = [],
    };
  }

  private void HandleBuyItems(BuyItemsMessage msg)
  {
    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = _currentPort.ItemsForSale.FirstOrDefault(si => si.ItemType == type);
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
    // Crew/components can add SellPriceBonus where 0.005 == +0.5% sale revenue.
    // We clamp at zero so negative values can never produce negative gold.
    float sellMultiplier = Math.Max(0.0f, 1.0f + _player.Stats.GetStat(PlayerStat.SellPriceBonus));

    foreach (var req in msg.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = _currentPort.ItemsForSale.FirstOrDefault(si => si.ItemType == type);
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
