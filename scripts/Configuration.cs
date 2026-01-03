using System;
using System.Linq;
using Godot;

partial class Configuration : Node
{
  public override void _Ready()
  {
    CallDeferred(MethodName.ConfigureWindowTitle);
  }

  private void ConfigureWindowTitle()
  {
    String title = $"Algonquin 1 {OS.GetCmdlineArgs().Join(" ")}";
    GD.Print($"setting title to {title}");
    DisplayServer.WindowSetTitle(title);
  }

  public static bool IsDesignatedServerMode()
  {
    var args = OS.GetCmdlineArgs();
    return args.Contains("--server");
  }
}