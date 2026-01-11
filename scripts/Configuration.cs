using System;
using System.Linq;
using Godot;

public record ServerListingInfo
{
  public string ServerName;
  public string IpAddress;
  public int Port;
}

partial class Configuration : Node
{
  public static bool RandomSpawnEnabled { get; } = false;
  public static int StartingCoin { get; } = 100;
  public static bool IsCreative { get; } = true;
  public static int DefaultPort { get; } = 7777;

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

  public static ServerListingInfo[] GetDefaultServerListings()
  {
    return
    [
      new ServerListingInfo
      {
        ServerName = "Localhost",
        IpAddress = "127.0.0.1",
        Port = DefaultPort
      },
      new ServerListingInfo
      {
        ServerName = "Sandbox",
        IpAddress = "sandbox.servers.pirates.quest",
        Port = DefaultPort
      }
    ];
  }
}