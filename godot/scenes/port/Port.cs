namespace PiratesQuest;

using PiratesQuest.Attributes;
using PiratesQuest.Data;
using Godot;
using Godot.Collections;
using System;
using System.Linq;

[Tool]
public partial class Port : Node3D, IIntractable
{
  private float _interactionRadius = 10.0f;

  [Export] public string PortId { get; set; } = "";
  [Export] public InteractionPoint DockingArea;
  [Export(PropertyHint.Range, "0.5,200,0.5,or_greater")]
  public float InteractionRadius
  {
    get => _interactionRadius;
    set
    {
      _interactionRadius = Mathf.Max(0.5f, value);
      SyncInteractionRadius();
    }
  }

  private PortDefinition _portDefinition;

  public string PortName => PortData.GetPortDisplayName(PortId);

  [Signal] public delegate void ShipDockedEventHandler(Port port, Player player, Variant payload);
  [Signal] public delegate void ShipDepartedEventHandler(Port port, Player player);

  public override void _Ready()
  {
    if (Engine.IsEditorHint())
    {
      // In the editor we only need the child interaction point to mirror the radius.
      // Skip gameplay setup like groups, data lookups, and body event wiring.
      SyncInteractionRadius();
      return;
    }

    // AI ships use this group to find nearby ports for awareness and
    // future behaviors like patrol routes or trade runs.
    AddToGroup("ports");

    _portDefinition = PortData.GetPortById(PortId);
    if (_portDefinition == null)
      GD.PushError($"Port '{Name}' is missing port data for id '{PortId}'.");

    SyncInteractionRadius();
    DockingArea.InteractionArea.BodyEntered += OnBodyEntered;
    DockingArea.InteractionArea.BodyExited += OnBodyExited;
  }

  public override void _Notification(int what)
  {
    if (what == NotificationReady)
    {
      SyncInteractionRadius();
    }
  }

  private void SyncInteractionRadius()
  {
    if (DockingArea == null) return;

    // The port owns the tuning value, then forwards it to the shared interaction point.
    DockingArea.InteractionRadius = InteractionRadius;
  }

  private void OnBodyEntered(Node3D body)
  {
    if (body is Player player)
    {
      // Mark player as docked in a safe zone (no combat damage).
      player.SetInPort(true);
      EmitSignal(SignalName.ShipDocked, this, player, GetPayload());
    }
  }
  private void OnBodyExited(Node3D body)
  {
    if (body is Player player)
    {
      // Leaving the port re-enables normal combat damage.
      player.SetInPort(false);
      // Defer the signal so that a same-frame BodyEntered (e.g. from a ship tier
      // collision swap) can set IsInPort back to true before we emit ShipDeparted.
      CallDeferred(MethodName.EmitDepartedIfStillOut, player);
    }
  }

  private void EmitDepartedIfStillOut(Player player)
  {
    if (player.IsInPort) return;
    // A ship-tier upgrade swaps collision shapes, which fires BodyExited but
    // may never fire BodyEntered again (Godot won't detect a stationary body
    // re-entering an area when a shape is re-enabled).  IsSwappingShipTier
    // tells us to ignore this transient exit.
    if (player.IsSwappingShipTier) return;
    EmitSignal(SignalName.ShipDeparted, this, player);
  }

  public Variant GetPayload()
  {
    var itemsForSale = new Godot.Collections.Array<Godot.Collections.Dictionary>();
    foreach (var item in PortData.GetItemsForSale(PortId))
    {
      itemsForSale.Add(new Godot.Collections.Dictionary
      {
        { "ItemType", item.ItemType.ToString() },
        { "BuyPrice", item.BuyPrice },
        { "SellPrice", item.SellPrice },
      });
    }

    return new Dictionary
    {
      { "PortId", PortId },
      { "PortName", PortName },
      { "ItemsForSale", itemsForSale }
    };
  }

  /// <summary>
  /// Exports all port-owned HUD fields in one snapshot object.
  /// </summary>
  public HudPortSnapshotDto ExportHudSnapshot()
  {
    var tavernCharacters = PortData.GetCharactersForPortId(PortId)
      .Select(c => new TavernCharacterDto(
        c.Id,
        c.Name,
        c.Role,
        c.Portrait,
        c.Hireable,
        c.TalkPhrases,
        c.HireText,
        c.FireText,
        c.StatChanges.Select(sc => new StatChangeDto(
          sc.Stat.ToString(),
          sc.Modifier.ToString(),
          sc.Value
        )).ToArray()
      ))
      .ToArray();

    return new HudPortSnapshotDto
    {
      PortId = PortId ?? "",
      PortName = PortName ?? "",
      ItemsForSale = PortData.GetItemsForSale(PortId)
        .Select(item => new ShopItemDto(
          item.ItemType.ToString(),
          item.BuyPrice,
          item.SellPrice
        ))
        .ToArray(),
      Tavern = new TavernStateDto
      {
        Characters = tavernCharacters
      }
    };
  }
}
