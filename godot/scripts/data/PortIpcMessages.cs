namespace PiratesQuest.Data;

using System.Collections.Generic;
using System.Text.Json.Serialization;

// ── Godot → React (serialized and pushed via eval) ──────────────────

/// <summary>
/// Complete port state sent to the React UI when the player docks
/// or after any transaction to keep the UI in sync.
/// </summary>
public record PortStateDto
{
  public string PortName { get; init; } = "";
  public ShopItemDto[] ItemsForSale { get; init; } = [];
  public Dictionary<string, int> Inventory { get; init; } = new();
  public ComponentDto[] Components { get; init; } = [];
  public OwnedComponentDto[] OwnedComponents { get; init; } = [];
  public Dictionary<string, float> Stats { get; init; } = new();
  public int Health { get; init; }
  public int MaxHealth { get; init; }
  public int ComponentCapacity { get; init; }
  public int ShipTier { get; init; }
  public ShipTierDto[] ShipTiers { get; init; } = [];
  public bool IsCreative { get; init; }

  /// <summary>
  /// The player's vault info. Null when no vault has been built yet.
  /// IsHere is true when the vault is at this port (enables the full UI).
  /// </summary>
  public VaultStateDto Vault { get; init; }
}

/// <summary>
/// Vault snapshot sent to the React UI.
/// </summary>
public record VaultStateDto
{
  public string PortName { get; init; } = "";
  public int Level { get; init; }
  public Dictionary<string, int> Items { get; init; } = new();
  /// <summary>True when the player is docked at the port where their vault is.</summary>
  public bool IsHere { get; init; }
  public int ItemCapacity { get; init; }
  public int GoldCapacity { get; init; }
}

public record ShopItemDto(string Type, int BuyPrice, int SellPrice);

public record ShipTierDto(
  string Name,
  string Description,
  int ComponentSlots,
  Dictionary<string, int> Cost
);

public record ComponentDto(
  string Name,
  string Description,
  string Icon,
  Dictionary<string, int> Cost,
  StatChangeDto[] StatChanges
);

public record StatChangeDto(string Stat, string Modifier, float Value);

// OwnedComponentDto is defined in PlayerStateDto.cs

// ── React → Godot (deserialized from IPC JSON) ─────────────────────

/// <summary>
/// Base type for all IPC messages from the React UI.
/// Uses System.Text.Json polymorphic deserialization keyed on the
/// "action" property so we can pattern-match in C#.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "action")]
[JsonDerivedType(typeof(ReadyMessage), "ready")]
[JsonDerivedType(typeof(BuyItemsMessage), "buy_items")]
[JsonDerivedType(typeof(SellItemsMessage), "sell_items")]
[JsonDerivedType(typeof(PurchaseComponentMessage), "purchase_component")]
[JsonDerivedType(typeof(EquipComponentMessage), "equip_component")]
[JsonDerivedType(typeof(UnequipComponentMessage), "unequip_component")]
[JsonDerivedType(typeof(HealMessage), "heal")]
[JsonDerivedType(typeof(FocusParentMessage), "focus_parent")]
[JsonDerivedType(typeof(SetInventoryMessage), "set_inventory")]
[JsonDerivedType(typeof(ClearComponentsMessage), "clear_components")]
[JsonDerivedType(typeof(SetHealthMessage), "set_health")]
[JsonDerivedType(typeof(BuildVaultMessage), "build_vault")]
[JsonDerivedType(typeof(UpgradeVaultMessage), "upgrade_vault")]
[JsonDerivedType(typeof(VaultDepositMessage), "vault_deposit")]
[JsonDerivedType(typeof(VaultWithdrawMessage), "vault_withdraw")]
[JsonDerivedType(typeof(UpgradeShipMessage), "upgrade_ship")]
[JsonDerivedType(typeof(SetVaultMessage), "set_vault")]
[JsonDerivedType(typeof(DeleteVaultMessage), "delete_vault")]
public record IpcMessage;

public record BuyItemsMessage : IpcMessage
{
  public ItemQuantity[] Items { get; init; } = [];
}

public record SellItemsMessage : IpcMessage
{
  public ItemQuantity[] Items { get; init; } = [];
}

public record PurchaseComponentMessage : IpcMessage
{
  public string Name { get; init; } = "";
}

public record EquipComponentMessage : IpcMessage
{
  public string Name { get; init; } = "";
}

public record UnequipComponentMessage : IpcMessage
{
  public string Name { get; init; } = "";
}

public record HealMessage : IpcMessage;

public record ReadyMessage : IpcMessage;

public record FocusParentMessage : IpcMessage;

/// <summary>
/// Creative-mode only: sets a specific inventory item to an exact quantity.
/// Rejected by the server if creative mode is not enabled.
/// </summary>
public record SetInventoryMessage : IpcMessage
{
  public ItemQuantity[] Items { get; init; } = [];
}

/// <summary>
/// Creative-mode only: removes all owned components.
/// Rejected by the server if creative mode is not enabled.
/// </summary>
public record ClearComponentsMessage : IpcMessage;

/// <summary>
/// Creative-mode only: sets the player's health to an exact value.
/// Rejected by the server if creative mode is not enabled.
/// </summary>
public record SetHealthMessage : IpcMessage
{
  public int Health { get; init; }
}

/// <summary>Upgrade the ship to the next tier.</summary>
public record UpgradeShipMessage : IpcMessage;

/// <summary>Build a new vault at the current port (one per player).</summary>
public record BuildVaultMessage : IpcMessage;

/// <summary>Upgrade the vault to the next level.</summary>
public record UpgradeVaultMessage : IpcMessage;

/// <summary>Move items from inventory into the vault.</summary>
public record VaultDepositMessage : IpcMessage
{
  public ItemQuantity[] Items { get; init; } = [];
}

/// <summary>Move items from the vault back into inventory.</summary>
public record VaultWithdrawMessage : IpcMessage
{
  public ItemQuantity[] Items { get; init; } = [];
}

/// <summary>Creative-mode only: set or create a vault at a port with a given level.</summary>
public record SetVaultMessage : IpcMessage
{
  public string PortName { get; init; } = "";
  public int Level { get; init; } = 1;
}

/// <summary>Creative-mode only: delete the player's vault entirely.</summary>
public record DeleteVaultMessage : IpcMessage;

/// <summary>
/// An item type + quantity pair used in buy/sell messages.
/// </summary>
public record ItemQuantity(string Type, int Quantity);
