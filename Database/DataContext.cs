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
}