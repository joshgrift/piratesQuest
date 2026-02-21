namespace PiratesQuest;

using Godot;

// This is an autoload singleton that persists across scene changes
public partial class NetworkManager : Node
{
  public override void _Ready()
  {
    GD.Print("NetworkManager ready");
  }

  public void CreateServer(int port)
  {
    var peer = new ENetMultiplayerPeer();
    var error = peer.CreateServer(port);
    if (error != Error.Ok)
    {
      GD.PrintErr($"Failed to create server: {error}");
      return;
    }

    // Disconnect existing signal connections before connecting
    Disconnect();

    Multiplayer.ConnectedToServer += OnConnectOk;
    Multiplayer.ConnectionFailed += OnConnectionFail;

    Multiplayer.MultiplayerPeer = peer;
    GD.Print($"Server created on port {port}");
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
