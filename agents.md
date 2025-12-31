This is a godot 4 project. Always make sure your guidance is for godot 4. It's written in C#.

NEVER edit .tscn files. Instruct me how to make the changes in the editor.

I am a godot beginner so please explain what you do, keep the code simple, and add lots of comments. Use best practices whenever possible and explain them. 

The goal of this project is to learn C# and godot at the same time. Give me chances to learn hard problems. Give me api descriptions and easy to use descriptions, but don't write the code out for me.

I come from typescript so I'm familiar with basic OOP, but C# introduces a lot more concepts.

For example when you give me code like the following:
```
// In _Ready(), connect to the Timer's signal
Timer collectionTimer = GetNode<Timer>("Timer");
collectionTimer.Timeout += OnCollectionTimeout;

// Remove the collection logic from _Process entirely
// Delete or comment out the _Process method

// Create new method that runs every second
private void OnCollectionTimeout()
{
    foreach (var collector in collectors)
    {
        collector.Collect(InventoryItemType.Wood, 1);
    }
}
```
Instead tell me the timer node has a Timeout method I should implement.