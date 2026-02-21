using Microsoft.EntityFrameworkCore;
using PiratesQuest.Server.Models;

namespace PiratesQuest.Server.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<GameServer> GameServers => Set<GameServer>();
    public DbSet<GameState> GameStates => Set<GameState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<GameState>(e =>
        {
            e.HasIndex(s => new { s.ServerId, s.UserId }).IsUnique();
            e.Property(s => s.State).HasColumnType("jsonb");
            e.HasOne(s => s.Server)
                .WithMany()
                .HasForeignKey(s => s.ServerId);
        });
    }
}
