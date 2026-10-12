using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.Orders;
using TORSEPAN.Application.Auth.Commands.CreateUser;
using TORSEPAN.Application.Auth.Commands.UpdateUser;
using TORSEPAN.Application.Auth.Commands.UpdateUserStatus;
using TORSEPAN.Application.Auth.Commands.DeleteUser;
using TORSEPAN.Application.Materials.Commands.CreateMaterial;
using TORSEPAN.Application.Materials.Commands.UpdateMaterial;
using TORSEPAN.Application.Materials.Commands.DeleteMaterial;
using TORSEPAN.Application.Scales;
using TORSEPAN.Infrastructure.Services;
namespace TORSEPAN.API.Offline;

public sealed partial class OfflineProductionService {
 private readonly Dictionary<Guid,Guid> workspaceAliases=new();
 private async Task<OfflineOperation> ResolveWorkspaceIdsAsync(OfflineOperation op,CancellationToken ct){
  workspaceAliases.Clear();
  var receipts=await db.OfflineReceipts.AsNoTracking().Where(x=>x.DeviceId==op.DeviceId).Select(x=>x.VersionsJson).ToListAsync(ct);
  var aliases=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  foreach(var json in receipts)foreach(var p in JsonSerializer.Deserialize<Dictionary<string,string>>(json)!)if(p.Key.StartsWith("alias:"))aliases[p.Key[6..]]=p.Value;
  string Actual(string text){var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);while(aliases.TryGetValue(text,out var next)&&seen.Add(text))text=next;return text;}
  JsonNode? Replace(JsonNode? node){if(node is JsonValue value&&value.TryGetValue<string>(out var text)&&aliases.TryGetValue(text,out var actual))return JsonValue.Create(Actual(actual));if(node is JsonObject obj)foreach(var key in obj.Select(x=>x.Key).ToArray()){var v=Replace(obj[key]?.DeepClone());obj.Remove(key);obj[Actual(key)]=v;}if(node is JsonArray array)for(int i=0;i<array.Count;i++)array[i]=Replace(array[i]?.DeepClone());return node;}
  return Replace(JsonSerializer.SerializeToNode(op))!.Deserialize<OfflineOperation>()!;
 }
 private async Task CheckWorkspaceVersionAsync(Guid owner,OfflineOperation op,CancellationToken ct){
  var version=op.WorkspaceExpectedVersion;
  if(op.WorkspacePreviousOperationId is {} previous){var receipt=await db.OfflineReceipts.AsNoTracking().SingleOrDefaultAsync(x=>x.OperationId==previous&&x.DeviceId==op.DeviceId,ct);if(receipt is null)throw new OfflineProblem("dependency","ثبت قبلی هنوز تأیید نشده است.");version=JsonSerializer.Deserialize<Dictionary<string,string>>(receipt.VersionsJson)?.GetValueOrDefault("workspace");}
  if(string.IsNullOrEmpty(version)||version!=(await WorkspaceAsync(owner,ct)).Version)throw new OfflineProblem("conflict","اطلاعات مدیریت در سرور تغییر کرده؛ ثبت محلی محفوظ است و نیاز به بررسی دارد.");
 }
 private T Controller<T>() where T:ControllerBase {var c=ActivatorUtilities.CreateInstance<T>(http.HttpContext!.RequestServices);c.ControllerContext=new ControllerContext{HttpContext=http.HttpContext!};return c;}
 private async Task ApplyWorkspaceAsync(OfflineOperation op,CancellationToken ct){
  if(op.WorkspaceData is not {} data)throw new OfflineProblem("invalid","اطلاعات ثبت کامل نیست.",400);
  T Read<T>()=>data.Deserialize<T>(WebJson)??throw new OfflineProblem("invalid","اطلاعات فرم معتبر نیست.",400);
  Guid Id()=>data.GetProperty("id").GetGuid();
  Guid? NullableId()=>data.TryGetProperty("id",out var v)&&v.ValueKind==JsonValueKind.String?v.GetGuid():null;
  void Alias(Guid actual){if(NullableId() is {} local&&local!=actual)workspaceAliases[local]=actual;}
  IActionResult? result=null;
  try {
   switch(op.WorkspaceModule,op.WorkspaceAction){
    case ("warehouses","save"):{var id=data.GetProperty("id").GetInt32();var warehouse=await db.InstrumentWarehouses.SingleOrDefaultAsync(x=>x.Id==id,ct);if(warehouse is null)throw new OfflineProblem("missing","انبار پیدا نشد.",404);
     var name=data.GetProperty("name").GetString()!;warehouse.Rename(name);if(await db.InstrumentWarehouses.AnyAsync(x=>x.Id!=id&&x.Name.ToUpper()==warehouse.Name.ToUpper(),ct))throw new OfflineProblem("duplicate","نام انبار تکراری است.");break;}
    case ("orders","save"):{var service=new CustomerOrderService(db,TimeProvider.System);var id=NullableId();if(id.HasValue&&await db.CustomerOrders.AnyAsync(x=>x.Id==id,ct)){var order=await db.CustomerOrders.AsNoTracking().SingleAsync(x=>x.Id==id,ct);var r=Read<SaveOrderDraftRequest>();if(order.IsDraft)await service.SaveDraftAsync(id,r,op.PerformerId,ct);else await service.UpdateOrderAsync(id.Value,r,ct);}else {var actual=await service.SaveDraftAsync(null,Read<SaveOrderDraftRequest>(),op.PerformerId,ct);Alias(actual);id=actual;}
     var stored=await db.CustomerOrders.AsNoTracking().Include(x=>x.Lines).SingleAsync(x=>x.Id==id,ct);var inputs=Read<SaveOrderDraftRequest>().Lines;for(int i=0;i<inputs.Count;i++)if(inputs[i].LineId is {} local){var line=stored.Lines.Single(x=>x.Position==i+1);if(local!=line.Id)workspaceAliases[local]=line.Id;}break;}
    case ("orders","finalize"):await new CustomerOrderService(db,TimeProvider.System).FinalizeAsync(Id(),data.GetProperty("version").GetInt32(),ct);break;
    case ("orders","assign"):await new CustomerOrderService(db,TimeProvider.System).AssignLineCodeAsync(Id(),data.GetProperty("lineId").GetGuid(),data.GetProperty("slot").GetInt32(),data.GetProperty("code").GetString()!,op.PerformerId,ct,data.TryGetProperty("expectedCode",out var ec)?ec.GetString()??"":"");break;
    case ("orders","delete"):await new CustomerOrderService(db,TimeProvider.System).DeleteAsync(Id(),ct);break;
    case ("parties","save"):{var c=new AccountingController(db){ControllerContext=new ControllerContext{HttpContext=http.HttpContext!}};if(NullableId() is {} id&&await db.AccountingParties.AnyAsync(x=>x.Id==id,ct))result=await c.UpdateParty(id,Read<PartyRequest>(),ct);else{result=await c.AddParty(Read<PartyRequest>(),ct);Alias(ResultId(result));}break;}
    case ("parties","delete"):result=await Controller<AccountingController>().DeleteParty(Id(),ct);break;
    case ("documents","save"):result=await Controller<AccountingController>().AddDocument(Read<DocumentRequest>(),ct);Alias(ResultId(result));break;
    case ("documents","payment"):result=await Controller<AccountingController>().AddPayment(Id(),Read<PaymentRequest>(),ct);break;
    case ("users","save"):{if(NullableId() is {} id&&await db.Users.AnyAsync(x=>x.Id==id,ct))await mediator.Send(Read<UpdateUserCommand>() with {UserId=id},ct);else Alias(await mediator.Send(Read<CreateUserCommand>(),ct));break;}
    case ("users","status"):if(Id()==op.PerformerId)throw new OfflineProblem("owner","حساب ثبت‌کننده را در حالت آفلاین غیرفعال نکنید.",400);await mediator.Send(new UpdateUserStatusCommand(Id(),data.GetProperty("isActive").GetBoolean()),ct);break;
    case ("users","delete"):await mediator.Send(new DeleteUserCommand(Id(),op.PerformerId),ct);break;
    case ("scales","save"):{if(NullableId() is {} id&&await db.Scales.AnyAsync(x=>x.Id==id,ct))await mediator.Send(new RenameScaleCommand(id,data.GetProperty("name").GetString()!),ct);else Alias(await mediator.Send(Read<CreateScaleCommand>(),ct));break;}
    case ("scales","delete"):await mediator.Send(new DeactivateScaleCommand(Id(),data.GetProperty("usage").GetInt32()),ct);break;
    case ("materials","save"):{if(NullableId() is {} id&&await db.Materials.AnyAsync(x=>x.Id==id,ct))await mediator.Send(new UpdateMaterialCommand(id,data.GetProperty("name").GetString()!),ct);else Alias(await mediator.Send(Read<CreateMaterialCommand>(),ct));break;}
    case ("materials","delete"):await mediator.Send(new DeleteMaterialCommand(Id()),ct);break;
    case ("designTypes","save"):{var c=Controller<DesignsController>();if(NullableId() is {} id&&await db.DesignTypes.AnyAsync(x=>x.Id==id,ct))result=await c.UpdateType(id,Read<DesignTypeRequest>(),ct);else{result=await c.AddType(Read<DesignTypeRequest>(),ct);Alias(ResultId(result));}break;}
    case ("designTypes","delete"):result=await Controller<DesignsController>().DeleteType(Id(),ct);break;
    case ("sources","save"):result=await Controller<SalesAttributionController>().AddSource(Read<NameRequest>(),ct);Alias(ResultId(result));break;
    case ("referrers","save"):result=await Controller<SalesAttributionController>().AddReferrer(Read<NameRequest>(),ct);Alias(ResultId(result));break;
    case ("marketing","save"):{var r=Read<MarketingLeadRequest>();if(r.Id.HasValue&&!await db.MarketingLeads.AnyAsync(x=>x.Id==r.Id,ct))r=r with {Id=null};result=await Controller<MarketingController>().Save(r,ct);Alias(ResultId(result));break;}
    case ("marketing","activity"):result=await Controller<MarketingController>().AddActivity(Id(),Read<MarketingActivityRequest>(),ct);break;
    case ("rates","save"):{var r=Read<PayrollRateRequest>();if(r.Id.HasValue&&!await db.PayrollRates.AnyAsync(x=>x.Id==r.Id,ct))r=r with {Id=null};result=await Controller<PayrollController>().SaveRate(r,ct);var rate=await db.PayrollRates.AsNoTracking().FirstOrDefaultAsync(x=>x.Action==(TORSEPAN.Domain.Enums.ProductionAction)r.Action&&x.IsExport==r.IsExport&&x.IsCustom==r.IsCustom&&x.MaterialId==r.MaterialId&&x.BowlType==(TORSEPAN.Domain.Enums.BowlType?)r.BowlType&&x.ScaleId==r.ScaleId,ct);if(rate is not null)Alias(rate.Id);break;}
    case ("rates","delete"):result=await Controller<PayrollController>().DeleteRate(Id(),ct);break;
    case ("payments","save"):{var c=Controller<PayrollController>();var r=Read<PayrollPaymentRequest>();var preview=await c.Get(r.From,r.To,r.ReadyForQc,r.ReadyForPackaging,r.EnteredWarehouse,r.ReadyForExportPackaging,r.ExportWarehouse,ct);if(preview is OkObjectResult ok&&data.TryGetProperty("totalAmount",out var expected)){var calc=JsonSerializer.SerializeToElement(ok.Value,WebJson);var total=calc.GetProperty("lines").EnumerateArray().Sum(x=>x.GetProperty("total").GetDecimal());if(total!=expected.GetDecimal())throw new OfflineProblem("payroll","مبلغ محلی با محاسبه سرور متفاوت است؛ ثبت محفوظ ماند و باید بررسی شود.");}result=await c.MarkPaid(r,ct);Alias(ResultId(result));break;}
    case ("payments","accounting"):result=await Controller<PayrollController>().SendPaymentToAccounting(Id(),ct);break;
    case ("events","exclude"):result=await Controller<PayrollExclusionsController>().Save(Read<PayrollExclusionSave>(),ct);break;
    case ("messages","save"):result=await Controller<NotificationsController>().Send(Read<SendWorkshopMessage>(),ct);break;
    case ("messages","read"):result=await Controller<NotificationsController>().MarkRead(Id(),ct);break;
    default:throw new OfflineProblem("unsupported","عملیات مدیریت پشتیبانی نمی‌شود.",400);
   }
   if(result is ObjectResult obj&&obj.StatusCode>=400)throw new OfflineProblem("validation",JsonSerializer.Serialize(obj.Value,WebJson),obj.StatusCode.Value);
   if(result is StatusCodeResult status&&status.StatusCode>=400)throw new OfflineProblem("validation","ثبت معتبر نیست یا رکورد پیدا نشد.",status.StatusCode);
  }catch(Exception ex)when(ex is OrderValidationException or ArgumentException or KeyNotFoundException or InvalidOperationException or JsonException or FormatException){throw new OfflineProblem("validation",ex.Message,422);}
 }
 private static Guid ResultId(IActionResult result){if(result is ObjectResult obj&&!(obj.StatusCode>=400)){var data=JsonSerializer.SerializeToElement(obj.Value,WebJson);if(data.ValueKind==JsonValueKind.String)return data.GetGuid();if(data.TryGetProperty("id",out var id))return id.GetGuid();}return Guid.Empty;}
}
