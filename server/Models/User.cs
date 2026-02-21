namespace PiratesQuest.Server.Models;

public enum UserRole
{
    Player,
    Mod,
    Admin
}

public class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; } = UserRole.Player;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
