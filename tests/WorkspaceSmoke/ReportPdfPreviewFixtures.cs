using QuestPDF.Drawing;
using QuestPDF.Infrastructure;
using TORSEPAN.API.Controllers;
using TORSEPAN.API.Reporting;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

internal static class ReportPdfPreviewFixtures
{
    public static IReadOnlyDictionary<string, byte[]> Build()
    {
        QuestPDF.Settings.License=LicenseType.Community;
        QuestPDF.Settings.EnableDebugging=true;
        using var font=typeof(ManagementReportsController).Assembly.GetManifestResourceStream("TORSEPAN.API.Assets.Vazirmatn-Regular.ttf")
            ?? throw new InvalidOperationException("Embedded Vazirmatn font was not found.");
        FontManager.RegisterFont(font);

        var months=new[]{"1405/01","1405/02","1405/03","1405/04","1405/05","1405/06"};
        var trend=months.Select((x,i)=>new TrendPoint(x,new[]{31,42,38,54,61,73}[i],49.8)).ToList();
        var domesticTrend=months.Select((x,i)=>new TrendPoint(x,new[]{20,27,23,33,36,42}[i],30.2)).ToList();
        var exportTrend=months.Select((x,i)=>new TrendPoint(x,new[]{11,15,15,21,25,31}[i],19.7)).ToList();
        var handpanTrend=months.Select((x,i)=>new TrendPoint(x,new[]{12,17,14,21,25,29}[i],19.7)).ToList();
        var charts=new[]{"متریال‌های تولیدشده","زمان مراحل","سهم تیونرها","سهم فاین‌تیونرها","سهم شیپ‌کارها","توزیع اسکیل‌ها"}
            .Select((title,index)=>new DonutChart(title,"خلاصه تحلیلی",100,[
                new DonutSegment(index%2==0?"استیل":"رضا",55,55,"#176B87"),
                new DonutSegment(index%2==0?"ولوت":"علیرضا",30,30,"#27A17B"),
                new DonutSegment(index%2==0?"سایر":"جاوید",15,15,"#F1B84B")])).ToList();
        var production=new ProductionAnalytics(new DateTime(2026,3,21,0,0,0,DateTimeKind.Utc),new DateTime(2026,9,11,0,0,0,DateTimeKind.Utc),66,25,91,60,31,73,49.8,23.2,trend,domesticTrend,exportTrend,38,29,19.7,9.3,handpanTrend,charts,
            Enumerable.Range(1,22).Select(i=>new ProductionSummaryRow(i%2==0?"Stainless Steel":"VELVET",$"{8+i%5} نت",i%3==0?"صادراتی":"داخلی",i+2)).ToList());

        var userId=Guid.NewGuid();
        var operationTrend=months.Select((x,i)=>new ReportTrendItem(x,new[]{13,18,16,22,25,29}[i],20.5)).ToList();
        var durationTrend=months.Select((x,i)=>new ReportTrendItem(x,new[]{260,355,310,445,510,590}[i],411.7)).ToList();
        var performance=new UserPerformanceItem
        {
            UserId=userId,UserName="reza",FullName="رضا ضرغامی",OperationCount=46,DurationMinutes=590,
            TimedOperations=[
                new TimedOperationPerformanceItem{Action=2,ActionTitle="دیمپل",Count=12,TotalDurationMinutes=260,AverageDurationMinutes=21.7},
                new TimedOperationPerformanceItem{Action=3,ActionTitle="شیپ",Count=9,TotalDurationMinutes=210,AverageDurationMinutes=23.3},
                new TimedOperationPerformanceItem{Action=6,ActionTitle="تیون",Count=8,TotalDurationMinutes=120,AverageDurationMinutes=15}],
            UntimedOperations=[new UntimedOperationPerformanceItem{Action=5,ActionTitle="چسب",Count=10},new UntimedOperationPerformanceItem{Action=8,ActionTitle="کنترل کیفیت",Count=7}]
        };
        var activities=Enumerable.Range(1,65).Select(i=>new ProductionActivityItem
        {
            Id=Guid.NewGuid(),EventDate=new DateTime(2026,9,10,8,0,0,DateTimeKind.Utc).AddMinutes(i*11),UserId=userId,UserName="reza",FullName="رضا ضرغامی",
            Action=i%2==0?2:5,ActionTitle=i%2==0?"دیمپل":"چسب",ProductionCode=$"TS-{700+i}",DurationTitle=i%2==0?"۲۰ دقیقه":"ثبت نشده",Description="ثبت عملیات تولید در کارگاه"
        }).ToList();
        var operations=new GetProductionReportResponse
        {
            TotalOperations=65,TotalDurationMinutes=590,UserPerformance=[performance],Activities=activities,Trend=operationTrend,DurationTrend=durationTrend,
            UserTrend=new UserTrendSummaryItem{CurrentOperationCount=29,PreviousOperationCount=25,AverageOperationCount=20.5,CurrentDurationMinutes=590,PreviousDurationMinutes=510,AverageDurationMinutes=411.7}
        };

        var inventory=new InventoryReport("materials",new DateTime(2026,8,23,0,0,0,DateTimeKind.Utc),new DateTime(2026,9,11,0,0,0,DateTimeKind.Utc),0,0,[],
            Enumerable.Range(1,14).Select(i=>new MaterialStockRow(Guid.NewGuid(),$"متریال نمونه {i}",i%2==0?"متریال کاسه":"اقلام بسته‌بندی",i*4,i*2,i*3)).ToList(),
            Enumerable.Range(1,48).Select(i=>new MaterialMovementRow($"متریال نمونه {1+i%8}",i%2==0?"کاسه رو":"موجودی عمومی",i%3==0?-i:i,i%3==0?"خروجی":"ورودی","رضا ضرغامی",new DateTime(2026,9,1,7,0,0,DateTimeKind.Utc).AddHours(i),"ثبت گردش انبار")).ToList(),
            Enumerable.Range(1,8).Select(i=>new MaterialOutflowRow($"متریال نمونه {i}",i%2==0?"کاسه رو":"کاسه زیر",i*3)).ToList());

        return new Dictionary<string,byte[]>
        {
            ["production-report-preview.pdf"]=ManagementReportPdfBuilder.Production(production),
            ["operations-report-preview.pdf"]=ManagementReportPdfBuilder.Operations(operations,new DateTime(2026,8,23),new DateTime(2026,9,11)),
            ["inventory-report-preview.pdf"]=ManagementReportPdfBuilder.Inventory(inventory)
        };
    }
}
