using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Services;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Offline;

// Sale commits offline independently of the remote guarantee service. Retry durable work afterwards.
public sealed class OfflineWarrantySyncWorker(IServiceScopeFactory scopes, ILogger<OfflineWarrantySyncWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(2));
        do {
            try {
                using var scope=scopes.CreateScope();
                await SyncAsync(scope.ServiceProvider.GetRequiredService<TORSEPANDbContext>(),
                    scope.ServiceProvider.GetRequiredService<GuaranteeServiceClient>(),stoppingToken);
            }catch(OperationCanceledException)when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception error){log.LogWarning("Offline warranty retry failed: {Type}",error.GetType().Name);}
        }while(await timer.WaitForNextTickAsync(stoppingToken));
    }
    public static async Task SyncAsync(TORSEPANDbContext db, GuaranteeServiceClient guarantee, CancellationToken ct)
    {
        var receipts=await db.OfflineReceipts.Where(x=>x.WarrantyPending).OrderBy(x=>x.AcceptedUtc).Take(50).ToListAsync(ct);
        foreach(var receipt in receipts){
            var op=JsonSerializer.Deserialize<OfflineOperation>(receipt.PayloadJson)!;
            var code=op.WorkspaceModule=="production"&&op.WorkspaceAction=="saleUpdate"?op.WorkspaceData!.Value.GetProperty("code").GetString():op.Code;
            var saleUtc=op.WorkspaceModule=="production"&&op.WorkspaceAction=="saleUpdate"?op.WorkspaceData!.Value.GetProperty("soldAt").GetDateTime():receipt.OccurredUtc;
            var bowl=await db.Bowls.AsNoTracking().SingleOrDefaultAsync(x=>x.ProductionCode==code,ct);
            var a=bowl is null?null:await db.HandpanAssemblies.AsNoTracking().SingleOrDefaultAsync(x=>x.TopBowlId==bowl.Id||x.BottomBowlId==bowl.Id,ct);
            var h=a is null?null:await db.Handpans.AsNoTracking().SingleOrDefaultAsync(x=>x.AssemblyId==a.Id,ct);
            // A returned/resold item must not be reactivated for an obsolete buyer.
            if(h is null || h.Stage!=ProductionStage.Sold || h.SoldAt!=saleUtc){receipt.WarrantyPending=false;await db.SaveChangesAsync(ct);continue;}
            var result=await guarantee.ActivateAsync(h.SerialNumber,h.BuyerName??"ثبت از فروش",h.BuyerPhoneNumber??"","",ct);
            if(result.IsActive){receipt.WarrantyPending=false;await db.SaveChangesAsync(ct);}
        }
    }
}
