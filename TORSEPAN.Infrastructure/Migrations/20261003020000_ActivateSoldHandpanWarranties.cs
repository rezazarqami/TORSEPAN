using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable

namespace TORSEPAN.Infrastructure.Migrations;

[DbContext(typeof(TORSEPANDbContext))]
[Migration("20261003020000_ActivateSoldHandpanWarranties")]
public sealed class ActivateSoldHandpanWarranties : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("""
            UPDATE "Handpans"
            SET "WarrantyActivatedAt" = CURRENT_TIMESTAMP,
                "UpdatedAt" = CURRENT_TIMESTAMP
            WHERE "Stage" = 20 AND "WarrantyActivatedAt" IS NULL;
            """);

    // Activation is a business-data correction; rollback must not revoke warranties.
    protected override void Down(MigrationBuilder migrationBuilder) { }
}
