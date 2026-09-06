using System.Globalization;
using System.Security.Claims;
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
        var rows=await query.OrderByDescending(x=>x.EventDate).ThenBy(x=>x.Id)
            .Skip((page-1)*50).Take(50).Select(x=>new{
                x.Id,x.EventDate,x.Action,x.Result,x.Duration,x.Description,
                Code=x.Bowl!=null?x.Bowl.ProductionCode:x.Handpan!=null?x.Handpan.SerialNumber:
                    x.Assembly!=null?x.Assembly.TopBowl.ProductionCode+" / "+x.Assembly.BottomBowl.ProductionCode:"",
                BowlType=x.Bowl!=null?(BowlType?)x.Bowl.BowlType:null
            }).ToListAsync(ct);
        return Ok(new{From=start,To=end,Total=total,Completed=completed,Page=page,PageSize=50,
            Items=rows.Select(x=>new{
                x.Id,x.EventDate,x.Code,
                Operation=Operation(x.Action)+(x.BowlType.HasValue?(x.BowlType==BowlType.Top?" کاسه رو":" کاسه زیر"):""),
                Result=Result(x.Result),
                Duration=x.Duration.HasValue?(x.Duration==OperationDuration.Over60?"بیش از ۶۰ دقیقه":$"{(int)x.Duration.Value*5} دقیقه"):"—",
                Details=Details(x.Description)
            })});
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
