using Godot;
using Algonquin1;
using System;
using System.Collections.Generic;

public partial class CollectionPoint : Node3D
{
	private readonly List<ICollector> collectors = [];

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
			collector.Collect(InventoryItemType.Wood, 1);
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
