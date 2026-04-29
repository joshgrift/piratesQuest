namespace PiratesQuest;

using Godot;
using PiratesQuest.AI;
using System.Collections.Generic;

public partial class AiDebugMenu : CanvasLayer
{
  private const float ListRefreshSeconds = 1.0f;
  private const float ZoomInFactor = 0.6f;
  private const float MinCameraDistanceFromShip = 12.0f;
  private const int ListPanelWidth = 320;
  private const int DetailPanelWidth = 380;
  private const float MinTimeScale = 0.25f;
  private const float MaxTimeScale = 8.0f;

  private VBoxContainer _shipList;
  private Label _detailLabel;
  private Label _timeScaleLabel;
  private SpinBox _timeScaleSpinBox;
  private Button _enableDebugButton;
  private Button _enableNavigationDebugButton;
  private Button _zoomInButton;
  private AiShip _selectedShip;
  private readonly Dictionary<AiShip, Button> _rows = new();
  private readonly Dictionary<string, Button> _spawnButtons = new();
  private Play _play;

  public override void _Ready()
  {
    if (!Multiplayer.IsServer())
    {
      QueueFree();
      return;
    }

    _play = GetTree().CurrentScene as Play;
    Layer = 100;
    BuildUi();

    var listTimer = new Timer { WaitTime = ListRefreshSeconds, Autostart = true };
    listTimer.Timeout += RefreshShipList;
    AddChild(listTimer);

    RefreshShipList();
  }

  private void BuildUi()
  {
    var listPanel = new PanelContainer();
    AddChild(listPanel);
    listPanel.AnchorLeft = 1.0f;
    listPanel.AnchorRight = 1.0f;
    listPanel.AnchorTop = 0.0f;
    listPanel.AnchorBottom = 1.0f;
    listPanel.OffsetLeft = -ListPanelWidth;
    listPanel.OffsetRight = 0;
    listPanel.OffsetTop = 0;
    listPanel.OffsetBottom = 0;

    var listScroll = new ScrollContainer
    {
      HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
    };
    listPanel.AddChild(listScroll);

    _shipList = new VBoxContainer
    {
      SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
    };
    listScroll.AddChild(_shipList);

    var header = new Label { Text = "AI Ships (server)" };
    _shipList.AddChild(header);

    var detailPanel = new PanelContainer();
    AddChild(detailPanel);
    detailPanel.AnchorLeft = 0.0f;
    detailPanel.AnchorRight = 0.0f;
    detailPanel.AnchorTop = 0.0f;
    detailPanel.AnchorBottom = 1.0f;
    detailPanel.OffsetLeft = 0;
    detailPanel.OffsetRight = DetailPanelWidth;
    detailPanel.OffsetTop = 0;
    detailPanel.OffsetBottom = 0;

    var margin = new MarginContainer();
    margin.AddThemeConstantOverride("margin_left", 12);
    margin.AddThemeConstantOverride("margin_right", 12);
    margin.AddThemeConstantOverride("margin_top", 12);
    margin.AddThemeConstantOverride("margin_bottom", 12);
    detailPanel.AddChild(margin);

    var detailLayout = new VBoxContainer
    {
      SizeFlagsVertical = Control.SizeFlags.ExpandFill,
    };
    detailLayout.AddThemeConstantOverride("separation", 10);
    margin.AddChild(detailLayout);

    _zoomInButton = new Button
    {
      Text = "Zoom In On Selected Ship",
      Disabled = true,
    };
    _zoomInButton.Pressed += ZoomInOnSelectedShip;
    detailLayout.AddChild(_zoomInButton);

    _timeScaleLabel = new Label
    {
      Text = "Game Speed: 1.00x",
    };
    detailLayout.AddChild(_timeScaleLabel);

    _timeScaleSpinBox = new SpinBox
    {
      MinValue = MinTimeScale,
      MaxValue = MaxTimeScale,
      Step = 0.25,
      Value = Engine.TimeScale,
    };
    _timeScaleSpinBox.ValueChanged += OnTimeScaleChanged;
    detailLayout.AddChild(_timeScaleSpinBox);

    var spawnHeader = new Label
    {
      Text = "Manual Spawn",
    };
    detailLayout.AddChild(spawnHeader);

    foreach (string archetypeId in AiShipDefinition.KnownIds)
    {
      var definition = AiShipDefinition.FromId(archetypeId);
      var spawnButton = new Button
      {
        Text = $"Spawn {definition.DisplayName}",
      };
      string capturedArchetypeId = archetypeId;
      spawnButton.Pressed += () => SpawnAiShip(capturedArchetypeId);
      detailLayout.AddChild(spawnButton);
      _spawnButtons[archetypeId] = spawnButton;
    }

    _enableDebugButton = new Button
    {
      Text = "Enable Debug On Selected Ship",
      Disabled = true,
    };
    _enableDebugButton.Pressed += EnableDebugOnSelectedShip;
    detailLayout.AddChild(_enableDebugButton);

    _enableNavigationDebugButton = new Button
    {
      Text = "Enable Rays + Patrol On Selected Ship",
      Disabled = true,
    };
    _enableNavigationDebugButton.Pressed += EnableNavigationDebugOnSelectedShip;
    detailLayout.AddChild(_enableNavigationDebugButton);

    _detailLabel = new Label
    {
      Text = "No ship selected",
      AutowrapMode = TextServer.AutowrapMode.WordSmart,
      VerticalAlignment = VerticalAlignment.Top,
      SizeFlagsVertical = Control.SizeFlags.ExpandFill,
    };
    detailLayout.AddChild(_detailLabel);
  }

  public override void _Process(double delta)
  {
    if (_timeScaleLabel != null)
      _timeScaleLabel.Text = $"Game Speed: {Engine.TimeScale:0.00}x";

    if (_timeScaleSpinBox != null && !Mathf.IsEqualApprox((float)_timeScaleSpinBox.Value, Engine.TimeScale))
      _timeScaleSpinBox.SetValueNoSignal(Engine.TimeScale);

    if (!IsInstanceValid(_selectedShip))
    {
      if (_selectedShip != null)
      {
        _selectedShip = null;
        _detailLabel.Text = "No ship selected";
        UpdateRowHighlights();
      }
      return;
    }

    var p = _selectedShip.GlobalPosition;
    var r = _selectedShip.Rotation;
    _detailLabel.Text = string.Join('\n', [
      $"Node: {_selectedShip.Name}",
      $"Pos: ({p.X:0.0}, {p.Y:0.0}, {p.Z:0.0})",
      $"Rot: ({Mathf.RadToDeg(r.X):0.0}°, {Mathf.RadToDeg(r.Y):0.0}°, {Mathf.RadToDeg(r.Z):0.0}°)",
      $"Ally: {_selectedShip.AllyTypeId}",
      $"Debug: {(_selectedShip.IsDebugEnabled ? "On" : "Off")}",
      $"Nav Debug: {(_selectedShip.IsNavigationDebugEnabled ? "On" : "Off")}",
      string.Empty,
      _selectedShip.BuildDebugText(),
    ]);
  }

  private void RefreshShipList()
  {
    var nodes = GetTree().GetNodesInGroup("ai_ships");

    var live = new HashSet<AiShip>();
    foreach (var node in nodes)
    {
      if (node is AiShip ship && IsInstanceValid(ship))
      {
        live.Add(ship);
      }
    }

    var toRemove = new List<AiShip>();
    foreach (var entry in _rows)
    {
      if (!live.Contains(entry.Key))
      {
        toRemove.Add(entry.Key);
      }
    }
    foreach (var ship in toRemove)
    {
      _rows[ship].QueueFree();
      _rows.Remove(ship);
    }

    foreach (var ship in live)
    {
      if (_rows.ContainsKey(ship)) continue;

      var button = new Button
      {
        Text = FormatRow(ship),
        Alignment = HorizontalAlignment.Left,
      };
      var captured = ship;
      button.Pressed += () =>
      {
        _selectedShip = captured;
        UpdateRowHighlights();
      };
      _shipList.AddChild(button);
      _rows[ship] = button;
    }

    foreach (var entry in _rows)
    {
      entry.Value.Text = FormatRow(entry.Key);
    }

    UpdateRowHighlights();
  }

  private static string FormatRow(AiShip ship)
  {
    return $"{ship.DisplayName} [{ship.ArchetypeId}] HP {ship.Health}/{ship.MaxHealth}";
  }

  private void UpdateRowHighlights()
  {
    foreach (var entry in _rows)
    {
      bool isSelected = entry.Key == _selectedShip;
      entry.Value.Modulate = isSelected ? new Color(1.0f, 0.85f, 0.4f) : Colors.White;
    }

    if (_zoomInButton != null)
    {
      _zoomInButton.Disabled = !IsInstanceValid(_selectedShip);
    }

    if (_enableDebugButton != null)
    {
      bool hasSelectedShip = IsInstanceValid(_selectedShip);
      bool canEnable = hasSelectedShip && !_selectedShip.IsDebugEnabled;
      _enableDebugButton.Disabled = !canEnable;
      _enableDebugButton.Text = !hasSelectedShip
        ? "Select A Ship To Enable Debug"
        : canEnable
          ? "Enable Debug On Selected Ship"
          : "Debug Already Enabled";
    }

    if (_enableNavigationDebugButton != null)
    {
      bool hasSelectedShip = IsInstanceValid(_selectedShip);
      bool canEnable = hasSelectedShip && !_selectedShip.IsNavigationDebugEnabled;
      _enableNavigationDebugButton.Disabled = !canEnable;
      _enableNavigationDebugButton.Text = !hasSelectedShip
        ? "Select A Ship To Enable Rays + Patrol"
        : canEnable
          ? "Enable Rays + Patrol On Selected Ship"
          : "Rays + Patrol Already Enabled";
    }

    foreach (var entry in _spawnButtons)
    {
      bool canSpawn = _play?.AiShipManager?.CanManuallySpawn(entry.Key) == true;
      entry.Value.Disabled = !canSpawn;
      entry.Value.Text = canSpawn
        ? $"Spawn {AiShipDefinition.FromId(entry.Key).DisplayName}"
        : $"Spawn {AiShipDefinition.FromId(entry.Key).DisplayName} (Unavailable)";
    }
  }

  private void ZoomInOnSelectedShip()
  {
    if (!IsInstanceValid(_selectedShip))
    {
      _detailLabel.Text = "No ship selected";
      UpdateRowHighlights();
      return;
    }

    // In server mode this is the active FreeCam. We still fetch the active camera
    // from the viewport so the debug menu works even if that setup changes later.
    var camera = GetViewport()?.GetCamera3D();
    if (camera == null)
    {
      GD.PrintErr("AI Debug Menu: cannot zoom - no active camera found.");
      return;
    }

    Vector3 shipPosition = _selectedShip.GlobalPosition;
    Vector3 shipToCamera = camera.GlobalPosition - shipPosition;

    // If the camera is exactly on the ship, pick a stable fallback direction.
    if (shipToCamera.LengthSquared() < 0.0001f)
    {
      shipToCamera = (_selectedShip.GlobalBasis.Z + (Vector3.Up * 0.35f)).Normalized();
    }

    float currentDistance = shipToCamera.Length();
    float nextDistance = Mathf.Max(MinCameraDistanceFromShip, currentDistance * ZoomInFactor);
    Vector3 directionFromShip = shipToCamera.Normalized();

    camera.GlobalPosition = shipPosition + (directionFromShip * nextDistance);
    camera.LookAt(shipPosition, Vector3.Up);
  }

  private void EnableDebugOnSelectedShip()
  {
    if (!IsInstanceValid(_selectedShip))
    {
      _detailLabel.Text = "No ship selected";
      UpdateRowHighlights();
      return;
    }

    _selectedShip.SetDebugEnabled(true);
    UpdateRowHighlights();
  }

  private void EnableNavigationDebugOnSelectedShip()
  {
    if (!IsInstanceValid(_selectedShip))
    {
      _detailLabel.Text = "No ship selected";
      UpdateRowHighlights();
      return;
    }

    _selectedShip.SetNavigationDebugEnabled(true);
    UpdateRowHighlights();
  }

  private void OnTimeScaleChanged(double value)
  {
    Engine.TimeScale = Mathf.Clamp((float)value, MinTimeScale, MaxTimeScale);
    if (_timeScaleLabel != null)
      _timeScaleLabel.Text = $"Game Speed: {Engine.TimeScale:0.00}x";
  }

  private void SpawnAiShip(string archetypeId)
  {
    bool spawned = _play?.AiShipManager?.TrySpawnManualShip(archetypeId) == true;
    if (!spawned && _detailLabel != null)
      _detailLabel.Text = $"Could not spawn {archetypeId}.";

    RefreshShipList();
  }
}
