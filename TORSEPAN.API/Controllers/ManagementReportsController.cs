using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Application.Common.Reporting;
using TORSEPAN.Application.Materials;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;
using TORSEPAN.API.Reporting;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/management-reports"), Authorize(Roles="Administrator,ProductionManager")]
public sealed class ManagementReportsController(TORSEPANDbContext db, IHttpClientFactory httpFactory, IConfiguration configuration, IMediator mediator) : ControllerBase
{
    private static readonly string[] Palette=["#176B87","#27A17B","#F1B84B","#E56A54","#7B61A8","#38A3C7","#91B64B","#C85D8B"];

    [HttpGet("production")]
    public async Task<ActionResult<ProductionAnalytics>> Production([FromQuery] DateTime? from,[FromQuery] DateTime? to,
        [FromQuery] string? materialIds,[FromQuery] string? scaleIds,[FromQuery] string payroll="all",
        [FromQuery] string destination="all",CancellationToken ct=default)
        => Ok(await BuildProductionAsync(new(from,to,ParseIds(materialIds),ParseIds(scaleIds),payroll,destination,"production"),ct));

    [HttpGet("inventory")]
    public async Task<ActionResult<InventoryReport>> Inventory([FromQuery] DateTime? from,[FromQuery] DateTime? to,
        [FromQuery] string kind="instruments",[FromQuery] string? materialIds=null,[FromQuery] string? scaleIds=null,
        [FromQuery] string destination="all",CancellationToken ct=default)
        => Ok(await BuildInventoryAsync(new(from,to,ParseIds(materialIds),ParseIds(scaleIds),"all",destination,kind),ct));

    [HttpGet("{kind}/pdf")]
    public async Task<IActionResult> Pdf(string kind,[FromQuery] DateTime? from,[FromQuery] DateTime? to,
        [FromQuery] string? materialIds,[FromQuery] string? scaleIds,[FromQuery] string payroll="all",
        [FromQuery] string destination="all",[FromQuery] string inventoryKind="instruments",[FromQuery] Guid? userId=null,
        [FromQuery] int? action=null,CancellationToken ct=default)
    {
        var request=new ManagementReportRequest(from,to,ParseIds(materialIds),ParseIds(scaleIds),payroll,destination,inventoryKind,userId,action);
        var bytes=await BuildReportPdfAsync(kind,request,ct);
        return File(bytes,"application/pdf",$"torsepan-{kind}-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf");
    }

    [HttpPost("{kind}/telegram")]
    public async Task<IActionResult> Telegram(string kind,ManagementReportRequest request,CancellationToken ct)
    {
        var bytes=await BuildReportPdfAsync(kind,request,ct);var fileName=$"torsepan-{kind}-{DateTime.UtcNow:yyyyMMdd-HHmm}.pdf";
        var relay=configuration["Telegram:BackupRelayUrl"]??configuration["Telegram:RelayUrl"];
        if(string.IsNullOrWhiteSpace(relay))return Problem("Telegram relay is not configured.");
        relay=relay.Replace("telegram-database-backup","telegram-payroll-report").Replace("telegram-inventory-alert","telegram-payroll-report").Replace("/database-backup","/payroll-report").Replace("/inventory-alert","/payroll-report");
        using var form=new MultipartFormDataContent();form.Add(new ByteArrayContent(bytes),"report",fileName);
        using var message=new HttpRequestMessage(HttpMethod.Post,relay){Content=form};message.Headers.Add("X-Relay-Secret",configuration["Telegram:RelaySecret"]);
        var response=await httpFactory.CreateClient().SendAsync(message,ct);return response.IsSuccessStatusCode?Ok():StatusCode((int)response.StatusCode);
    }

    private async Task<ProductionAnalytics> BuildProductionAsync(ManagementReportRequest request,CancellationToken ct)
    {
        var (start,end)=Range(request.From,request.To);
        var allEvents=await db.ProductionEvents.AsNoTracking().Include(x=>x.User).Where(x=>x.Result==EventResult.Completed).ToListAsync(ct);
        var allBowls=await db.Bowls.AsNoTracking().Include(x=>x.Material).Include(x=>x.Scale).ToListAsync(ct);
        var bowlsById=allBowls.ToDictionary(x=>x.Id);
        var assemblies=await db.HandpanAssemblies.AsNoTracking().ToListAsync(ct);
        var assembliesById=assemblies.ToDictionary(x=>x.Id);
        var allHandpans=await db.Handpans.AsNoTracking().ToListAsync(ct);
        var handpansById=allHandpans.ToDictionary(x=>x.Id);
        var paid=await PaidBowlIdsAsync(ct);

        bool MatchesBowl(Bowl bowl)=>(request.MaterialIds.Count==0||request.MaterialIds.Contains(bowl.MaterialId))&&
            (request.ScaleIds.Count==0||(bowl.ScaleId.HasValue&&request.ScaleIds.Contains(bowl.ScaleId.Value)))&&
            (request.Payroll=="all"||(request.Payroll=="calculated")==paid.Contains(bowl.Id));

        var warehouseDates=allEvents.Where(IsHandpanWarehouseEntry)
            .GroupBy(x=>x.HandpanId!.Value).ToDictionary(x=>x.Key,x=>x.Min(y=>y.EventDate));
        var domesticCompletions=new List<CompletedBowl>();
        foreach(var entry in warehouseDates)
        {
            if(!handpansById.TryGetValue(entry.Key,out var handpan)||!assembliesById.TryGetValue(handpan.AssemblyId,out var assembly))continue;
            if(bowlsById.TryGetValue(assembly.TopBowlId,out var top)&&top.HasNotes)domesticCompletions.Add(new(top,entry.Value,false));
            if(bowlsById.TryGetValue(assembly.BottomBowlId,out var bottom)&&bottom.HasNotes)domesticCompletions.Add(new(bottom,entry.Value,false));
        }
        domesticCompletions=domesticCompletions.GroupBy(x=>x.Bowl.Id).Select(x=>x.MinBy(y=>y.CompletedAt)!).ToList();
        var domesticIds=domesticCompletions.Select(x=>x.Bowl.Id).ToHashSet();

        var exportCompletions=allEvents.Where(IsExportCompletion)
            .GroupBy(x=>x.BowlId!.Value).Select(x=>x.MinBy(y=>y.EventDate)!)
            .Where(x=>!domesticIds.Contains(x.BowlId!.Value)&&bowlsById.TryGetValue(x.BowlId.Value,out var bowl)&&bowl.HasNotes)
            .Select(x=>new CompletedBowl(bowlsById[x.BowlId!.Value],x.EventDate,true)).ToList();

        var completed=domesticCompletions.Concat(exportCompletions)
            .Where(x=>MatchesBowl(x.Bowl)&&(request.Destination=="all"||(request.Destination=="export")==x.IsExport)).ToList();
        var selectedEntries=completed.Where(x=>x.CompletedAt>=start&&x.CompletedAt<end).ToList();
        var selected=selectedEntries.Select(x=>x.Bowl).ToList();
        var ids=selected.Select(x=>x.Id).ToHashSet();
        var selectedHandpanIds=warehouseDates.Where(x=>x.Value>=start&&x.Value<end&&handpansById.TryGetValue(x.Key,out var h)&&MatchesHandpan(h))
            .Select(x=>x.Key).ToHashSet();
        var ranged=allEvents.Where(x=>x.EventDate>=start&&x.EventDate<end).ToList();
        var stageEvents=ranged.Where(x=>(x.BowlId.HasValue&&ids.Contains(x.BowlId.Value))||(x.HandpanId.HasValue&&selectedHandpanIds.Contains(x.HandpanId.Value))).ToList();
        var donuts=new List<DonutChart>
        {
            Donut("متریال‌های تولیدشده","تعداد کاسه",selected.GroupBy(x=>x.Material.Name).Select(x=>(x.Key,(double)x.Count()))),
            Donut("زمان مراحل","دقیقه ثبت‌شده",stageEvents.Where(x=>x.Action is ProductionAction.Dimple or ProductionAction.Shape or ProductionAction.Tune or ProductionAction.FineTune).GroupBy(x=>ActionTitle(x.Action)).Select(x=>(x.Key,(double)x.Sum(DurationMinutes)))),
            Donut("سهم تیونرها","از کل تیون",stageEvents.Where(x=>x.Action==ProductionAction.Tune).GroupBy(x=>UserName(x.User)).Select(x=>(x.Key,(double)x.Count()))),
            Donut("سهم فاین‌تیونرها","از کل فاین‌تیون",stageEvents.Where(x=>x.Action==ProductionAction.FineTune).GroupBy(x=>UserName(x.User)).Select(x=>(x.Key,(double)x.Count()))),
            Donut("سهم شیپ‌کارها","از کل شیپ",stageEvents.Where(x=>x.Action==ProductionAction.Shape).GroupBy(x=>UserName(x.User)).Select(x=>(x.Key,(double)x.Count()))),
            Donut("توزیع اسکیل‌ها","بر اساس تعداد نت",selected.Where(x=>x.Scale!=null).GroupBy(x=>NoteCount(x.Scale!.Name)).Select(x=>($"{x.Key} نت",(double)x.Count())))
        };

        var domesticBowlTrend=BuildUniqueTrend(completed.Where(x=>!x.IsExport).Select(x=>(x.Bowl.Id,x.CompletedAt)),end);
        var exportBowlTrend=BuildUniqueTrend(completed.Where(x=>x.IsExport).Select(x=>(x.Bowl.Id,x.CompletedAt)),end);
        var bowlTrend=CombineTrends(domesticBowlTrend,exportBowlTrend);

        bool MatchesHandpan(Handpan handpan)
        {
            if(request.Destination=="export"||!assembliesById.TryGetValue(handpan.AssemblyId,out var assembly))return false;
            if(!bowlsById.TryGetValue(assembly.TopBowlId,out var top)||!MatchesBowl(top))return false;
            return (request.ScaleIds.Count==0||(handpan.ScaleId.HasValue&&request.ScaleIds.Contains(handpan.ScaleId.Value)))&&
                (request.Payroll=="all"||(request.Payroll=="calculated")==paid.Contains(handpan.Id));
        }
        var warehouseEntries=warehouseDates.Where(x=>handpansById.TryGetValue(x.Key,out var handpan)&&MatchesHandpan(handpan))
            .Select(x=>(x.Key,x.Value)).ToList();
        var handpanTrend=BuildUniqueTrend(warehouseEntries,end);
        var bowlCurrent=bowlTrend.LastOrDefault()?.Count??0;var bowlAverage=Average(bowlTrend);
        var handpanCurrent=handpanTrend.LastOrDefault()?.Count??0;var handpanAverage=Average(handpanTrend);
        var rangedHandpanCount=warehouseEntries.Count(x=>x.Value>=start&&x.Value<end);
        return new(start,end.AddTicks(-1),selected.Count(x=>x.BowlType==BowlType.Top),selected.Count(x=>x.BowlType==BowlType.Bottom&&x.HasNotes),selected.Count,
            selectedEntries.Count(x=>!x.IsExport),selectedEntries.Count(x=>x.IsExport),bowlCurrent,bowlAverage,bowlCurrent-bowlAverage,bowlTrend,domesticBowlTrend,exportBowlTrend,
            rangedHandpanCount,handpanCurrent,handpanAverage,handpanCurrent-handpanAverage,handpanTrend,donuts,
            selectedEntries.GroupBy(x=>new{x.Bowl.Material.Name,Scale=x.Bowl.Scale?.Name??"بدون اسکیل",x.IsExport}).Select(x=>new ProductionSummaryRow(x.Key.Name,x.Key.Scale,x.Key.IsExport?"صادراتی":"داخلی",x.Count())).OrderByDescending(x=>x.Count).ToList());
    }
    private async Task<InventoryReport> BuildInventoryAsync(ManagementReportRequest request,CancellationToken ct)
    {
        var (start,end)=Range(request.From,request.To);
        var materials=await db.Materials.AsNoTracking().OrderBy(x=>x.Name).ToListAsync(ct);
        var stock=materials.Select(x=>new MaterialStockRow(x.Id,x.Name,CategoryName(x.Category),x.Quantity,x.TopBowlQuantity,x.BottomBowlQuantity)).ToList();
        var logEvents=await db.ProductionEvents.AsNoTracking().Include(x=>x.User).Where(x=>x.EventDate>=start&&x.EventDate<end&&x.Description.StartsWith(MaterialStockMetadata.Prefix)).OrderByDescending(x=>x.EventDate).ToListAsync(ct);
        var movements=logEvents.Select(x=>(Event:x,Data:MaterialStockMetadata.Decode(x.Description))).Where(x=>x.Data!=null).Select(x=>new MaterialMovementRow(x.Data!.MaterialName,StockKind(x.Data.StockKind),x.Data.Delta,x.Data.Delta>=0?"ورودی":"خروجی",UserName(x.Event.User),x.Event.EventDate,x.Data.Reason??"")).ToList();
        var outflow=movements.Where(x=>x.Delta<0).GroupBy(x=>new{x.MaterialName,x.StockKind}).Select(x=>new MaterialOutflowRow(x.Key.MaterialName,x.Key.StockKind,-x.Sum(y=>y.Delta))).OrderByDescending(x=>x.Quantity).ToList();
        var inventory=new List<InstrumentInventoryRow>();
        if(request.InventoryKind!="materials")
        {
            var handpans=await db.Handpans.AsNoTracking().Include(x=>x.Scale).Include(x=>x.Assembly).ThenInclude(x=>x.TopBowl).ThenInclude(x=>x.Material).Where(x=>x.Stage==ProductionStage.FinishedWarehouse).ToListAsync(ct);
            inventory.AddRange(handpans.Where(x=>(request.MaterialIds.Count==0||request.MaterialIds.Contains(x.Assembly.TopBowl.MaterialId))&&(request.ScaleIds.Count==0||(x.ScaleId.HasValue&&request.ScaleIds.Contains(x.ScaleId.Value)))&&request.Destination!="export").Select(x=>new InstrumentInventoryRow(x.Id,x.SerialNumber,"ساز","داخلی",x.Assembly.TopBowl.Material.Name,x.Scale?.Name??"—",x.UpdatedAt??x.CreatedAt)));
            var export=await db.Bowls.AsNoTracking().Include(x=>x.Material).Include(x=>x.Scale).Where(x=>x.Stage==ProductionStage.ExportWarehouse).ToListAsync(ct);
            inventory.AddRange(export.Where(x=>(request.MaterialIds.Count==0||request.MaterialIds.Contains(x.MaterialId))&&(request.ScaleIds.Count==0||(x.ScaleId.HasValue&&request.ScaleIds.Contains(x.ScaleId.Value)))&&request.Destination!="domestic").Select(x=>new InstrumentInventoryRow(x.Id,x.ProductionCode,x.BowlType==BowlType.Top?"کاسه رو":"کاسه زیر","صادراتی",x.Material.Name,x.Scale?.Name??"—",DateTime.MinValue)));
        }
        return new(request.InventoryKind,start,end.AddTicks(-1),inventory.Count(x=>x.Destination=="داخلی"),inventory.Count(x=>x.Destination=="صادراتی"),inventory,stock,movements,outflow);
    }

    private async Task<byte[]> BuildReportPdfAsync(string kind,ManagementReportRequest request,CancellationToken ct)
    {
        if(kind=="production")return ManagementReportPdfBuilder.Production(await BuildProductionAsync(request,ct));
        if(kind=="inventory")return ManagementReportPdfBuilder.Inventory(await BuildInventoryAsync(request,ct));
        if(kind!="operations")throw new ArgumentException("Unknown report type.",nameof(kind));
        var action=request.Action.HasValue?(ProductionAction?)request.Action.Value:null;
        var report=await mediator.Send(new GetProductionReportQuery(request.From,request.To,request.UserId,action),ct);
        return ManagementReportPdfBuilder.Operations(report,request.From,request.To);
    }
    private async Task<HashSet<Guid>> PaidBowlIdsAsync(CancellationToken ct){var json=await db.PayrollPayments.AsNoTracking().Select(x=>x.HandpanIdsJson).ToListAsync(ct);var paid=json.SelectMany(ParseGuidJson).ToHashSet();var maps=await db.Handpans.AsNoTracking().Include(x=>x.Assembly).Where(x=>paid.Contains(x.Id)).ToListAsync(ct);foreach(var h in maps){paid.Add(h.Assembly.TopBowlId);paid.Add(h.Assembly.BottomBowlId);}return paid;}
    private static List<Guid> ParseGuidJson(string value){try{return JsonSerializer.Deserialize<List<Guid>>(value)??[];}catch{return[];}}
    private static (DateTime Start,DateTime End) Range(DateTime? from,DateTime? to){var now=DateTime.UtcNow.AddHours(3.5);var pc=new PersianCalendar();var start=from?.Date??pc.ToDateTime(pc.GetYear(now),pc.GetMonth(now),1,0,0,0,0);var end=to?.Date.AddDays(1)??now.Date.AddDays(1);return(DateTime.SpecifyKind(start.AddHours(-3.5),DateTimeKind.Utc),DateTime.SpecifyKind(end.AddHours(-3.5),DateTimeKind.Utc));}
    private static List<Guid> ParseIds(string? text)=>string.IsNullOrWhiteSpace(text)?[]:text.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(x=>Guid.TryParse(x,out var id)?id:Guid.Empty).Where(x=>x!=Guid.Empty).Distinct().Take(3).ToList();
    private static List<TrendPoint> BuildUniqueTrend(IEnumerable<(Guid Id,DateTime EventDate)> source,DateTime end)
    {
        var firstEvents=source.GroupBy(x=>x.Id).Select(x=>x.MinBy(y=>y.EventDate)).ToList();
        var months=PersianMonthCalendar.LastMonths(end.AddHours(3.5).AddTicks(-1),6);
        var counts=months.Select(month=>new TrendPoint(month.Label,firstEvents.Count(x=>x.EventDate>=month.UtcStart&&x.EventDate<month.UtcEnd),0)).ToList();
        var avg=Average(counts);return counts.Select(x=>x with{Average=avg}).ToList();
    }
    private static List<TrendPoint> CombineTrends(IReadOnlyList<TrendPoint> domestic,IReadOnlyList<TrendPoint> export)
    {
        var counts=domestic.Select((x,index)=>new TrendPoint(x.Label,x.Count+(index<export.Count?export[index].Count:0),0)).ToList();
        var avg=Average(counts);return counts.Select(x=>x with{Average=avg}).ToList();
    }
    private static bool IsHandpanWarehouseEntry(ProductionEvent productionEvent)=>productionEvent.HandpanId.HasValue&&
        ((productionEvent.Action==ProductionAction.WarehouseEntry&&!productionEvent.Description.StartsWith(MaterialStockMetadata.Prefix,StringComparison.Ordinal))||
         (productionEvent.Action==ProductionAction.Packaging&&!productionEvent.BowlId.HasValue));
    private static bool IsExportCompletion(ProductionEvent productionEvent)=>productionEvent.BowlId.HasValue&&
        ((productionEvent.Action==ProductionAction.Tune&&string.Equals(productionEvent.Description,"Tune completed - export package",StringComparison.Ordinal))||
         (productionEvent.Action==ProductionAction.Packaging&&productionEvent.Description.Contains("Export packaging completed",StringComparison.OrdinalIgnoreCase))||
         (productionEvent.Action==ProductionAction.Sale&&productionEvent.Description.StartsWith("EXPORT_SALE:",StringComparison.Ordinal)));
    private static double Average(IReadOnlyCollection<TrendPoint> points)=>points.Count==0?0:Math.Round(points.Average(x=>x.Count),1);
    private static DonutChart Donut(string title,string subtitle,IEnumerable<(string Label,double Value)> values){var clean=values.Where(x=>x.Value>0).OrderByDescending(x=>x.Value).ToList();var total=clean.Sum(x=>x.Value);return new(title,subtitle,total,clean.Select((x,i)=>new DonutSegment(x.Label,x.Value,total==0?0:Math.Round(x.Value*100/total,1),Palette[i%Palette.Length])).ToList());}
    private static int DurationMinutes(ProductionEvent x)=>x.Duration.HasValue?(x.Duration==OperationDuration.Over60?65:(int)x.Duration.Value*5):0;
    private static int NoteCount(string value){var m=Regex.Match(value,@"\d+");return m.Success&&int.TryParse(m.Value,out var n)?n:0;}
    private static string UserName(User? x)=>x==null?"ثبت نشده":string.IsNullOrWhiteSpace(x.FullName)?x.UserName:x.FullName;
    private static string ActionTitle(ProductionAction a)=>a switch{ProductionAction.Dimple=>"دیمپل",ProductionAction.Shape=>"شیپ",ProductionAction.Tune=>"تیون",ProductionAction.FineTune=>"فاین‌تیون",ProductionAction.Glue=>"چسب",ProductionAction.Packaging=>"بسته‌بندی",ProductionAction.Sale=>"فروش",ProductionAction.QualityCheck=>"کنترل کیفیت",_=>a.ToString()};
    private static string CategoryName(MaterialCategory x)=>x switch{MaterialCategory.BowlMaterial=>"متریال کاسه",MaterialCategory.TopBowl=>"کاسه رو",MaterialCategory.BottomBowl=>"کاسه زیر",_=>"اقلام بسته‌بندی"};
    private static string StockKind(string x)=>x switch{"top"=>"کاسه رو","bottom"=>"کاسه زیر",_=>"موجودی عمومی"};
}

public sealed record ManagementReportRequest(DateTime? From,DateTime? To,List<Guid> MaterialIds,List<Guid> ScaleIds,string Payroll="all",string Destination="all",string InventoryKind="instruments",Guid? UserId=null,int? Action=null);
public sealed record ProductionAnalytics(DateTime From,DateTime To,int TopBowlCount,int BottomNoteBowlCount,int TotalBowlCount,
    int DomesticBowlCount,int ExportBowlCount,int CurrentMonthCount,double MonthlyAverage,double DifferenceFromAverage,List<TrendPoint> Trend,
    List<TrendPoint> DomesticBowlTrend,List<TrendPoint> ExportBowlTrend,
    int TotalHandpanCount,int CurrentMonthHandpanCount,double HandpanMonthlyAverage,double HandpanDifferenceFromAverage,List<TrendPoint> HandpanTrend,
    List<DonutChart> Donuts,List<ProductionSummaryRow> Summary);
file sealed record CompletedBowl(Bowl Bowl,DateTime CompletedAt,bool IsExport);
public sealed record TrendPoint(string Label,int Count,double Average);
public sealed record DonutChart(string Title,string Subtitle,double Total,List<DonutSegment> Segments);
public sealed record DonutSegment(string Label,double Value,double Percentage,string Color);
public sealed record ProductionSummaryRow(string Material,string Scale,string Destination,int Count);
public sealed record InventoryReport(string Kind,DateTime From,DateTime To,int DomesticCount,int ExportCount,List<InstrumentInventoryRow> Instruments,List<MaterialStockRow> Stock,List<MaterialMovementRow> Movements,List<MaterialOutflowRow> Outflow);
public sealed record InstrumentInventoryRow(Guid Id,string Code,string ItemType,string Destination,string Material,string Scale,DateTime EnteredAt);
public sealed record MaterialStockRow(Guid Id,string Name,string Category,int Quantity,int TopQuantity,int BottomQuantity);
public sealed record MaterialMovementRow(string MaterialName,string StockKind,int Delta,string Direction,string PerformedBy,DateTime OccurredAt,string Reason);
public sealed record MaterialOutflowRow(string MaterialName,string StockKind,int Quantity);
