using Microsoft.EntityFrameworkCore;
#pragma warning disable CS8618

namespace DotaHead.Database;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<PlayerDbo> Players { get; set; }
    public DbSet<MatchDbo> Matches { get; set; }
    public DbSet<ServerDbo> Servers { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ServerDbo>()
            .HasKey(e => e.GuildId);

        modelBuilder.Entity<MatchDbo>()
            .HasKey(e => new { e.GuildId, e.MatchId });

        modelBuilder.Entity<PlayerDbo>()
            .HasOne<ServerDbo>()
            .WithMany()
            .HasForeignKey(o => o.GuildId);

        modelBuilder.Entity<MatchDbo>()
            .HasOne<ServerDbo>()
            .WithMany()
            .HasForeignKey(o => o.GuildId);
    }
}