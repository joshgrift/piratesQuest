namespace PiratesQuest.Data;

using System.Collections.Generic;
using System.Text.Json.Serialization;

// ── Godot → React (serialized and pushed via eval) ──────────────────

/// <summary>
/// Complete HUD state sent to the React UI.
/// Includes both at-sea and in-port data in one payload.
/// </summary>
public record HudStateDto
{
  /// <summary>True when the player is currently docked at a port.</summary>
  public bool IsInPort { get; init; }
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
  public PortCostsDto Costs { get; init; } = new();
  public TavernStateDto Tavern { get; init; } = new();
  public LeaderboardEntryDto[] Leaderboard { get; init; } = [];

  /// <summary>
  /// The player's vault info. Null when no vault has been built yet.
  /// IsHere is true when the vault is at this port (enables the full UI).
  /// </summary>
  public VaultStateDto Vault { get; init; }
}

public record LeaderboardEntryDto(
  string Nickname,
  int Trophies,
  bool IsLocal
);

/// <summary>
/// Tavern snapshot for the currently docked port.
/// </summary>
public record TavernStateDto
{
  /// <summary>How many crew can be hired with the current ship tier.</summary>
  public int CrewSlots { get; init; }
  /// <summary>Character ids currently hired by this player.</summary>
  public string[] HiredCharacterIds { get; init; } = [];
  /// <summary>Characters physically present at this port.</summary>
  public TavernCharacterDto[] Characters { get; init; } = [];
}

public record TavernCharacterDto(
  string Id,
  string Name,
  string Role,
  string Portrait,
  bool Hireable,
  StatChangeDto[] StatChanges
);

/// <summary>
/// Gameplay costs pushed from C# so the webview never hardcodes them.
/// </summary>
public record PortCostsDto
{
  public Dictionary<string, int> VaultBuild { get; init; } = new();
  /// <summary>
  /// Null when the next vault upgrade is not available (no vault or max level).
  /// </summary>
  public Dictionary<string, int> VaultUpgrade { get; init; }
  public RepairCostDto Repair { get; init; } = new();
}

/// <summary>
/// Resource cost required to repair 1 HP.
/// </summary>
public record RepairCostDto
{
  public int WoodPerHp { get; init; }
  public int FishPerHp { get; init; }
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

/// <summary>
/// Export payload for port-specific HUD data.
/// Kept separate so ports can grow HUD fields over time.
/// </summary>
public record HudPortSnapshotDto
{
  public string PortName { get; init; } = "";
  public ShopItemDto[] ItemsForSale { get; init; } = [];
}

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
[JsonDerivedType(typeof(HireCharacterMessage), "hire_character")]
[JsonDerivedType(typeof(FireCharacterMessage), "fire_character")]
[JsonDerivedType(typeof(SetShipTierMessage), "set_ship_tier")]
[JsonDerivedType(typeof(SetVaultMessage), "set_vault")]
[JsonDerivedType(typeof(DeleteVaultMessage), "delete_vault")]
[JsonDerivedType(typeof(InputKeyMessage), "input_key")]
[JsonDerivedType(typeof(InputCameraRotateMessage), "input_camera_rotate")]
[JsonDerivedType(typeof(InputCameraZoomMessage), "input_camera_zoom")]
[JsonDerivedType(typeof(InputCameraPanMessage), "input_camera_pan")]
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

/// <summary>Hire a tavern character currently at this port.</summary>
public record HireCharacterMessage : IpcMessage
{
  public string CharacterId { get; init; } = "";
}

/// <summary>Fire a currently hired tavern character.</summary>
public record FireCharacterMessage : IpcMessage
{
  public string CharacterId { get; init; } = "";
}

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

/// <summary>Creative-mode only: set the player's ship tier directly (0-based index).</summary>
public record SetShipTierMessage : IpcMessage
{
  public int Tier { get; init; }
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

// ── Input forwarding (webview → Godot) ──────────────────────────────────────

/// <summary>
/// A key press or release forwarded from the webview.
/// Key is the browser key name lowercased (e.g. "w", "a", "q").
/// </summary>
public record InputKeyMessage : IpcMessage
{
  public string Key { get; init; } = "";
  public bool Pressed { get; init; }
}

/// <summary>
/// Camera rotation delta from a mouse drag.
/// DeltaX/DeltaY are raw movementX/Y pixels from the browser.
/// Godot applies CameraPivot.MouseSensitivity to scale them.
/// </summary>
public record InputCameraRotateMessage : IpcMessage
{
  public float DeltaX { get; init; }
  public float DeltaY { get; init; }
}

/// <summary>
/// Camera zoom delta from a scroll wheel or trackpad pinch.
/// Delta is pre-normalized to ~1.0 per scroll step (deltaY / 100).
/// Godot applies CameraPivot.ZoomSpeed to scale it.
/// </summary>
public record InputCameraZoomMessage : IpcMessage
{
  public float Delta { get; init; }
}

/// <summary>
/// Camera horizontal rotation from a trackpad two-finger horizontal swipe.
/// DeltaX is raw wheel deltaX from the browser.
/// Godot applies CameraPivot.TrackpadRotationSensitivity to scale it.
/// </summary>
public record InputCameraPanMessage : IpcMessage
{
  public float DeltaX { get; init; }
}
