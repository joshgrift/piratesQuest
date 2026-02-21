namespace PiratesQuest;

using PiratesQuest.Attributes;
using Godot;
using Godot.Collections;
using System;

public partial class Port : Node3D, IIntractable
{
  [Export] public String PortName { get; set; } = "Default Port";
  [Export] public ShopItemData[] ItemsForSale { get; set; } = [];
  [Export] public InteractionPoint DockingArea;

  [Signal] public delegate void ShipDockedEventHandler(Port port, Player player, Variant payload);
  [Signal] public delegate void ShipDepartedEventHandler(Port port, Player player);

  public override void _Ready()
  {
    DockingArea.InteractionArea.BodyEntered += OnBodyEntered;
    DockingArea.InteractionArea.BodyExited += OnBodyExited;
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
      EmitSignal(SignalName.ShipDeparted, this, player);
    }
  }

  public Variant GetPayload()
  {
    return new Dictionary
    {
      { "PortName", PortName },
      { "ItemsForSale", ItemsForSale }
    };
  }
}
