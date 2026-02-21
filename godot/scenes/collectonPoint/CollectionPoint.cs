using Godot;
using PiratesQuest.Data;
using PiratesQuest.Attributes;
using System.Collections.Generic;

public partial class CollectionPoint : Node3D, IDropper
{
  // HashSet prevents duplicate entries if BodyEntered fires multiple times
  // for the same collector (which can happen with complex colliders).
  private readonly HashSet<ICanCollect> _collectors = [];

  private Timer _collectionTimer;
  private BoxMesh _progressFillMesh;
  private Tween _pulseTween;

  [Export] public InventoryItemType ResourceType = InventoryItemType.Wood;
  [Export] public int CollectionPerSecond = 4;
  [Export] public float CollectionSpeed = 2.0f;
  [Export] public InteractionPoint DockingArea;

  // ===== Visual feedback settings =====
  // These are optional references. If they are not set, collection still works.
  [ExportGroup("Feedback")]
  [Export] public Node3D FeedbackRoot;
  [Export] public MeshInstance3D FeedbackFill;
  [Export] public float FeedbackBarWidth = 2.2f;
  [Export] public float FeedbackBarHeight = 0.18f;
  [Export] public float MinVisibleBarWidth = 0.08f;

  public override void _Ready()
  {
    DockingArea.InteractionArea.BodyEntered += OnBodyEntered;
    DockingArea.InteractionArea.BodyExited += OnBodyExited;

    _collectionTimer = GetNode<Timer>("CollectionTimer");
    _collectionTimer.Timeout += OnCollectionTimeout;
    _collectionTimer.WaitTime = CollectionSpeed;
    _collectionTimer.Start();

    // Duplicate the fill mesh so this collection point can edit its own bar
    // without affecting other collection points that share the same scene.
    if (FeedbackFill?.Mesh is BoxMesh boxMesh)
    {
      _progressFillMesh = (BoxMesh)boxMesh.Duplicate();
      FeedbackFill.Mesh = _progressFillMesh;
    }

    UpdateFeedbackVisual(0.0f, false);
  }

  public override void _Process(double delta)
  {
    bool hasCollectors = _collectors.Count > 0;

    if (!hasCollectors || _collectionTimer == null || _collectionTimer.WaitTime <= 0.0)
    {
      UpdateFeedbackVisual(0.0f, false);
      return;
    }

    // Timer fills from 0 -> 1 while waiting for the next resource payout.
    float progress = 1.0f - (float)(_collectionTimer.TimeLeft / _collectionTimer.WaitTime);
    UpdateFeedbackVisual(progress, true);
  }

  public override void _ExitTree()
  {
    // Unsubscribe signals as a safety best practice.
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
    foreach (var collector in _collectors)
    {
      collector.CollectResource(ResourceType, CollectionPerSecond);
    }

    // A tiny "pop" makes each successful collection tick feel responsive.
    if (_collectors.Count > 0)
    {
      PlayFeedbackPulse();
    }
  }

  private void OnBodyEntered(Node3D body)
  {
    if (body is ICanCollect collector)
    {
      _collectors.Add(collector);
      UpdateFeedbackVisual(0.0f, true);
    }
  }

  private void OnBodyExited(Node3D body)
  {
    if (body is ICanCollect collector)
    {
      _collectors.Remove(collector);
      if (_collectors.Count == 0)
      {
        UpdateFeedbackVisual(0.0f, false);
      }
    }
  }

  /// <summary>
  /// Updates the world-space progress bar shown above the collection point.
  /// </summary>
  private void UpdateFeedbackVisual(float progress, bool isVisible)
  {
    if (FeedbackRoot == null || FeedbackFill == null || _progressFillMesh == null)
    {
      return;
    }

    FeedbackRoot.Visible = isVisible;
    if (!isVisible) return;

    float normalized = Mathf.Clamp(progress, 0.0f, 1.0f);
    float currentWidth = Mathf.Lerp(MinVisibleBarWidth, FeedbackBarWidth, normalized);

    // Resize the mesh to represent progress.
    _progressFillMesh.Size = new Vector3(currentWidth, FeedbackBarHeight, FeedbackBarHeight);

    // Keep bar filling from left -> right.
    float leftAlignedOffset = -(FeedbackBarWidth - currentWidth) * 0.5f;
    FeedbackFill.Position = new Vector3(leftAlignedOffset, 0.0f, 0.0f);
  }

  /// <summary>
  /// Quick scale pulse each time resources are granted.
  /// </summary>
  private void PlayFeedbackPulse()
  {
    if (FeedbackRoot == null) return;

    _pulseTween?.Kill();
    _pulseTween = CreateTween();
    _pulseTween.TweenProperty(FeedbackRoot, "scale", new Vector3(1.12f, 1.12f, 1.12f), 0.08f);
    _pulseTween.TweenProperty(FeedbackRoot, "scale", Vector3.One, 0.12f);
  }
}
