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
    CallDeferred(MethodName.OnStateChange);
  }

  private void OnPlayerDepartedPort(Port port, Player player)
  {
    if (_player == null || player.Name != _player.Name) return;
    GD.Print($"Player {player.Name} departed port");
    _currentPort = null;
    CallDeferred(MethodName.OnStateChange);
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

    if (msg is ReadyMessage)
    {
      _webViewLoaded = true;
      CallDeferred(MethodName.SyncWebViewSize);
      CallDeferred(MethodName.OnStateChange);
      return;
    }

    if (!HudIpcActionMap.Handlers.TryGetValue(msg.Action, out var handler))
    {
      GD.PushError($"HUD: Unknown IPC action '{msg.Action}'");
      return;
    }

    handler(msg, _player, _currentPort);
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
}
