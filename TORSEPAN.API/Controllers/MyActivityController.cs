using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;
[ApiController,Route("api/me/activity"),Authorize]
public sealed class MyActivityController(TORSEPANDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,
        [FromQuery]int page=1,CancellationToken ct=default)
    {
        var today=DateTime.UtcNow.AddHours(3.5).Date;
        var calendar=new PersianCalendar();
        var start=(from??calendar.ToDateTime(calendar.GetYear(today),calendar.GetMonth(today),1,0,0,0,0)).Date;
        var end=(to??today).Date;
        if(start.Year<1900||start>end||end-start>TimeSpan.FromDays(366)||page<1||page>100000||end.Year>=9999)
            return BadRequest("بازه معتبر تا حداکثر یک سال انتخاب کنید.");
        var startUtc=DateTime.SpecifyKind(start.AddHours(-3.5),DateTimeKind.Utc);
        var endUtc=DateTime.SpecifyKind(end.AddDays(1).AddHours(-3.5),DateTimeKind.Utc);
        var userId=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        // The client cannot choose a user ID. Filtering happens before paging or aggregation.
        var query=db.ProductionEvents.AsNoTracking().Where(x=>x.UserId==userId&&x.EventDate>=startUtc&&x.EventDate<endUtc
            &&!x.Description.StartsWith("NOTE:")&&x.Description!="Released from glue room");
        var total=await query.CountAsync(ct);
        var completed=await query.CountAsync(x=>x.Result==EventResult.Completed,ct);
        var summaryRows=await query.Where(x=>x.Result==EventResult.Completed).GroupBy(x=>new{
                x.Action,
                BowlType=x.Bowl!=null?(BowlType?)x.Bowl.BowlType:null,
                IsExport=EF.Functions.ILike(x.Description,"%export%")
            }).Select(x=>new{x.Key.Action,x.Key.BowlType,x.Key.IsExport,Count=x.Count()})
            .OrderByDescending(x=>x.Count).ToListAsync(ct);
        var rows=await query.OrderByDescending(x=>x.EventDate).ThenBy(x=>x.Id)
            .Skip((page-1)*50).Take(50).Select(x=>new{
                x.Id,x.EventDate,x.Action,x.Result,x.Duration,x.Description,
                Code=x.Bowl!=null?x.Bowl.ProductionCode:x.Handpan!=null?x.Handpan.SerialNumber:
                    x.Assembly!=null?x.Assembly.TopBowl.ProductionCode+" / "+x.Assembly.BottomBowl.ProductionCode:"",
                BowlType=x.Bowl!=null?(BowlType?)x.Bowl.BowlType:null,
                IsExport=EF.Functions.ILike(x.Description,"%export%")
            }).ToListAsync(ct);
        return Ok(new{From=start,To=end,Total=total,Completed=completed,Page=page,PageSize=50,
            Summary=summaryRows.Select(x=>new{Operation=OperationLabel(x.Action,x.BowlType,x.IsExport),x.Count}),
            Items=rows.Select(x=>new{
                x.Id,x.EventDate,x.Code,
                Operation=OperationLabel(x.Action,x.BowlType,x.IsExport),
                Result=Result(x.Result),
                Duration=x.Duration.HasValue?(x.Duration==OperationDuration.Over60?"بیش از ۶۰ دقیقه":$"{(int)x.Duration.Value*5} دقیقه"):"—",
                Details=Details(x.Description)
            })});
    }

    [HttpGet("payroll")]
    public async Task<IActionResult> Payroll([FromQuery]DateTime? from=null,[FromQuery]DateTime? to=null,CancellationToken ct=default)
    {
        var userId=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var enabled=await db.Users.AsNoTracking().Where(x=>x.Id==userId).Select(x=>x.ShowMyPayroll).FirstOrDefaultAsync(ct);
        if(!enabled)return Ok(new{Enabled=false,Total=0m,Lines=Array.Empty<object>()});
        var today=DateTime.UtcNow.AddHours(3.5).Date;var calendar=new PersianCalendar();
        var start=(from??calendar.ToDateTime(calendar.GetYear(today),calendar.GetMonth(today),1,0,0,0,0)).Date;var end=(to??today).Date;
        if(start.Year<1900||start>end||end-start>TimeSpan.FromDays(366))return BadRequest("بازه معتبر تا حداکثر یک سال انتخاب کنید.");
        var startUtc=DateTime.SpecifyKind(start.AddHours(-3.5),DateTimeKind.Utc);var endUtc=DateTime.SpecifyKind(end.AddDays(1).AddHours(-3.5),DateTimeKind.Utc);
        var paidIds=(await db.PayrollPayments.AsNoTracking().Select(x=>x.HandpanIdsJson).ToListAsync(ct)).SelectMany(ParsePaidIds).ToHashSet();
        var eligibleHandpanIds=(await db.ProductionEvents.AsNoTracking()
            .Where(x=>x.HandpanId.HasValue&&x.Action==ProductionAction.Packaging&&x.Result==EventResult.Completed&&x.EventDate>=startUtc&&x.EventDate<endUtc)
            .Select(x=>x.HandpanId!.Value).Distinct().ToListAsync(ct)).Where(x=>!paidIds.Contains(x)).ToHashSet();
        var eligibleHandpans=await db.Handpans.AsNoTracking().Include(x=>x.Assembly).Where(x=>eligibleHandpanIds.Contains(x.Id)).ToListAsync(ct);
        var eligibleAssemblyIds=eligibleHandpans.Select(x=>x.AssemblyId).ToHashSet();
        var eligibleBowlIds=eligibleHandpans.SelectMany(x=>new[]{x.Assembly.TopBowlId,x.Assembly.BottomBowlId}).ToHashSet();
        var events=await db.ProductionEvents.AsNoTracking().Include(x=>x.Bowl)!.ThenInclude(x=>x.Material).Include(x=>x.Bowl)!.ThenInclude(x=>x.Scale)
            .Include(x=>x.Assembly)!.ThenInclude(x=>x.TopBowl).ThenInclude(x=>x.Material).Include(x=>x.Handpan)!.ThenInclude(x=>x.Scale)
            .Include(x=>x.Handpan)!.ThenInclude(x=>x.Assembly).ThenInclude(x=>x.TopBowl).ThenInclude(x=>x.Material)
            .Where(x=>x.UserId==userId&&x.Result==EventResult.Completed&&!x.IsPayrollExcluded&&!x.Description.StartsWith("NOTE:")&&x.Description!="Released from glue room"&&
                ((x.HandpanId.HasValue&&eligibleHandpanIds.Contains(x.HandpanId.Value))||(x.AssemblyId.HasValue&&eligibleAssemblyIds.Contains(x.AssemblyId.Value))||(x.BowlId.HasValue&&eligibleBowlIds.Contains(x.BowlId.Value)))&&
                (x.Action==ProductionAction.Dimple||x.Action==ProductionAction.Shape||x.Action==ProductionAction.Glue||x.Action==ProductionAction.Tune||x.Action==ProductionAction.FineTune||x.Action==ProductionAction.Design)).ToListAsync(ct);
        var rates=await db.PayrollRates.AsNoTracking().ToListAsync(ct);var designs=await db.DesignTypes.AsNoTracking().ToDictionaryAsync(x=>x.Id,ct);
        var lines=events.GroupBy(x=>new{x.Action,IsExport=x.Description.Contains("export",StringComparison.OrdinalIgnoreCase),
            IsCustom=x.Handpan!=null?x.Handpan.Assembly.TopBowl.IsCustomScale:x.Assembly!=null?x.Assembly.TopBowl.IsCustomScale:x.Bowl!=null&&x.Bowl.IsCustomScale,
            MaterialId=x.Action is ProductionAction.Glue or ProductionAction.Design?(Guid?)null:x.Bowl!=null?x.Bowl.MaterialId:x.Assembly!=null?x.Assembly.TopBowl.MaterialId:x.Handpan!=null?x.Handpan.Assembly.TopBowl.MaterialId:null,
            Material=x.Action is ProductionAction.Glue or ProductionAction.Design?"":x.Bowl!=null?x.Bowl.Material.Name:x.Assembly!=null?x.Assembly.TopBowl.Material.Name:x.Handpan!=null?x.Handpan.Assembly.TopBowl.Material.Name:"—",
            BowlType=x.Action is ProductionAction.Glue or ProductionAction.Design||x.Bowl==null?(int?)null:(int)x.Bowl.BowlType,
            DesignId=x.Action==ProductionAction.Design?ParseDesignTypeId(x.Description):null,
            ScaleId=x.Action==ProductionAction.FineTune&&x.Handpan!=null?x.Handpan.ScaleId:(x.Action==ProductionAction.Dimple||x.Action==ProductionAction.Shape||x.Action==ProductionAction.Tune)&&x.Bowl!=null?x.Bowl.ScaleId:null,
            Scale=x.Action==ProductionAction.FineTune&&x.Handpan?.Scale!=null?x.Handpan.Scale.Name:(x.Action==ProductionAction.Dimple||x.Action==ProductionAction.Shape||x.Action==ProductionAction.Tune)&&x.Bowl?.Scale!=null?x.Bowl.Scale.Name:""})
            .Select(g=>{var rate=g.Key.Action==ProductionAction.Design&&g.Key.DesignId.HasValue&&designs.TryGetValue(g.Key.DesignId.Value,out var d)?(g.Key.IsExport?d.ExportRate:d.Rate):rates.Where(r=>r.Action==g.Key.Action&&r.IsExport==g.Key.IsExport&&r.IsCustom==g.Key.IsCustom&&(!r.MaterialId.HasValue||r.MaterialId==g.Key.MaterialId)&&(!r.BowlType.HasValue||(int)r.BowlType==g.Key.BowlType)&&(!r.ScaleId.HasValue||r.ScaleId==g.Key.ScaleId)).OrderByDescending(r=>r.MaterialId.HasValue).ThenByDescending(r=>r.BowlType.HasValue).ThenByDescending(r=>r.ScaleId.HasValue).FirstOrDefault()?.Amount??0;var count=g.Key.Action==ProductionAction.Glue?g.Where(x=>x.HandpanId.HasValue).Select(x=>x.HandpanId).Distinct().Count():g.Count();return new{Operation=OperationLabel(g.Key.Action,g.Key.BowlType.HasValue?(BowlType?)g.Key.BowlType.Value:null,g.Key.IsExport),Description=string.Join(" — ",new[]{g.Key.Material,g.Key.Scale,g.Key.IsCustom?"Custom":""}.Where(x=>!string.IsNullOrWhiteSpace(x))),Count=count,Rate=rate,Total=count*rate};}).OrderBy(x=>x.Operation).ToList();
        return Ok(new{Enabled=true,From=start,To=end,Total=lines.Sum(x=>x.Total),Lines=lines});
    }

    private static Guid? ParseDesignTypeId(string? value){if(string.IsNullOrWhiteSpace(value)||!value.StartsWith("DESIGN:"))return null;var parts=value.Split(':');return parts.Length>1&&Guid.TryParse(parts[1],out var id)?id:null;}
    private static List<Guid> ParsePaidIds(string json){try{return JsonSerializer.Deserialize<List<Guid>>(json)??[];}catch{return[];}}
    private static string OperationLabel(ProductionAction action,BowlType? bowlType,bool isExport)
    {
        var suffix=bowlType.HasValue?(bowlType==BowlType.Top?" کاسه رو":" کاسه زیر"):"";
        return action==ProductionAction.Packaging
            ? (isExport?"بسته‌بندی صادراتی":"بسته‌بندی عادی")+suffix
            : Operation(action)+suffix+(isExport?" صادراتی":"");
    }
    private static string Operation(ProductionAction a)=>a switch{
        ProductionAction.Created=>"ثبت اولیه",ProductionAction.Dimple=>"دیمپل",ProductionAction.Shape=>"شیپ",
        ProductionAction.Furnace=>"پخت",ProductionAction.Glue=>"چسب",ProductionAction.Tune=>"تیون",
        ProductionAction.FineTune=>"فاین‌تیون",ProductionAction.QualityCheck=>"کنترل کیفیت",
        ProductionAction.Packaging=>"بسته‌بندی",ProductionAction.Design=>"دیزاین",ProductionAction.Sale=>"فروش",
        ProductionAction.WarehouseEntry=>"ورود به انبار",ProductionAction.Reject=>"برگشتی",_=>a.ToString()};
    private static string Result(EventResult r)=>r switch{
        EventResult.Completed=>"تکمیل‌شده",EventResult.Failed=>"ناموفق",EventResult.Rejected=>"ردشده",EventResult.Skipped=>"عبور از مرحله",_=>r.ToString()};
    private static string Details(string text)
    {
        if(text.StartsWith("DESIGN:")){var parts=text.Split(':',3);return parts.Length==3?"دیزاین: "+parts[2]:"دیزاین ثبت شد";}
        if(text.StartsWith("PACKAGING_ITEMS:"))return "اقلام بسته‌بندی: "+text[16..].Replace("|","، ");
        if(text.StartsWith("Glued with bowl "))return "جفت‌شده با کاسه "+text["Glued with bowl ".Length..];
        if(text.StartsWith("Shape completed"))return "شیپ تکمیل شد";
        return text switch{
            "Dimple completed"=>"دیمپل تکمیل شد","Tune completed"=>"تیون تکمیل شد",
            "Bake completed"=>"پخت تکمیل شد","Final tune completed"=>"فاین‌تیون تکمیل شد",
            "Export packaging completed"=>"بسته‌بندی صادراتی تکمیل شد",_=>text};
    }
}
