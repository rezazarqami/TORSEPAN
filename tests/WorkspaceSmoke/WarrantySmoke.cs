using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

internal static class WarrantySmoke
{
    public static void Run()
    {
        var handpan = new Handpan(Guid.NewGuid(), "WARRANTY-TEST");
        typeof(Handpan).GetProperty(nameof(Handpan.Stage))!.SetValue(handpan, ProductionStage.FinishedWarehouse);
        handpan.Sell("buyer", null, null, null, false, Guid.NewGuid());
        Check(handpan.WarrantyActivatedAt.HasValue, "selling automatically activates warranty");
        var activation = handpan.WarrantyActivatedAt;
        handpan.ActivateWarranty();
        handpan.UpdateSaleDetails("updated buyer", null, null, null);
        Check(handpan.WarrantyActivatedAt == activation, "repeat activation and sale editing preserve activation date");
        var rejected = new Handpan(Guid.NewGuid(), "NOT-IN-WAREHOUSE");
        try
        {
            rejected.Sell(null, null, null, null, true, Guid.NewGuid());
            throw new Exception("Invalid sale was accepted.");
        }
        catch (InvalidOperationException) { }
        Check(!rejected.WarrantyActivatedAt.HasValue, "rejected sale does not activate warranty");
        using var db = new TORSEPANDbContext(new DbContextOptionsBuilder<TORSEPANDbContext>()
            .UseNpgsql("Host=127.0.0.1;Database=unused;Username=unused;Password=unused").Options);
        var migration = db.GetService<IMigrator>().GenerateScript(
            "20260914050000_AddLocalWarrantyStatus", "20261003020000_ActivateSoldHandpanWarranties");
        Check(migration.Contains("WHERE \"Stage\" = 20 AND \"WarrantyActivatedAt\" IS NULL"),
            "backfill targets only sold instruments with inactive local warranty");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }
}
