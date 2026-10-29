using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;

namespace TORSEPAN.Infrastructure.Persistence;

public class TORSEPANDbContext : DbContext
{
    public TORSEPANDbContext(DbContextOptions<TORSEPANDbContext> options)
        : base(options)
    {
    }

    public DbSet<OfflineDevice> OfflineDevices => Set<OfflineDevice>();
    public DbSet<OfflineReceipt> OfflineReceipts => Set<OfflineReceipt>();

    public DbSet<WorkPlan> WorkPlans => Set<WorkPlan>();
    public DbSet<User> Users => Set<User>();
    public DbSet<MessagePushKeys> MessagePushKeys => Set<MessagePushKeys>();
    public DbSet<MessagePushSubscription> MessagePushSubscriptions => Set<MessagePushSubscription>();
    public DbSet<MessagePushDelivery> MessagePushDeliveries => Set<MessagePushDelivery>();
    public DbSet<WorkshopMessage> WorkshopMessages => Set<WorkshopMessage>();
    public DbSet<WorkshopMessageReceipt> WorkshopMessageReceipts => Set<WorkshopMessageReceipt>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<Bowl> Bowls => Set<Bowl>();
    public DbSet<HandpanAssembly> HandpanAssemblies => Set<HandpanAssembly>();
    public DbSet<InstrumentWarehouse> InstrumentWarehouses => Set<InstrumentWarehouse>();

    public DbSet<Handpan> Handpans => Set<Handpan>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<Scale> Scales => Set<Scale>();
    public DbSet<CustomerOrder> CustomerOrders => Set<CustomerOrder>();
    public DbSet<CustomerOrderLine> CustomerOrderLines => Set<CustomerOrderLine>();
    public DbSet<OrderInstrument> OrderInstruments => Set<OrderInstrument>();
    public DbSet<OrderReminder> OrderReminders => Set<OrderReminder>();
    public DbSet<ProductionEvent> ProductionEvents => Set<ProductionEvent>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PayrollRate> PayrollRates => Set<PayrollRate>();
    public DbSet<PayrollPayment> PayrollPayments => Set<PayrollPayment>();
    public DbSet<MarketingLead> MarketingLeads => Set<MarketingLead>();
    public DbSet<MarketingActivity> MarketingActivities => Set<MarketingActivity>();
    public DbSet<DesignType> DesignTypes => Set<DesignType>();
    public DbSet<HandpanPhoto> HandpanPhotos => Set<HandpanPhoto>();
    public DbSet<AccountingParty> AccountingParties => Set<AccountingParty>();
    public DbSet<HallProfitPayment> HallProfitPayments => Set<HallProfitPayment>();
    public DbSet<SaleProfitRule> SaleProfitRules => Set<SaleProfitRule>();
    public DbSet<SaleProfitEntry> SaleProfitEntries => Set<SaleProfitEntry>();
    public DbSet<AccountingDocument> AccountingDocuments => Set<AccountingDocument>();
    public DbSet<SaleLeadSource> SaleLeadSources => Set<SaleLeadSource>();
    public DbSet<SalesReferrer> SalesReferrers => Set<SalesReferrer>();

    private void StampProductionVersions()
    {
        ChangeTracker.DetectChanges();
        foreach (var entry in ChangeTracker.Entries().Where(e => e.Entity is Bowl or Handpan or HandpanAssembly or Material
            && e.State is EntityState.Added or EntityState.Modified))
            entry.Property("OfflineVersion").CurrentValue = Guid.NewGuid();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampProductionVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampProductionVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<WorkPlan>(b => {
            b.HasKey(x => x.Id);
            b.Property(x => x.EffectiveDate).HasColumnType("date");
            b.Property(x => x.TasksJson).HasMaxLength(12000);
            b.Property(x => x.Notes).HasMaxLength(2000);
            b.Property(x => x.Revision).IsConcurrencyToken();
            b.HasIndex(x => new { x.UserId, x.EffectiveDate, x.IsException }).IsUnique();
            b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OfflineDevice>().ToTable("OfflineDevices", t => t.HasCheckConstraint("CK_OfflineDevices_Singleton", "\"Id\" = 1"));
        modelBuilder.Entity<OfflineDevice>().HasKey(x => x.Id);
        modelBuilder.Entity<OfflineDevice>().Property(x => x.Id).ValueGeneratedNever();
        modelBuilder.Entity<OfflineReceipt>().HasKey(x => x.OperationId);
        modelBuilder.Entity<OfflineReceipt>().HasIndex(x => x.WarrantyPending);
        modelBuilder.Entity<OfflineReceipt>().Property(x => x.PayloadJson).HasMaxLength(16384);
        modelBuilder.Entity<OfflineReceipt>().Property(x => x.PayloadHash).HasMaxLength(64);
        modelBuilder.Entity<OfflineReceipt>().HasIndex(x => new { x.DeviceId, x.AcceptedUtc });
        modelBuilder.Entity<Material>().Property(x => x.OfflineVersion).IsConcurrencyToken();
        modelBuilder.Entity<Bowl>().Property<Guid>("OfflineVersion").IsConcurrencyToken();
        modelBuilder.Entity<Handpan>().Property<Guid>("OfflineVersion").IsConcurrencyToken();
        modelBuilder.Entity<HandpanAssembly>().Property<Guid>("OfflineVersion").IsConcurrencyToken();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TORSEPANDbContext).Assembly);
        modelBuilder.Entity<Handpan>().HasIndex(x => x.SaleLeadSourceId);
        modelBuilder.Entity<Handpan>().HasIndex(x => x.SalesReferrerId);
    }
}
