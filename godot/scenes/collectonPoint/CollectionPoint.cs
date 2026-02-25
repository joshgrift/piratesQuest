using Godot;
using PiratesQuest;
using PiratesQuest.Data;
using PiratesQuest.Attributes;
using System.Collections.Generic;

public partial class CollectionPoint : Node3D, IDropper
{
  // HashSet prevents duplicate entries if BodyEntered fires multiple times
  // for the same collector (which can happen with complex colliders).
  private readonly HashSet<ICanCollect> _collectors = [];

  private Timer _collectionTimer;

  // The compass shader material — we set its "progress" uniform each frame.
  private ShaderMaterial _shaderMaterial;
  private Tween _pulseTween;

  // True when all collectors failed their last collection attempt (inventory full).
  // Drives the "blocked" visual on the compass shader.
  private bool _isBlocked = false;

  [Export] public InventoryItemType ResourceType = InventoryItemType.Wood;
  [Export] public int CollectionPerSecond = 4;
  [Export] public float CollectionSpeed = 2.0f;
  [Export] public InteractionPoint DockingArea;

  // ===== Visual feedback =====
  // FeedbackRoot controls visibility; FeedbackRing holds the compass shader mesh.
  [ExportGroup("Feedback")]
  [Export] public Node3D FeedbackRoot;
  [Export] public MeshInstance3D FeedbackRing;

  public override void _Ready()
  {
    DockingArea.InteractionArea.BodyEntered += OnBodyEntered;
    DockingArea.InteractionArea.BodyExited += OnBodyExited;

    _collectionTimer = GetNode<Timer>("CollectionTimer");
    _collectionTimer.Timeout += OnCollectionTimeout;
    _collectionTimer.WaitTime = CollectionSpeed;
    _collectionTimer.Start();

    // Duplicate the shader material so each collection point instance
    // drives its own progress value independently.
    if (FeedbackRing?.MaterialOverride is ShaderMaterial mat)
    {
      _shaderMaterial = (ShaderMaterial)mat.Duplicate();
      FeedbackRing.MaterialOverride = _shaderMaterial;

      // Pass this point's resource icon to the compass shader so the
      // center shows what's being collected (wood, fish, iron, etc.).
      Texture2D icon = Icons.GetInventoryIcon(ResourceType);
      _shaderMaterial.SetShaderParameter("resource_icon", icon);
    }

    SetProgress(0.0f, false);
  }

  public override void _Process(double delta)
  {
    bool hasCollectors = _collectors.Count > 0;

    if (!hasCollectors || _collectionTimer == null || _collectionTimer.WaitTime <= 0.0)
    {
      SetProgress(0.0f, false);
      return;
    }

    if (_isBlocked)
    {
      // Ship is full — show the compass locked at 100% in "blocked" colours.
      SetProgress(1.0f, true);
      _shaderMaterial?.SetShaderParameter("blocked", 1.0f);
      return;
    }

    _shaderMaterial?.SetShaderParameter("blocked", 0.0f);

    // Timer fills from 0 -> 1 while waiting for the next resource payout.
    float progress = 1.0f - (float)(_collectionTimer.TimeLeft / _collectionTimer.WaitTime);
    SetProgress(progress, true);
  }

  public override void _ExitTree()
  {
    if (DockingArea?.InteractionArea != null)
    {
      DockingArea.InteractionArea.BodyEntered -= OnBodyEntered;
      DockingArea.InteractionArea.BodyExited -= OnBodyExited;
    }

    if (_collectionTimer != null)
    {
      _collectionTimer.Timeout -= OnCollectionTimeout;
    }
  }

  private void OnCollectionTimeout()
  {
    bool anySucceeded = false;
    foreach (var collector in _collectors)
    {
      if (collector.CollectResource(ResourceType, CollectionPerSecond))
        anySucceeded = true;
    }

    if (_collectors.Count > 0)
    {
      _isBlocked = !anySucceeded;
      if (anySucceeded) PlayFeedbackPulse();
    }
  }

  private void OnBodyEntered(Node3D body)
  {
    if (body is ICanCollect collector)
    {
      _collectors.Add(collector);
      _isBlocked = false; // re-evaluate on next timeout
      SetProgress(0.0f, true);
    }
  }

  private void OnBodyExited(Node3D body)
  {
    if (body is ICanCollect collector)
    {
      _collectors.Remove(collector);
      if (_collectors.Count == 0)
      {
        _isBlocked = false;
        SetProgress(0.0f, false);
      }
    }
  }

  /// <summary>
  /// Sends the current progress (0–1) to the compass ring shader.
  /// </summary>
  private void SetProgress(float progress, bool isVisible)
  {
    if (FeedbackRoot == null || _shaderMaterial == null) return;

    FeedbackRoot.Visible = isVisible;
    if (!isVisible) return;

    _shaderMaterial.SetShaderParameter("progress", Mathf.Clamp(progress, 0.0f, 1.0f));
  }

  /// <summary>
  /// Quick scale pop each time resources are collected for tactile feedback.
  /// </summary>
  private void PlayFeedbackPulse()
  {
    if (FeedbackRoot == null) return;

    _pulseTween?.Kill();
    _pulseTween = CreateTween();
    _pulseTween.TweenProperty(FeedbackRoot, "scale", new Vector3(1.15f, 1.15f, 1.15f), 0.08f);
    _pulseTween.TweenProperty(FeedbackRoot, "scale", Vector3.One, 0.15f);
  }
}
