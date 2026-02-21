using Godot;
using System;

public partial class TerrainBlock : Node3D
{
  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
    // Get the Display node
    var displayNode = GetNode("Display");

    // Get all children of the Display node
    var children = displayNode.GetChildren();

    // If there are no children, exit early
    if (children.Count == 0)
    {
      return;
    }

    // Create a random number generator
    var random = new Random();

    // Pick a random index
    int randomIndex = random.Next(0, children.Count);

    // Generate a random rotation around the Y axis (0 to 360 degrees)
    float randomRotationDegrees = (float)(random.NextDouble() * 360.0);

    // Convert degrees to radians for Godot
    float randomRotationRadians = Mathf.DegToRad(randomRotationDegrees);

    // Generate a random scale between 0.5 and 1.5
    float randomScale = 0.5f + (float)(random.NextDouble() * 1.0);

    // Loop through all children
    for (int i = 0; i < children.Count; i++)
    {
      var child = children[i];

      // Make the randomly selected child visible, all others invisible
      if (child is Node3D node3D)
      {
        node3D.Visible = (i == randomIndex);
      }
    }

    // Apply the random rotation to this TerrainBlock
    RotationDegrees = new Vector3(0, randomRotationDegrees, 0);

    // Apply the random scale uniformly to all axes
    Scale = new Vector3(randomScale, randomScale, randomScale);
  }
}
