using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence;

public class TORSEPANDbContext : DbContext
{
    public TORSEPANDbContext(DbContextOptions<TORSEPANDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Bowl> Bowls => Set<Bowl>();

    public DbSet<HandpanAssembly> HandpanAssemblies => Set<HandpanAssembly>();

    public DbSet<Handpan> Handpans => Set<Handpan>();

    public DbSet<ProductionEvent> ProductionEvents => Set<ProductionEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TORSEPANDbContext).Assembly);
    }
}