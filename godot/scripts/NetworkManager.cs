namespace PiratesQuest;

using Godot;
using System;
using System.Net;
using System.Net.Sockets;

// This is an autoload singleton that persists across scene changes
public partial class NetworkManager : Node
{
  public override void _Ready()
  {
    GD.Print("NetworkManager ready");
  }

  public Error CreateServer(int port)
  {
    LogUdpBindDiagnostics(port);

    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateServer(port);
    if (error != Error.Ok)
    {
      GD.PrintErr($"Failed to create server: {error}");
      return error;
    }

    // Disconnect existing signal connections before connecting
    Disconnect();

    Multiplayer.ConnectedToServer += OnConnectOk;
    Multiplayer.ConnectionFailed += OnConnectionFail;

    Multiplayer.MultiplayerPeer = peer;
    GD.Print($"Server created on port {port}");
    return Error.Ok;
  }

  /// <summary>
  /// Fast UDP bind preflight to help diagnose ENet startup failures on Linux hosts.
  /// This does not keep the socket open; it only validates that bind is possible.
  /// </summary>
  private static void LogUdpBindDiagnostics(int port)
  {
    TryUdpBind(AddressFamily.InterNetwork, IPAddress.Any, port, "IPv4");
    TryUdpBind(AddressFamily.InterNetworkV6, IPAddress.IPv6Any, port, "IPv6");
  }

  private static void TryUdpBind(AddressFamily family, IPAddress ip, int port, string label)
  {
    try
    {
      using var socket = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
      socket.Bind(new IPEndPoint(ip, port));
      GD.Print($"UDP preflight bind ok ({label}) on port {port}");
    }
    catch (Exception exception)
    {
      GD.PrintErr($"UDP preflight bind failed ({label}) on port {port}: {exception.GetType().Name}: {exception.Message}");
    }
  }

  public Error CreateClient(string address, int port)
  {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateClient(address, port);
    if (error != Error.Ok)
    {
      GD.PrintErr($"Failed to create client: {error}");
      return error;
    }

    // Disconnect existing signal connections before connecting
    Disconnect();

    Multiplayer.ConnectedToServer += OnConnectOk;
    Multiplayer.ConnectionFailed += OnConnectionFail;

    Multiplayer.MultiplayerPeer = peer;
    GD.Print($"Client connecting to {address}:{port}");
    return Error.Ok;
  }

  private void Disconnect()
  {
    var connectOkCallable = Callable.From(OnConnectOk);
    var connectionFailCallable = Callable.From(OnConnectionFail);

    if (Multiplayer.IsConnected(MultiplayerApi.SignalName.ConnectedToServer, connectOkCallable))
    {
      Multiplayer.ConnectedToServer -= OnConnectOk;
    }

    if (Multiplayer.IsConnected(MultiplayerApi.SignalName.ConnectionFailed, connectionFailCallable))
    {
      Multiplayer.ConnectionFailed -= OnConnectionFail;
    }
  }

  private void OnConnectOk()
  {
    GD.Print("Connected to server successfully");
  }

  private void OnConnectionFail()
  {
    GD.PrintErr("Failed to connect to server");
  }
}
