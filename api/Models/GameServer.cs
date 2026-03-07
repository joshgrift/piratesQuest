namespace PiratesQuest.Server.Models;

public class GameServer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public required string Address { get; set; }
    public int Port { get; set; }
    public bool IsActive { get; set; } = true;
    // Updated by the dedicated server heartbeat endpoint.
    // Null means this server has never checked in.
    public DateTime? LastSeenUtc { get; set; }
    // Runtime values persisted by heartbeat so API stays stateless.
    public int PlayerCount { get; set; }
    public int PlayerMax { get; set; } = 8;
    public string ServerVersion { get; set; } = "unknown";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
