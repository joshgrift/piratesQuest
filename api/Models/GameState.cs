namespace PiratesQuest.Server.Models;

public class GameState
{
    public int Id { get; set; }
    public int ServerId { get; set; }
    public required string UserId { get; set; }
    public required string State { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public GameServer Server { get; set; } = null!;
}
