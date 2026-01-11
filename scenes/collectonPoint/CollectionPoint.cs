using Godot;
using PiratesQuest.Data;
using PiratesQuest.Attributes;
using System.Collections.Generic;

public partial class CollectionPoint : Node3D, IDropper
{
	private readonly List<ICanCollect> collectors = [];

	[Export] public InventoryItemType ResourceType = InventoryItemType.Wood;
	[Export] public int CollectionPerSecond = 4;

	public override void _Ready()
	{
		Area3D area = GetNode<Area3D>("Area3D");
		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;

		Timer collectionTimer = GetNode<Timer>("CollectionTimer");
		collectionTimer.Timeout += OnCollectionTimeout;
		collectionTimer.WaitTime = 2.0f;
		collectionTimer.Start();
	}

	private void OnCollectionTimeout()
	{
		foreach (var collector in collectors)
		{
			collector.CollectResource(ResourceType, CollectionPerSecond);
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is ICanCollect collector)
		{
			collectors.Add(collector);
		}
	}
	private void OnBodyExited(Node3D body)
	{
		if (body is ICanCollect collector)
		{
			collectors.Remove(collector);
		}
	}
}
