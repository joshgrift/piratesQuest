namespace PiratesQuest.Server.Models;

public class GameServer
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Address { get; set; }
    public int Port { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
