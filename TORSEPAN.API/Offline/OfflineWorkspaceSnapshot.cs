using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace TORSEPAN.API.Offline;
public sealed record OfflineWorkspace(string Version,Dictionary<string,JsonElement> Sections);
public sealed partial class OfflineProductionService {
 private static readonly JsonSerializerOptions WebJson=new(JsonSerializerDefaults.Web);
 private async Task<OfflineWorkspace> WorkspaceAsync(Guid owner,CancellationToken ct){
  var w=new Dictionary<string,JsonElement>();void Put(string k,object v)=>w[k]=JsonSerializer.SerializeToElement(v,WebJson);
  Put("warehouses",await db.InstrumentWarehouses.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,x.Version}).ToListAsync(ct));
  Put("users",await db.Users.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.UserName,x.FullName,x.Title,x.Role,x.IsActive,x.IsDeleted,x.DisplayOrder,x.ShowMyPayroll,RoleIds=x.UserRoles.OrderBy(r=>r.RoleId).Select(r=>r.RoleId).ToList(),Roles=x.UserRoles.OrderBy(r=>r.RoleId).Select(r=>r.Role.Name).ToList()}).ToListAsync(ct));
  Put("roles",await db.Roles.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,x.DisplayName}).ToListAsync(ct));
  Put("scales",await db.Scales.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,Usage=(int)x.Usage,x.IsActive,x.CanonicalScaleId,x.LegacyNoteCount}).ToListAsync(ct));
  Put("materials",await db.Materials.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,Category=(int)x.Category,x.Quantity,TopQuantity=x.TopBowlQuantity,BottomQuantity=x.BottomBowlQuantity,x.LowStockThreshold,x.TopBowlLowStockThreshold,x.BottomBowlLowStockThreshold,x.TopBowlCodeTemplate,x.BottomBowlCodeTemplate}).ToListAsync(ct));
  Put("designTypes",await db.DesignTypes.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,x.Rate,x.ExportRate,x.IsActive}).ToListAsync(ct));
  Put("orders",await db.CustomerOrders.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.CustomerName,x.DurationDays,x.CreatedAtUtc,x.DueAtUtc,x.IsDraft,x.Version,x.InstrumentCode,Lines=x.Lines.OrderBy(l=>l.Position).Select(l=>new{l.Id,l.Position,l.ScaleId,l.ScaleName,l.DesignTypeId,l.DesignName,l.Quantity,Instruments=l.Instruments.OrderBy(i=>i.Slot).Select(i=>new{i.Slot,i.Code}).ToList()}).ToList()}).ToListAsync(ct));
  Put("parties",await db.AccountingParties.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,x.Company,x.Phone,x.Notes,x.IsActive}).ToListAsync(ct));
  Put("documents",await db.AccountingDocuments.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,Type=(int)x.Type,x.Title,x.TotalAmount,x.PaidAmount,x.PartyId,x.HandpanId,x.PayrollPaymentId,x.DueDate,x.Notes,x.CreatedAt}).ToListAsync(ct));
  Put("marketing",await db.MarketingLeads.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,x.CompanyName,x.Country,x.City,x.Website,x.Email,x.Phone,x.SocialContact,x.ContactPerson,x.Priority,x.Status,x.CooperationScore,x.SamplePurchaseScore,x.CurrentProducts,x.Target,x.Notes,Activities=x.Activities.OrderBy(a=>a.Id).Select(a=>new{a.Id,a.Date,a.Method,a.SentText,a.Result,a.NextAction,a.RegisteredBy}).ToList()}).ToListAsync(ct));
  Put("sources",await db.SaleLeadSources.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,x.RequiresReferrer,x.IsActive}).ToListAsync(ct));
  Put("referrers",await db.SalesReferrers.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.Name,x.IsActive}).ToListAsync(ct));
  Put("rates",await db.PayrollRates.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,Action=(int)x.Action,x.MaterialId,BowlType=(int?)x.BowlType,x.ScaleId,x.IsExport,x.IsCustom,x.Amount}).ToListAsync(ct));
  Put("payments",await db.PayrollPayments.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.From,x.To,x.PaidAt,x.PaidBy,x.TotalAmount,x.HandpanIdsJson,x.HandpanCodesJson,x.LinesJson}).ToListAsync(ct));
  Put("events",await db.ProductionEvents.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.BowlId,x.AssemblyId,x.HandpanId,x.UserId,Action=(int)x.Action,Result=(int)x.Result,x.Duration,x.Description,x.EventDate,x.IsPayrollExcluded}).ToListAsync(ct));
  Put("bowls",await db.Bowls.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.ProductionCode,x.MaterialId,BowlType=(int)x.BowlType,x.ScaleId,x.LegacyScaleId,x.NoteCount,x.IsCustomScale,Stage=(int)x.Stage}).ToListAsync(ct));
  Put("assemblies",await db.HandpanAssemblies.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.TopBowlId,x.BottomBowlId}).ToListAsync(ct));
  Put("handpans",await db.Handpans.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.SerialNumber,x.AssemblyId,x.ScaleId,Stage=(int)x.Stage,x.QualityWarehouseId,x.ExportWarehouseLocation,x.SoldAt,x.BuyerName,x.BuyerPhoneNumber,x.SalePrice,x.SaleLeadSourceId,x.SalesReferrerId}).ToListAsync(ct));
  Put("messages",await db.WorkshopMessages.AsNoTracking().Where(x=>x.SenderId==owner||x.Broadcast||db.WorkshopMessageReceipts.Any(r=>r.MessageId==x.Id&&r.RecipientId==owner)).OrderBy(x=>x.Id).Select(x=>new{x.Id,x.SenderId,x.Title,x.Body,x.Broadcast,x.CreatedAt,ReadAt=db.WorkshopMessageReceipts.Where(r=>r.MessageId==x.Id&&r.RecipientId==owner).Select(r=>r.ReadAt).SingleOrDefault(),RecipientIds=x.SenderId==owner?db.WorkshopMessageReceipts.Where(r=>r.MessageId==x.Id).Select(r=>r.RecipientId).ToList():new List<Guid>{owner}}).ToListAsync(ct));
  return new(Hash(JsonSerializer.Serialize(w,WebJson)),w);
 }
}
