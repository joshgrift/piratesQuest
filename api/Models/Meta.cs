namespace PiratesQuest.Server.Models;

public class Meta
{
    public int Id { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
