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
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Bowl> Bowls => Set<Bowl>();
    public DbSet<HandpanAssembly> HandpanAssemblies => Set<HandpanAssembly>();
    public DbSet<Handpan> Handpans => Set<Handpan>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Scale> Scales => Set<Scale>();
    public DbSet<ProductionEvent> ProductionEvents => Set<ProductionEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PayrollRate> PayrollRates => Set<PayrollRate>();
    public DbSet<PayrollPayment> PayrollPayments => Set<PayrollPayment>();
    public DbSet<MarketingLead> MarketingLeads => Set<MarketingLead>();
    public DbSet<MarketingActivity> MarketingActivities => Set<MarketingActivity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TORSEPANDbContext).Assembly);
    }
}
