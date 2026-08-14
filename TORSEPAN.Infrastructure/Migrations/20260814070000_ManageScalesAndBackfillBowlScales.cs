using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Infrastructure.Persistence;

#nullable disable
namespace TORSEPAN.Infrastructure.Migrations;
[DbContext(typeof(TORSEPANDbContext))]
[Migration("20260814070000_ManageScalesAndBackfillBowlScales")]
public partial class ManageScalesAndBackfillBowlScales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(name:"IsActive",table:"Scales",type:"boolean",nullable:false,defaultValue:true);
        migrationBuilder.Sql("INSERT INTO \"Scales\" (\"Id\",\"Name\",\"IsActive\") SELECT gen_random_uuid(),'9 نت',TRUE WHERE NOT EXISTS (SELECT 1 FROM \"Scales\" WHERE \"Name\"='9 نت');");
        migrationBuilder.Sql("INSERT INTO \"Scales\" (\"Id\",\"Name\",\"IsActive\") SELECT gen_random_uuid(),'2 نت زیر F-G',TRUE WHERE NOT EXISTS (SELECT 1 FROM \"Scales\" WHERE \"Name\"='2 نت زیر F-G');");
        migrationBuilder.Sql("UPDATE \"Bowls\" SET \"ScaleId\"=(SELECT \"Id\" FROM \"Scales\" WHERE \"Name\"='9 نت' LIMIT 1) WHERE \"ScaleId\" IS NULL AND \"Stage\" <> 2;");
        migrationBuilder.Sql("UPDATE \"Bowls\" SET \"ScaleId\"=(SELECT \"Id\" FROM \"Scales\" WHERE \"Name\"='2 نت زیر F-G' LIMIT 1) WHERE \"BowlType\"=2 AND \"HasNotes\"=TRUE AND \"Stage\" <> 2;");
    }
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(name:"IsActive",table:"Scales");
}
