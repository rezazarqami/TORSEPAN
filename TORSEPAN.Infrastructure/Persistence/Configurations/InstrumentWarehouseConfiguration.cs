using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TORSEPAN.Domain.Entities;
namespace TORSEPAN.Infrastructure.Persistence.Configurations;
public sealed class InstrumentWarehouseConfiguration : IEntityTypeConfiguration<InstrumentWarehouse>
{
    public void Configure(EntityTypeBuilder<InstrumentWarehouse> b)
    {
        b.ToTable("InstrumentWarehouses", t => t.HasCheckConstraint("CK_InstrumentWarehouses_Three", "\"Id\" BETWEEN 1 AND 3"));
        b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(60);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x=>x.Name).IsUnique();
        b.HasData(new { Id = 1, Name = "Economy", Version = Guid.Parse("11111111-1111-4111-8111-111111111111") },
            new { Id = 2, Name = "Studio", Version = Guid.Parse("22222222-2222-4222-8222-222222222222") },
            new { Id = 3, Name = "Signature", Version = Guid.Parse("33333333-3333-4333-8333-333333333333") });
    }
}
