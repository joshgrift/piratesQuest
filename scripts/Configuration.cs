using System;
using System.Linq;
using Godot;

partial class Configuration : Node
{
  public static bool RandomSpawnEnabled { get; } = false;
  public static int StartingCoin { get; } = 100;
  public static bool IsCreative { get; } = true;

  public override void _Ready()
  {
    CallDeferred(MethodName.ConfigureWindowTitle);
  }

  private void ConfigureWindowTitle()
  {
    String title = $"PiratesQuest {OS.GetCmdlineArgs().Join(" ")}";
    GD.Print($"setting title to {title}");
    DisplayServer.WindowSetTitle(title);
  }

  public static string GetVersion()
  {
    return ProjectSettings.GetSetting("application/config/version").AsString();
  }

  public static bool IsDesignatedServerMode()
  {
    var args = OS.GetCmdlineArgs();

    return args.Contains("--server") || OS.HasFeature("dedicated_server");
  }
}