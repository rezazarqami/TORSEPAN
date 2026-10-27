using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Application.Bowls.Dimpling;
using TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;
using TORSEPAN.Application.Materials.Commands.AdjustBowlStock;
using TORSEPAN.Application.Materials.Commands.AdjustMaterialStock;
using TORSEPAN.Application.Sales;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.API.Offline;

public sealed partial class OfflineProductionService
{
    internal static Guid[] MaterialTargets(OfflineOperation op) => op.Action switch {
        "create" or "stock" => op.MaterialId.HasValue ? [op.MaterialId.Value] : [],
        "packaging" => op.MaterialIds?.Distinct().ToArray() ?? [], _ => []
    };
    private async Task CheckMaterialsAsync(OfflineOperation op, CancellationToken ct)
    {
        var ids = MaterialTargets(op);
        if (op.Action is "create" or "stock" && ids.Length != 1)
            throw new OfflineProblem("material", "متریال را انتخاب کنید.",400);
        if (ids.Length > 30) throw new OfflineProblem("material", "تعداد اقلام بیش از حد است.",400);
        foreach (var id in ids) {
            var material = await db.Materials.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id,ct)
                ?? throw new OfflineProblem("material", "متریال پیدا نشد.",404);
            string? version = op.MaterialVersions?.GetValueOrDefault(id.ToString());
            if (op.MaterialPreviousOperations?.TryGetValue(id.ToString(), out var prev) == true) {
                var receipt = await db.OfflineReceipts.AsNoTracking().SingleOrDefaultAsync(x=>x.OperationId==prev && x.DeviceId==op.DeviceId,ct);
                version = receipt is null ? null : JsonSerializer.Deserialize<Dictionary<string,string>>(receipt.VersionsJson)?.GetValueOrDefault("material:"+id);
            }
            if (version != material.OfflineVersion.ToString()) throw new OfflineProblem("stock-conflict", "موجودی این متریال در سرور تغییر کرده؛ عملیات محلی حفظ شد.");
        }
    }
    private static void Require(bool valid, string message) {
        if (!valid) throw new OfflineProblem("validation",message,422);
    }
    private async Task<Handpan> InstrumentAsync(OfflineBowl bowl, CancellationToken ct) {
        Require(bowl.InstrumentCode is not null,"این کاسه هنوز ساز نشده است.");
        return await db.Handpans.SingleAsync(x=>x.SerialNumber==bowl.InstrumentCode,ct);
    }
    private async Task<Result<BowlDimpleDto>> ApplyExtendedAsync(OfflineOperation op, OfflineBowl? bowl, CancellationToken ct)
    {
        switch(op.Action) {
            case "create":
                Require(System.Text.RegularExpressions.Regex.IsMatch(op.Code,"^[A-Z0-9][A-Z0-9-]{0,49}$"),"کد کاسه باید با حروف لاتین، عدد و خط تیره نوشته شود.");
                Require(Enum.IsDefined((BowlType)op.BowlType) && Enum.IsDefined((InstrumentType)op.InstrumentType),"نوع کاسه معتبر نیست.");
                var created = await mediator.Send(new CreateBowlCommand { ProductionCode=op.Code,BowlType=(BowlType)op.BowlType,
                    HasNotes=op.HasNotes,InstrumentType=(InstrumentType)op.InstrumentType,MaterialId=op.MaterialId!.Value },ct);
                if (created.IsFailure) throw new OfflineProblem(created.Error.Code,created.Error.Message,422);
                break;
            case "stock":
                Require(op.Code=="@"+op.MaterialId,"شناسه موجودی معتبر نیست.");
                var m = await db.Materials.AsNoTracking().SingleAsync(x=>x.Id==op.MaterialId,ct);
                Require(op.Quantity>=0 && op.TopQuantity>=0 && op.BottomQuantity>=0 && op.Quantity<=1_000_000 && op.TopQuantity<=1_000_000 && op.BottomQuantity<=1_000_000,"مقدار موجودی معتبر نیست.");
                if(m.Category==MaterialCategory.BowlMaterial) {
                    Require(op.SetAbsolute || op.TopQuantity+op.BottomQuantity>0,"مقدار ورودی را وارد کنید.");
                    await mediator.Send(new AdjustBowlStockCommand(m.Id,op.TopQuantity,op.BottomQuantity,op.SetAbsolute),ct);
                } else {
                    Require(op.SetAbsolute || op.Quantity>0,"مقدار ورودی را وارد کنید.");
                    await mediator.Send(new AdjustMaterialStockCommand(m.Id,op.Quantity,op.SetAbsolute),ct);
                }
                break;
            case "packaging":
                Require(op.ExportLocation is null || Enum.IsDefined((ExportWarehouseLocation)op.ExportLocation),"مقصد انبار معتبر نیست.");
                return await mediator.Send(new CompleteHandpanPackagingCommand(op.Code,op.MaterialIds??[],
                    op.ExportLocation.HasValue?(ExportWarehouseLocation)op.ExportLocation.Value:null),ct);
            case "tuneExport": return await mediator.Send(new CompleteBowlTuneForExportCommand(op.Code,(OperationDuration)op.Duration),ct);
            case "exportRoute": return await mediator.Send(new SendGlueBowlToExportCommand(op.Code),ct);
            case "returnGlue": return await mediator.Send(new ReturnExportBowlToGlueCommand(op.Code),ct);
            case "exportPackaging":
                Require(op.ExportLocation.HasValue && Enum.IsDefined((ExportWarehouseLocation)op.ExportLocation.Value),"مقصد انبار لازم است.");
                return await mediator.Send(new CompleteExportPackagingCommand(op.Code,(ExportWarehouseLocation)op.ExportLocation!.Value),ct);
            case "sell":
                Require(op.BuyerName?.Length<=200 || op.BuyerName is null,"نام خریدار طولانی است.");
                Require(op.BuyerPhoneNumber?.Length<=50 || op.BuyerPhoneNumber is null,"شماره خریدار طولانی است.");
                Require(op.Destination?.Length<=200 || op.Destination is null,"مقصد طولانی است.");
                Require(TORSEPAN.Domain.Entities.SaleMoney.Valid(op.Price,(TORSEPAN.Domain.Entities.MoneyCurrency)(op.PriceCurrency==0?1:op.PriceCurrency))&&TORSEPAN.Domain.Entities.SaleMoney.Valid(op.ReceivedAmount,(TORSEPAN.Domain.Entities.MoneyCurrency)(op.PriceCurrency==0?1:op.PriceCurrency))&&TORSEPAN.Domain.Entities.SaleMoney.Valid(op.NetProfit,(TORSEPAN.Domain.Entities.MoneyCurrency)(op.ProfitCurrency==0?1:op.ProfitCurrency)),"مبلغ یا ارز معتبر نیست.");
                Require(op.ReceivedAmount is null || op.Price.HasValue && op.ReceivedAmount>=0 && op.ReceivedAmount<=op.Price,"مبلغ دریافتی معتبر نیست.");
                var handpan=await InstrumentAsync(bowl!,ct);
                Require(handpan.Stage==ProductionStage.FinishedWarehouse || handpan.Stage==ProductionStage.ExportWarehouse && op.IsExportSale,"ساز باید در انبار مربوط باشد.");
                if(op.LeadSourceId.HasValue) {
                    var source=await db.SaleLeadSources.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==op.LeadSourceId && x.IsActive,ct);
                    Require(source is not null,"روش آشنایی معتبر نیست.");

                    Require(source!.RequiresReferrer || !op.ReferrerId.HasValue,"معرف فقط برای روش آشنایی معرف قابل انتخاب است.");
                }
                Require(op.LeadSourceId.HasValue || !op.ReferrerId.HasValue,"روش آشنایی معرف را انتخاب کنید.");
                if(op.ReferrerId.HasValue) Require(await db.SalesReferrers.AnyAsync(x=>x.Id==op.ReferrerId && x.IsActive,ct),"معرف معتبر نیست.");
                var party=op.PartyId.HasValue?await db.AccountingParties.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==op.PartyId && x.IsActive,ct):null;
                Require(!op.PartyId.HasValue || party is not null,"طرف حساب معتبر نیست.");
                // Existing sale handler uses detached reads; clear before it attaches the instrument.
                var handpanId=handpan.Id;db.ChangeTracker.Clear();
                await mediator.Send(new SellHandpanCommand(handpanId,party?.Name??op.BuyerName,party?.Phone??op.BuyerPhoneNumber,op.Price,op.Destination,op.IsExportSale),ct);
                handpan=await db.Handpans.SingleAsync(x=>x.Id==handpanId,ct);
                handpan.SetSaleAttribution(op.LeadSourceId,op.ReferrerId);
                db.Entry(handpan).Property(x=>x.SoldAt).CurrentValue=op.OccurredUtc;
                db.Entry(handpan).Property(x=>x.WarrantyActivatedAt).CurrentValue=op.OccurredUtc;
                db.Entry(handpan).Property(x=>x.UpdatedAt).CurrentValue=op.OccurredUtc;
                await new TORSEPAN.API.Services.SaleProfitService(db).UpdateAsync(handpan,op.NetProfit,(MoneyCurrency)(op.ProfitCurrency==0?1:op.ProfitCurrency));
                handpan.SetSaleFinancials((MoneyCurrency)(op.PriceCurrency==0?1:op.PriceCurrency),op.NetProfit,(MoneyCurrency)(op.ProfitCurrency==0?1:op.ProfitCurrency));
                if(op.Price.HasValue) {
                    var document=new AccountingDocument(AccountingDocumentType.Revenue,$"فروش ساز {handpan.SerialNumber}",op.Price.Value,
                        op.ReceivedAmount??0,party?.Id,handpan.Id,null,op.Destination,op.PerformerId);
                    document.SetCurrency(handpan.SaleCurrency);
                    db.AccountingDocuments.Add(document);db.Entry(document).Property(x=>x.CreatedAt).CurrentValue=op.OccurredUtc;
                    if(document.SettledAt.HasValue)db.Entry(document).Property(x=>x.SettledAt).CurrentValue=op.OccurredUtc;
                }
                // Handpan sale is authoritative; component bowls remain completed warehouse records online.
                break;
            case "ship":
                Require(bowl!.InstrumentCode is null && bowl.Stage==22,"کاسه صادراتی باید در انبار باشد.");
                if(op.PartyId.HasValue) Require(await db.AccountingParties.AnyAsync(x=>x.Id==op.PartyId && x.IsActive,ct),"طرف حساب معتبر نیست.");
                Require((op.BuyerName?.Length??0)<=200 && (op.Destination?.Length??0)<=200 && (op.ShippingMethod?.Length??0)<=200,"اطلاعات ارسال طولانی است.");
                await mediator.Send(new ShipExportBowlCommand(bowl.Id,op.BuyerName,op.PartyId,op.Destination,op.ShippingMethod,op.IsSettled),ct);
                break;
            case "note":
                Require(!string.IsNullOrWhiteSpace(op.Details),"متن یادداشت لازم است.");
                if(op.IsInstrumentNote) Require(bowl!.InstrumentCode is not null && bowl.Stage>=12,"یادداشت ساز پس از خروج از چسب فعال است.");
                var target=bowl!;
                if(op.IsInstrumentNote && target.Type!=1)target=await BowlAsync(target.PairCode!,ct);
                // Stable operation IDs provide deduplication, without the online one-minute heuristic.
                db.ProductionEvents.Add(new ProductionEvent(null,null,target.Id,op.PerformerId,ProductionAction.Shape,EventResult.Completed,null,
                    op.IsInstrumentNote?$"NOTE:INSTRUMENT:{bowl!.Stage}:{op.Details!.Trim()}":"NOTE:"+op.Details!.Trim()));
                break;
            case "design":
                Require(op.DesignTypeId.HasValue,"دیزاین را انتخاب کنید.");
                var response=await new DesignsController(db) { ControllerContext=new ControllerContext { HttpContext=http.HttpContext! } }
                    .Register(new RegisterDesignRequest(op.Code,[new RegisterDesignItem(op.DesignTypeId!.Value,op.PerformerId)]),ct);
                Require(response is OkObjectResult,"دیزاین معتبر نیست، تکراری است یا ساز در انبار قفل شده است.");
                break;
            case "warehouseTransfer":
                Require(op.ExportLocation.HasValue ? Enum.IsDefined((ExportWarehouseLocation)op.ExportLocation.Value) : op.QualityWarehouseId.HasValue && InstrumentWarehouse.IsValidId(op.QualityWarehouseId.Value),"انبار مقصد معتبر نیست.");
                var stockInstrument=await InstrumentAsync(bowl!,ct);
                Require(!stockInstrument.SoldAt.HasValue && stockInstrument.Stage is ProductionStage.FinishedWarehouse or ProductionStage.ExportWarehouse,"فقط سازهای موجود در انبار قابل انتقال‌اند.");
                var warehouseNames=await db.InstrumentWarehouses.AsNoTracking().ToDictionaryAsync(x=>x.Id,x=>x.Name,ct);
                var sourceName=stockInstrument.Stage==ProductionStage.ExportWarehouse?$"انبار صادراتی — {stockInstrument.ExportWarehouseLocation}":warehouseNames[stockInstrument.QualityWarehouseId];
                string targetName;
                if(op.ExportLocation is int location){stockInstrument.MoveToExportWarehouse((ExportWarehouseLocation)location);targetName=$"انبار صادراتی — {(ExportWarehouseLocation)location}";}
                else {stockInstrument.SetQualityWarehouse(op.QualityWarehouseId!.Value);if(stockInstrument.Stage==ProductionStage.ExportWarehouse){stockInstrument.ChangeStage(ProductionStage.FinishedWarehouse);stockInstrument.SetPackagingDestination(null);}targetName=warehouseNames[stockInstrument.QualityWarehouseId];}
                db.ProductionEvents.Add(new ProductionEvent(stockInstrument.Id,stockInstrument.AssemblyId,null,op.PerformerId,ProductionAction.WarehouseTransfer,EventResult.Completed,null,$"انتقال انبار از {sourceName} به {targetName}"));
                break;
            case "workspace":await ApplyWorkspaceAsync(op,ct);break;
            case "edit":
                Require(op.Edit is not null,"اطلاعات اصلاحات لازم است.");
                var edit=op.Edit!;
                if(op.EditScales is not null) {
                    Require(op.EditScales.Keys.All(x=>x==bowl!.Code || x==bowl!.PairCode),"کاسه به ساز تعلق ندارد.");
                    var updates=new List<AdminBowlScaleUpdate>();
                    foreach(var pair in op.EditScales)updates.Add(new AdminBowlScaleUpdate((await BowlAsync(pair.Key,ct)).Id,pair.Value));
                    edit=edit with { BowlScales=updates };
                }
                if(op.LocalEventUsers is {Count:>0}){
                    Require(op.LocalEventUsers.Count<=100&&op.LocalEventUsers.Select(x=>x.OperationId).Distinct().Count()==op.LocalEventUsers.Count,"اصلاحات تکراری هستند.");var users=new List<AdminEventUserUpdate>(edit.EventUsers??[]);
                    foreach(var update in op.LocalEventUsers){var r=await db.OfflineReceipts.AsNoTracking().SingleOrDefaultAsync(x=>x.OperationId==update.OperationId&&x.DeviceId==op.DeviceId,ct);Require(r is not null,"مرحله محلی هنوز تأیید نشده است.");var source=JsonSerializer.Deserialize<OfflineOperation>(r!.PayloadJson)!;Require(source.Code==bowl!.Code||source.Code==bowl.PairCode||source.PairCode==bowl.Code,"مرحله به این ساز تعلق ندارد.");var ids=JsonSerializer.Deserialize<Dictionary<string,string>>(r.VersionsJson)!.Where(x=>x.Key.StartsWith("performerEvent:")).Select(x=>Guid.Parse(x.Value)).ToArray();Require(ids.Length>0,"دریافت اطلاعات تازه لازم است.");Require(await db.Users.AnyAsync(x=>x.Id==update.UserId&&x.IsActive&&!x.IsDeleted,ct),"همکار فعال نیست.");users.AddRange(ids.Select(id=>new AdminEventUserUpdate(id,update.UserId)));}edit=edit with {EventUsers=users};
                }
                var result=await new AdminProductionEditsController(db).Update(op.Code,edit,ct);
                if(result.Result is ObjectResult error && error.StatusCode>=400) throw new OfflineProblem("edit",error.Value?.ToString()??"اصلاح اطلاعات معتبر نیست.",422);
                break;
            case "photoDelete":
                var photoInstrument=await InstrumentAsync(bowl!,ct);
                var oldPhoto=await db.HandpanPhotos.SingleOrDefaultAsync(x=>x.Id==op.PhotoId&&x.HandpanId==photoInstrument.Id,ct);
                Require(oldPhoto is not null,"عکس متعلق به این ساز پیدا نشد.");
                db.HandpanPhotos.Remove(oldPhoto!);db.Entry(photoInstrument).Property(x=>x.OfflineVersion).IsModified=true;
                break;
            case "photo":
                var instrument=await InstrumentAsync(bowl!,ct);
                Require(instrument.Stage>=ProductionStage.WaitingForFinalTune,"عکس پس از خروج از اتاق چسب فعال است.");
                byte[] image,thumbnail;
                try { image=Convert.FromBase64String(op.Image??"");thumbnail=Convert.FromBase64String(op.Thumbnail??""); }
                catch(FormatException){throw new OfflineProblem("photo","فایل عکس معتبر نیست.",400);}
                Require(image.Length is >0 and <=5_000_000 && thumbnail.Length is >0 and <=500_000,"حجم عکس معتبر نیست.");
                Require(HandpanPhotosController.IsImage(image,image.Length,"image/jpeg") && HandpanPhotosController.IsImage(thumbnail,thumbnail.Length,"image/jpeg"),"عکس باید JPEG معتبر باشد.");
                Require(Convert.ToHexString(SHA256.HashData(image)).Equals(op.ImageHash,StringComparison.OrdinalIgnoreCase),"فایل عکس با اثر انگشت ذخیره‌شده مطابقت ندارد.");
                var photo=new HandpanPhoto(instrument.Id,image,thumbnail,"image/jpeg",op.PerformerId);
                db.Entry(photo).Property(x=>x.CreatedAt).CurrentValue=op.OccurredUtc;
                db.HandpanPhotos.Add(photo);if(op.PhotoId is {} localPhoto&&localPhoto!=op.OperationId)workspaceAliases[localPhoto]=photo.Id;db.Entry(instrument).Property(x=>x.OfflineVersion).IsModified=true;
                break;
            default: throw new OfflineProblem("unsupported","عملیات شناخته نشد.",400);
        }
        return Result<BowlDimpleDto>.Success(null!);
    }
}
