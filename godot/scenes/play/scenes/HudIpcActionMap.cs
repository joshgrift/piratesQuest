namespace PiratesQuest;

using Godot;
using PiratesQuest.Data;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central action dispatch table for HUD IPC.
/// Each handler receives the raw IPC message, player, and current port (if docked).
/// </summary>
public static class HudIpcActionMap
{
  public static readonly Dictionary<IpcAction, Action<IpcMessage, Player, Port>> Handlers = new()
  {
    [IpcAction.BuyItems] = (message, player, currentPort) => HandleBuyItems(message as BuyItemsMessage, player, currentPort),
    [IpcAction.SellItems] = (message, player, currentPort) => HandleSellItems(message as SellItemsMessage, player, currentPort),
    [IpcAction.PurchaseComponent] = (message, player, currentPort) => HandlePurchaseComponent(message as PurchaseComponentMessage, player, currentPort),
    [IpcAction.EquipComponent] = (message, player, currentPort) => HandleEquipComponent(message as EquipComponentMessage, player, currentPort),
    [IpcAction.UnequipComponent] = (message, player, currentPort) => HandleUnequipComponent(message as UnequipComponentMessage, player, currentPort),
    [IpcAction.Heal] = (message, player, currentPort) => HandleHeal(player, currentPort),
    [IpcAction.SetInventory] = (message, player, currentPort) => HandleSetInventory(message as SetInventoryMessage, player),
    [IpcAction.ClearComponents] = (message, player, currentPort) => HandleClearComponents(player),
    [IpcAction.SetHealth] = (message, player, currentPort) => HandleSetHealth(message as SetHealthMessage, player),
    [IpcAction.UpgradeShip] = (message, player, currentPort) => HandleUpgradeShip(player, currentPort),
    [IpcAction.HireCharacter] = (message, player, currentPort) => HandleHireCharacter(message as HireCharacterMessage, player, currentPort),
    [IpcAction.FireCharacter] = (message, player, currentPort) => HandleFireCharacter(message as FireCharacterMessage, player),
    [IpcAction.TalkToNpc] = (message, player, currentPort) => HandleTalkToNpc(message as TalkToNpcMessage, player),
    [IpcAction.AcceptQuest] = (message, player, currentPort) => HandleAcceptQuest(message as AcceptQuestMessage, player, currentPort),
    [IpcAction.CompleteQuest] = (message, player, currentPort) => HandleCompleteQuest(message as CompleteQuestMessage, player),
    [IpcAction.UncompleteQuest] = (message, player, currentPort) => HandleUncompleteQuest(message as UncompleteQuestMessage, player),
    [IpcAction.SetActiveQuest] = (message, player, currentPort) => HandleSetActiveQuest(message as SetActiveQuestMessage, player),
    [IpcAction.BuildVault] = (message, player, currentPort) => HandleBuildVault(player, currentPort),
    [IpcAction.UpgradeVault] = (message, player, currentPort) => HandleUpgradeVault(player),
    [IpcAction.VaultDeposit] = (message, player, currentPort) => HandleVaultDeposit(message as VaultDepositMessage, player, currentPort),
    [IpcAction.VaultWithdraw] = (message, player, currentPort) => HandleVaultWithdraw(message as VaultWithdrawMessage, player, currentPort),
    [IpcAction.SetShipTier] = (message, player, currentPort) => HandleSetShipTier(message as SetShipTierMessage, player),
    [IpcAction.SetVault] = (message, player, currentPort) => HandleSetVault(message as SetVaultMessage, player, currentPort),
    [IpcAction.DeleteVault] = (message, player, currentPort) => HandleDeleteVault(player),
    [IpcAction.InputKey] = (message, player, currentPort) => HandleInputKey(message as InputKeyMessage, player),
    [IpcAction.InputCameraRotate] = (message, player, currentPort) => HandleInputCameraRotate(message as InputCameraRotateMessage, player),
    [IpcAction.InputCameraZoom] = (message, player, currentPort) => HandleInputCameraZoom(message as InputCameraZoomMessage, player),
    [IpcAction.InputCameraPan] = (message, player, currentPort) => HandleInputCameraPan(message as InputCameraPanMessage, player),
  };

  private static void HandleBuyItems(BuyItemsMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.BuyGoods, "buy goods")) return;
    var shopItems = currentPort.ExportHudSnapshot().ItemsForSale;

    foreach (var req in message.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = shopItems.FirstOrDefault(si => si.Type == req.Type);
      if (shopItem == null || shopItem.BuyPrice <= 0) continue;

      int totalCost = shopItem.BuyPrice * req.Quantity;
      bool ok = player.UpdateInventory(type, req.Quantity, -totalCost);
      GD.Print(ok
        ? $"HUD: Bought {req.Quantity}x {req.Type} for {totalCost} coin"
        : $"HUD: Buy failed for {req.Type}");
      if (ok)
      {
        player.Progress.RecordItemBought(type, req.Quantity, totalCost);
        player.ReevaluateQuestProgress(currentPort?.PortName);
      }
    }
  }

  private static void HandleSellItems(SellItemsMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.SellGoods, "sell goods")) return;
    var shopItems = currentPort.ExportHudSnapshot().ItemsForSale;

    // Crew/components can add SellPriceBonus where 0.005 == +0.5% sale revenue.
    // Clamp to zero so negative values can never produce negative gold.
    float sellMultiplier = Math.Max(0.0f, 1.0f + player.Stats.GetStat(PlayerStat.SellPriceBonus));

    foreach (var req in message.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;

      var shopItem = shopItems.FirstOrDefault(si => si.Type == req.Type);
      if (shopItem == null || shopItem.SellPrice <= 0) continue;

      int totalRevenue = (int)MathF.Round(shopItem.SellPrice * req.Quantity * sellMultiplier);
      bool ok = player.UpdateInventory(type, -req.Quantity, totalRevenue);
      GD.Print(ok
        ? $"HUD: Sold {req.Quantity}x {req.Type} for {totalRevenue} coin"
        : $"HUD: Sell failed for {req.Type}");
      if (ok)
      {
        player.Progress.RecordItemSold(type, req.Quantity, totalRevenue);
        player.ReevaluateQuestProgress(currentPort?.PortName);
      }
    }
  }

  private static void HandlePurchaseComponent(PurchaseComponentMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.ShipyardComponents, "purchase components")) return;
    var component = GameData.Components.FirstOrDefault(c => c.name == message.Name);
    if (component == null) return;
    player.PurchaseComponent(component);
  }

  private static void HandleEquipComponent(EquipComponentMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.ShipyardComponents, "equip components")) return;
    var component = GameData.Components.FirstOrDefault(c => c.name == message.Name);
    if (component == null) return;
    player.EquipComponent(component);
    player.ReevaluateQuestProgress(currentPort?.PortName);
  }

  private static void HandleUnequipComponent(UnequipComponentMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.ShipyardComponents, "unequip components")) return;
    var component = GameData.Components.FirstOrDefault(c => c.name == message.Name);
    if (component == null) return;
    player.UnEquipComponent(component);
    player.ReevaluateQuestProgress(currentPort?.PortName);
  }

  private static void HandleHeal(Player player, Port currentPort)
  {
    if (currentPort == null) return;

    int healthNeeded = player.MaxHealth - player.Health;
    if (healthNeeded <= 0) return;

    int woodAvailable = player.GetInventoryCount(InventoryItemType.Wood);
    int fishAvailable = player.GetInventoryCount(InventoryItemType.Fish);

    int woodCostPerHealth = Player.RepairCostPerHp[InventoryItemType.Wood];
    int fishCostPerHealth = Player.RepairCostPerHp[InventoryItemType.Fish];

    int maxHealFromWood = woodAvailable / woodCostPerHealth;
    int maxHealFromFish = fishAvailable / fishCostPerHealth;
    int healthToHeal = Math.Min(healthNeeded, Math.Min(maxHealFromWood, maxHealFromFish));

    if (healthToHeal <= 0) return;

    int woodCost = healthToHeal * woodCostPerHealth;
    int fishCost = healthToHeal * fishCostPerHealth;

    player.UpdateInventory(InventoryItemType.Wood, -woodCost);
    player.UpdateInventory(InventoryItemType.Fish, -fishCost);
    player.Health += healthToHeal;
    player.EmitSignal(Player.SignalName.HealthUpdate, player.Health);

    GD.Print($"HUD: Healed {healthToHeal} HP. Cost: {woodCost} wood, {fishCost} fish.");
  }

  private static void HandleSetInventory(SetInventoryMessage message, Player player)
  {
    if (message == null) return;
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_inventory rejected - creative mode is not enabled");
      return;
    }

    foreach (var req in message.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;
      int current = player.GetInventoryCount(type);
      int delta = req.Quantity - current;
      player.UpdateInventory(type, delta);
      GD.Print($"HUD: [Creative] Set {req.Type} to {req.Quantity} (delta {delta})");
    }
  }

  private static void HandleClearComponents(Player player)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: clear_components rejected - creative mode is not enabled");
      return;
    }

    int count = player.OwnedComponents.Count;
    player.OwnedComponents.Clear();
    player.UpdatePlayerStats();
    GD.Print($"HUD: [Creative] Cleared {count} components");
  }

  private static void HandleSetHealth(SetHealthMessage message, Player player)
  {
    if (message == null) return;
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_health rejected - creative mode is not enabled");
      return;
    }

    int clamped = Math.Clamp(message.Health, 1, player.MaxHealth);
    player.Health = clamped;
    player.EmitSignal(Player.SignalName.HealthUpdate, player.Health);
    GD.Print($"HUD: [Creative] Set health to {clamped}");
  }

  private static void HandleSetShipTier(SetShipTierMessage message, Player player)
  {
    if (message == null) return;
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_ship_tier rejected - creative mode is not enabled");
      return;
    }

    int tier = Math.Clamp(message.Tier, 0, GameData.ShipTiers.Length - 1);
    player.ShipTier = tier;
    player.ApplyShipTier();
    player.UpdatePlayerStats();
    GD.Print($"HUD: [Creative] Set ship tier to {tier} ({GameData.ShipTiers[tier].Name})");
  }

  private static void HandleUpgradeShip(Player player, Port currentPort)
  {
    if (currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.ShipTierUpgrades, "upgrade ship class")) return;
    bool ok = player.UpgradeShip();
    GD.Print(ok
      ? $"HUD: Upgraded ship to tier {player.ShipTier}"
      : "HUD: Ship upgrade failed");
  }

  private static void HandleHireCharacter(HireCharacterMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.TavernTalk, "hire tavern crew")) return;
    bool ok = player.HireCrew(message.CharacterId, currentPort.PortName);
    GD.Print(ok
      ? $"HUD: Hired character '{message.CharacterId}'"
      : $"HUD: Hire failed for '{message.CharacterId}'");
  }

  private static void HandleFireCharacter(FireCharacterMessage message, Player player)
  {
    if (message == null) return;
    bool ok = player.FireCrew(message.CharacterId);
    GD.Print(ok
      ? $"HUD: Fired character '{message.CharacterId}'"
      : $"HUD: Fire failed for '{message.CharacterId}'");
  }

  private static void HandleTalkToNpc(TalkToNpcMessage message, Player player)
  {
    if (message == null) return;
    player.RecordNpcTalkedTo(message.CharacterId);
  }

  private static void HandleAcceptQuest(AcceptQuestMessage message, Player player, Port currentPort)
  {
    if (message == null) return;
    bool ok = player.AcceptQuest(message.QuestId, message.CharacterId);
    GD.Print(ok
      ? $"HUD: Accepted quest '{message.QuestId}' from '{message.CharacterId}'"
      : $"HUD: Accept quest failed for '{message.QuestId}'");
  }

  private static void HandleBuildVault(Player player, Port currentPort)
  {
    if (currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.Vault, "build a vault")) return;
    bool ok = player.BuildVault(currentPort.PortName);
    GD.Print(ok
      ? $"HUD: Built vault at {currentPort.PortName}"
      : "HUD: Build vault failed");
  }

  private static void HandleCompleteQuest(CompleteQuestMessage message, Player player)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: complete_quest rejected - creative mode is not enabled");
      return;
    }

    bool ok = player.ForceCompleteQuest(message?.QuestId);
    GD.Print(ok
      ? $"HUD: [Creative] Completed quest '{message?.QuestId ?? player.Progress.CurrentQuestId ?? ""}'"
      : "HUD: [Creative] Quest completion failed");
  }

  private static void HandleUncompleteQuest(UncompleteQuestMessage message, Player player)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: uncomplete_quest rejected - creative mode is not enabled");
      return;
    }

    bool ok = player.ForceUncompleteQuest(message?.QuestId);
    GD.Print(ok
      ? $"HUD: [Creative] Rolled back quest '{message?.QuestId ?? ""}'"
      : "HUD: [Creative] Quest rollback failed");
  }

  private static void HandleSetActiveQuest(SetActiveQuestMessage message, Player player)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_active_quest rejected - creative mode is not enabled");
      return;
    }

    bool ok = player.ForceSetActiveQuest(message?.QuestId);
    GD.Print(ok
      ? $"HUD: [Creative] Set active quest to '{message?.QuestId ?? ""}'"
      : "HUD: [Creative] Set active quest failed");
  }

  private static void HandleUpgradeVault(Player player)
  {
    if (!RequireFeature(player, FeatureUnlock.Vault, "upgrade the vault")) return;
    bool ok = player.UpgradeVault();
    GD.Print(ok
      ? $"HUD: Upgraded vault to level {player.VaultLevel}"
      : "HUD: Upgrade vault failed");
  }

  private static void HandleVaultDeposit(VaultDepositMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.Vault, "use the vault")) return;
    if (player.VaultPortName != currentPort.PortName) return;

    foreach (var req in message.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;
      bool ok = player.VaultDeposit(type, req.Quantity);
      GD.Print(ok
        ? $"HUD: Deposited {req.Quantity}x {req.Type} into vault"
        : $"HUD: Vault deposit failed for {req.Type}");
    }
  }

  private static void HandleVaultWithdraw(VaultWithdrawMessage message, Player player, Port currentPort)
  {
    if (message == null || currentPort == null) return;
    if (!RequireFeature(player, FeatureUnlock.Vault, "use the vault")) return;
    if (player.VaultPortName != currentPort.PortName) return;

    foreach (var req in message.Items)
    {
      if (!Enum.TryParse<InventoryItemType>(req.Type, out var type)) continue;
      bool ok = player.VaultWithdraw(type, req.Quantity);
      GD.Print(ok
        ? $"HUD: Withdrew {req.Quantity}x {req.Type} from vault"
        : $"HUD: Vault withdraw failed for {req.Type}");
    }
  }

  private static void HandleSetVault(SetVaultMessage message, Player player, Port currentPort)
  {
    if (message == null) return;
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: set_vault rejected - creative mode is not enabled");
      return;
    }

    int level = Math.Clamp(message.Level, 1, Player.VaultMaxLevel);
    string portName = string.IsNullOrWhiteSpace(message.PortName)
      ? (currentPort?.PortName ?? "Creative Port")
      : message.PortName;

    player.VaultPortName = portName;
    player.VaultLevel = level;
    player.VaultItems ??= new Dictionary<InventoryItemType, int>();
    GD.Print($"HUD: [Creative] Set vault at {portName} level {level}");
  }

  private static void HandleDeleteVault(Player player)
  {
    if (!Configuration.IsCreative)
    {
      GD.PrintErr("HUD: delete_vault rejected - creative mode is not enabled");
      return;
    }

    player.VaultPortName = null;
    player.VaultLevel = 0;
    player.VaultItems = new Dictionary<InventoryItemType, int>();
    GD.Print("HUD: [Creative] Deleted vault");
  }

  private static void HandleInputKey(InputKeyMessage message, Player player)
  {
    if (message == null) return;
    player.HandleInputKey(message.Key, message.Pressed);
  }

  private static void HandleInputCameraRotate(InputCameraRotateMessage message, Player player)
  {
    if (message == null) return;
    GetCameraPivot(player)?.HandleCameraRotate(message.DeltaX, message.DeltaY);
  }

  private static void HandleInputCameraZoom(InputCameraZoomMessage message, Player player)
  {
    if (message == null) return;
    GetCameraPivot(player)?.HandleCameraZoom(message.Delta);
  }

  private static void HandleInputCameraPan(InputCameraPanMessage message, Player player)
  {
    if (message == null) return;
    GetCameraPivot(player)?.HandleCameraPan(message.DeltaX);
  }

  private static CameraPivot GetCameraPivot(Player player)
  {
    return player?.GetNodeOrNull<CameraPivot>("CameraPivot");
  }

  private static bool RequireFeature(Player player, FeatureUnlock feature, string actionName)
  {
    if (player.IsFeatureUnlocked(feature))
      return true;

    GD.PrintErr($"HUD: {actionName} rejected - feature '{feature}' is still locked");
    return false;
  }
}
