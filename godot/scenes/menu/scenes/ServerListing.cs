using Godot;
using System;

public partial class ServerListing : GridContainer
{
  [Export] public String ServerName;
  [Export] public String PlayerMax;
  [Export] public String PlayerCount;
  [Export] public String IpAddress;
  [Export] public int Port;

  [Signal] public delegate void JoinServerEventHandler(string ipAddress, int port);

  public override void _Ready()
  {
    GetNode<Label>("ServerName").Text = ServerName;
    GetNode<Label>("PlayerCountLabel").Text = $"({PlayerCount} / {PlayerMax})";
    GetNode<Button>("MarginContainer/JoinButton").Pressed += () =>
    {
      EmitSignal(SignalName.JoinServer, IpAddress, Port);
    };
  }
}
