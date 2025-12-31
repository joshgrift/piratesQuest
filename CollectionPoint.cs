using Godot;
using Algonquin1;
using System;
using System.Collections.Generic;

public partial class CollectionPoint : Node3D
{
	private readonly List<ICollector> collectors = [];

	[Export] public InventoryItemType ResourceType = InventoryItemType.Wood;
	[Export] public int CollectionPerSecond = 1;

	public override void _Ready()
	{
		Area3D area = GetNode<Area3D>("Area3D");
		area.BodyEntered += OnBodyEntered;
		area.BodyExited += OnBodyExited;

		Timer collectionTimer = GetNode<Timer>("CollectionTimer");
		collectionTimer.Timeout += OnCollectionTimeout;
	}

	private void OnCollectionTimeout()
	{
		foreach (var collector in collectors)
		{
			collector.Collect(ResourceType, CollectionPerSecond);
		}
	}

	private void OnBodyEntered(Node3D body)
	{
		if (body is ICollector collector)
		{
			collectors.Add(collector);
		}
	}
	private void OnBodyExited(Node3D body)
	{
		if (body is ICollector collector)
		{
			collectors.Remove(collector);
		}
	}
}
