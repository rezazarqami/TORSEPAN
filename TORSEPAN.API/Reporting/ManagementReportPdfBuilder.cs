using System.Globalization;
using System.Net;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TORSEPAN.API.Controllers;
using TORSEPAN.Application.ProductionEvents.Queries.GetProductionReport;

namespace TORSEPAN.API.Reporting;

public static class ManagementReportPdfBuilder
{
    private const string Navy = "#173F63";
    private const string Teal = "#187A72";
    private const string Gold = "#E7A42D";
    private const string Pale = "#F4F8FA";
    private const string Border = "#DCE7EC";

    public static byte[] Production(ProductionAnalytics report) => Document.Create(document => document.Page(page =>
    {
        Configure(page, "گزارش جامع تولید", PersianRange(report.From, report.To));
        page.Content().PaddingTop(12).Column(column =>
        {
            column.Spacing(10);
            column.Item().Element(c => Metrics(c,
                ("کاسه‌های یکتای تولیدشده", report.TotalBowlCount.ToString("N0")),
                ("کاسه رو", report.TopBowlCount.ToString("N0")),
                ("کاسه زیر نت‌دار", report.BottomNoteBowlCount.ToString("N0")),
                ("سازهای واردشده به انبار", report.TotalHandpanCount.ToString("N0"))));
            column.Item().Element(c => LineChart(c, "روند ماهانه کاسه‌های ساخته‌شده", report.Trend.Select(x => (x.Label, (double)x.Count)).ToList(), report.MonthlyAverage));
            column.Item().Element(c => LineChart(c, "روند ماهانه سازهای واردشده به انبار", report.HandpanTrend.Select(x => (x.Label, (double)x.Count)).ToList(), report.HandpanMonthlyAverage));
            column.Item().PageBreak();
            column.Item().Element(c => Metrics(c,
                ("کاسه این ماه", report.CurrentMonthCount.ToString("N0")),
                ("میانگین ماهانه کاسه", report.MonthlyAverage.ToString("N1")),
                ("فاصله کاسه با میانگین", report.DifferenceFromAverage.ToString("+0.#;-0.#;0")),
                ("ساز این ماه", report.CurrentMonthHandpanCount.ToString("N0")),
                ("میانگین ماهانه ساز", report.HandpanMonthlyAverage.ToString("N1")),
                ("فاصله ساز با میانگین", report.HandpanDifferenceFromAverage.ToString("+0.#;-0.#;0"))));
            column.Item().Element(c => DonutGrid(c, report.Donuts));
            column.Item().PageBreak();
            column.Item().Element(c => Section(c, "جزئیات کامل نمودارها", "مقدار و سهم هر بخش از نمودارهای تحلیلی"));
            column.Item().Element(c => DonutDetailsTable(c, report.Donuts));
            column.Item().Element(c => Section(c, "خلاصه تولید", $"{report.Summary.Sum(x => x.Count):N0} کاسه"));
            column.Item().Element(c => ProductionTable(c, report.Summary));
        });
    })).GeneratePdf();

    public static byte[] Operations(GetProductionReportResponse report, DateTime? from, DateTime? to) => Document.Create(document => document.Page(page =>
    {
        Configure(page, "گزارش جامع عملیات", RequestedRange(from, to));
        page.Content().PaddingTop(12).Column(column =>
        {
            column.Spacing(10);
            column.Item().Element(c => Metrics(c,
                ("کل عملیات", report.TotalOperations.ToString("N0")),
                ("مجموع زمان ثبت‌شده", $"{report.TotalDurationMinutes:N0} دقیقه")));
            if (report.UserTrend is not null)
            {
                column.Item().Element(c => Metrics(c,
                    ("عملیات این ماه", report.UserTrend.CurrentOperationCount.ToString("N0")),
                    ("تغییر با ماه قبل", (report.UserTrend.CurrentOperationCount-report.UserTrend.PreviousOperationCount).ToString("+0;-0;0")),
                    ("میانگین شش‌ماهه عملیات", report.UserTrend.AverageOperationCount.ToString("N1")),
                    ("زمان این ماه", $"{report.UserTrend.CurrentDurationMinutes:N0} دقیقه"),
                    ("تغییر زمان با ماه قبل", $"{report.UserTrend.CurrentDurationMinutes-report.UserTrend.PreviousDurationMinutes:+0;-0;0} دقیقه"),
                    ("میانگین شش‌ماهه زمان", $"{report.UserTrend.AverageDurationMinutes:N1} دقیقه")));
                column.Item().Element(c => LineChart(c, "روند شش‌ماهه تعداد عملیات فرد", report.Trend.Select(x => (x.Label, (double)x.Count)).ToList(), report.UserTrend.AverageOperationCount));
                column.Item().Element(c => LineChart(c, "روند شش‌ماهه زمان ثبت‌شده فرد", report.DurationTrend.Select(x => (x.Label, (double)x.Count)).ToList(), report.UserTrend.AverageDurationMinutes));
            }
            column.Item().Element(c => Section(c, "عملکرد اعضای کارگاه", $"{report.UserPerformance.Count:N0} نفر"));
            column.Item().Element(c => PerformanceTable(c, report.UserPerformance));
            column.Item().PageBreak();
            column.Item().Element(c => Section(c, "ریز کامل فعالیت‌ها", $"{report.Activities.Count:N0} مورد"));
            column.Item().Element(c => ActivityTable(c, report.Activities));
        });
    })).GeneratePdf();

    public static byte[] Inventory(InventoryReport report) => Document.Create(document => document.Page(page =>
    {
        Configure(page, report.Kind == "materials" ? "گزارش جامع انبار مواد اولیه" : "گزارش جامع انبار سازها", PersianRange(report.From, report.To));
        page.Content().PaddingTop(12).Column(column =>
        {
            column.Spacing(10);
            if (report.Kind == "materials")
            {
                column.Item().Element(c => Metrics(c,
                    ("اقلام موجودی", report.Stock.Count.ToString("N0")),
                    ("گردش ثبت‌شده", report.Movements.Count.ToString("N0")),
                    ("مجموع خروجی بازه", report.Outflow.Sum(x => x.Quantity).ToString("N0"))));
                column.Item().Element(c => Section(c, "موجودی فعلی مواد اولیه", $"{report.Stock.Count:N0} قلم"));
                column.Item().Element(c => MaterialStockTable(c, report.Stock));
                column.Item().ShowEntire().Column(block =>
                {
                    block.Item().Element(c => Section(c, "خروجی مواد در بازه", $"{report.Outflow.Sum(x => x.Quantity):N0} عدد"));
                    block.Item().Element(c => OutflowTable(c, report.Outflow));
                });
                column.Item().Element(c => Section(c, "گردش ورودی و خروجی انبار", $"{report.Movements.Count:N0} رویداد"));
                column.Item().Element(c => MovementTable(c, report.Movements));
            }
            else
            {
                column.Item().Element(c => Metrics(c,
                    ("موجودی داخلی", report.DomesticCount.ToString("N0")),
                    ("موجودی صادراتی", report.ExportCount.ToString("N0")),
                    ("کل موجودی ساز و کاسه", report.Instruments.Count.ToString("N0"))));
                column.Item().Element(c => Section(c, "موجودی کامل انبار سازها", $"{report.Instruments.Count:N0} مورد"));
                column.Item().Element(c => InstrumentTable(c, report.Instruments));
            }
        });
    })).GeneratePdf();

    private static void Configure(PageDescriptor page, string title, string subtitle)
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(22);
        page.DefaultTextStyle(x => x.FontFamily("Vazirmatn").FontSize(9).FontColor("#314B5D"));
        page.Header().Background(Navy).PaddingVertical(10).PaddingHorizontal(14).ContentFromRightToLeft().Row(row =>
        {
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text(title).FontSize(17).Bold().FontColor(Colors.White);
                column.Item().Text(subtitle).FontSize(8).FontColor("#CFE0E8");
            });
            row.ConstantItem(155).AlignLeft().AlignMiddle().Text("TORSEPAN  ANALYTICS").FontFamily("Arial").FontSize(11).Bold().FontColor("#FFF0BE");
        });
        page.Footer().BorderTop(1).BorderColor(Border).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(PersianDateTime(DateTime.UtcNow)).FontSize(7).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignCenter().DefaultTextStyle(x=>x.FontSize(8).FontColor(Navy)).Text(text => { text.Span("TORSEPAN  •  "); text.CurrentPageNumber(); text.Span(" / "); text.TotalPages(); });
            row.RelativeItem();
        });
    }

    private static void Metrics(IContainer container, params (string Title, string Value)[] items) => container.ContentFromRightToLeft().Row(row =>
    {
        foreach (var item in items)
            row.RelativeItem().PaddingHorizontal(3).Border(1).BorderColor(Border).Background(Pale).Padding(9).Column(column =>
            {
                column.Item().AlignRight().Text(item.Title).FontSize(7.5f).FontColor("#6F8492");
                column.Item().PaddingTop(3).AlignRight().Text(item.Value).FontSize(14).Bold().FontColor(Teal);
            });
    });

    private static void Section(IContainer container, string title, string subtitle) => container.PaddingTop(4).PaddingBottom(3).ContentFromRightToLeft().Row(row =>
    {
        row.RelativeItem().AlignRight().Text(title).FontSize(12).Bold().FontColor(Navy);
        row.RelativeItem().AlignLeft().Text(subtitle).FontSize(7.5f).FontColor("#718493");
    });

    private static void LineChart(IContainer container, string title, IReadOnlyList<(string Label, double Value)> points, double average) =>
        container.ShowEntire().Border(1).BorderColor(Border).Background("#FBFDFE").Padding(10).Column(column =>
        {
            column.Item().ShowEntire().ContentFromRightToLeft().Row(row =>
            {
                row.RelativeItem().AlignRight().Text(title).FontSize(11).Bold().FontColor(Navy);
                row.RelativeItem().AlignLeft().Text($"میانگین {average:N1}").FontSize(8).FontColor(Teal);
            });
            column.Item().Height(150).Svg(LineSvg(points, average));
        });

    private static string LineSvg(IReadOnlyList<(string Label, double Value)> points, double average)
    {
        const double left=38,right=722,top=22,bottom=128;
        var max=Math.Max(1,points.Select(x=>Math.Max(x.Value,average)).DefaultIfEmpty(1).Max());
        double X(int i)=>points.Count<=1?(left+right)/2:right-i*((right-left)/(points.Count-1));
        double Y(double value)=>bottom-value/max*(bottom-top);
        var culture=CultureInfo.InvariantCulture;
        var path=string.Join(" ",points.Select((x,i)=>$"{X(i).ToString("0.##",culture)},{Y(x.Value).ToString("0.##",culture)}"));
        var svg=new StringBuilder($"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 760 165'>");
        foreach(var y in new[]{30,55,80,105,128})svg.Append($"<line x1='{left}' y1='{y}' x2='{right}' y2='{y}' stroke='#DDE8ED' stroke-width='1' stroke-dasharray='4 5'/>");
        svg.Append($"<line x1='{left}' y1='{Y(average).ToString("0.##",culture)}' x2='{right}' y2='{Y(average).ToString("0.##",culture)}' stroke='{Gold}' stroke-width='2' stroke-dasharray='8 6'/>");
        if(points.Count>0)svg.Append($"<polyline points='{path}' fill='none' stroke='#19788F' stroke-width='4' stroke-linecap='round' stroke-linejoin='round'/>");
        for(var i=0;i<points.Count;i++)
        {
            var x=X(i);var y=Y(points[i].Value);var label=WebUtility.HtmlEncode(points[i].Label);
            svg.Append($"<circle cx='{x.ToString("0.##",culture)}' cy='{y.ToString("0.##",culture)}' r='5' fill='white' stroke='#19788F' stroke-width='3'/>");
            svg.Append($"<text x='{x.ToString("0.##",culture)}' y='{(y-9).ToString("0.##",culture)}' text-anchor='middle' font-family='Arial' font-size='10' font-weight='bold' fill='#173F63'>{points[i].Value:0.#}</text>");
            svg.Append($"<text x='{x.ToString("0.##",culture)}' y='153' text-anchor='middle' font-family='Arial' font-size='9' fill='#647D8D'>{label}</text>");
        }
        return svg.Append("</svg>").ToString();
    }

    private static void DonutGrid(IContainer container, IReadOnlyList<DonutChart> charts) => container.Column(column =>
    {
        column.Spacing(7);
        column.Item().Element(c => Section(c, "شش نمودار کلیدی تولید", "نمای فشرده بازه انتخاب‌شده"));
        foreach (var chunk in charts.Chunk(3))
            column.Item().ShowEntire().ContentFromRightToLeft().Row(row =>
            {
                foreach (var chart in chunk)
                    row.RelativeItem().PaddingHorizontal(3).Border(1).BorderColor(Border).Padding(7).Column(card =>
                    {
                        card.Item().AlignCenter().Text(chart.Title).FontSize(9).Bold().FontColor(Navy);
                        card.Item().Height(82).Svg(DonutSvg(chart));
                        card.Item().AlignCenter().Text(chart.Subtitle).FontSize(6.5f).FontColor("#748793");
                        card.Item().PaddingTop(3).AlignCenter().Text(string.Join("  |  ",chart.Segments.Take(4).Select(x=>$"{x.Label} {x.Percentage:N1}%"))).FontSize(6.2f).FontColor("#526B7C");
                    });
                for(var i=chunk.Length;i<3;i++)row.RelativeItem();
            });
    });

    private static string DonutSvg(DonutChart chart)
    {
        var culture=CultureInfo.InvariantCulture;var offset=0d;
        var svg=new StringBuilder("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 120 100'><circle cx='60' cy='49' r='34' fill='none' stroke='#E7EEF1' stroke-width='14'/>");
        foreach(var segment in chart.Segments)
        {
            var value=Math.Max(0,segment.Percentage);
            svg.Append($"<circle cx='60' cy='49' r='34' fill='none' stroke='{segment.Color}' stroke-width='14' pathLength='100' stroke-dasharray='{value.ToString("0.##",culture)} {(100-value).ToString("0.##",culture)}' stroke-dashoffset='{(-offset).ToString("0.##",culture)}' transform='rotate(-90 60 49)'/>");
            offset+=value;
        }
        svg.Append($"<text x='60' y='53' text-anchor='middle' font-family='Arial' font-size='13' font-weight='bold' fill='#173F63'>{chart.Total:0.#}</text></svg>");
        return svg.ToString();
    }

    private static void DonutDetailsTable(IContainer container, IReadOnlyList<DonutChart> charts) => SimpleTable(container,
        ["نمودار","بخش","مقدار","سهم"],
        charts.SelectMany(chart=>chart.Segments.DefaultIfEmpty(new DonutSegment("بدون داده",0,0,"#DDE7EC")).Select(segment=>new[]{chart.Title,segment.Label,segment.Value.ToString("N1"),$"{segment.Percentage:N1}%"})).ToList(),
        [2f,2f,1f,1f]);

    private static void ProductionTable(IContainer container, IReadOnlyList<ProductionSummaryRow> rows) => SimpleTable(container,
        ["متریال","اسکیل","مقصد","تعداد"],rows.Select(x=>new[]{x.Material,x.Scale,x.Destination,x.Count.ToString("N0")}).ToList(),[2f,2f,1f,1f],3.5f);

    private static void PerformanceTable(IContainer container, IReadOnlyList<UserPerformanceItem> users)
    {
        var rows=new List<string[]>();
        foreach(var user in users)
        {
            var name=string.IsNullOrWhiteSpace(user.FullName)?user.UserName:user.FullName;
            if(user.TimedOperations.Count==0)rows.Add([name,"بدون عملیات زمان‌دار","-","-","-",string.Join("، ",user.UntimedOperations.Select(x=>$"{x.ActionTitle}: {x.Count}"))]);
            else foreach(var operation in user.TimedOperations)rows.Add([name,operation.ActionTitle,operation.Count.ToString("N0"),operation.TotalDurationMinutes.ToString("N0"),operation.AverageDurationMinutes.ToString("N1"),string.Join("، ",user.UntimedOperations.Select(x=>$"{x.ActionTitle}: {x.Count}"))]);
        }
        SimpleTable(container,["کاربر","عملیات زمان‌دار","تعداد","دقیقه کل","میانگین دقیقه","سایر عملیات بدون زمان"],rows,[1.4f,1.2f,.65f,.75f,.85f,2.4f]);
    }

    private static void ActivityTable(IContainer container, IReadOnlyList<ProductionActivityItem> rows) => SimpleTable(container,
        ["تاریخ","کاربر","عملیات","کد","مدت","توضیحات"],rows.Select(x=>new[]{PersianDateTime(x.EventDate),string.IsNullOrWhiteSpace(x.FullName)?x.UserName:x.FullName,x.ActionTitle,string.IsNullOrWhiteSpace(x.ProductionCode)?"-":x.ProductionCode,x.DurationTitle,string.IsNullOrWhiteSpace(x.Description)?"-":x.Description}).ToList(),[1.2f,1.1f,.8f,.75f,.85f,2.6f]);

    private static void InstrumentTable(IContainer container, IReadOnlyList<InstrumentInventoryRow> rows) => SimpleTable(container,
        ["کد","نوع","متریال","اسکیل","مقصد","زمان ورود"],rows.Select(x=>new[]{x.Code,x.ItemType,x.Material,x.Scale,x.Destination,x.EnteredAt==DateTime.MinValue?"-":PersianDateTime(x.EnteredAt)}).ToList(),[1f,1f,1.4f,1.4f,1f,1.2f]);

    private static void MaterialStockTable(IContainer container, IReadOnlyList<MaterialStockRow> rows) => SimpleTable(container,
        ["متریال","دسته","موجودی عمومی","کاسه رو","کاسه زیر"],rows.Select(x=>new[]{x.Name,x.Category,x.Quantity.ToString("N0"),x.TopQuantity.ToString("N0"),x.BottomQuantity.ToString("N0")}).ToList(),[2f,1.4f,1f,1f,1f]);

    private static void OutflowTable(IContainer container, IReadOnlyList<MaterialOutflowRow> rows) => SimpleTable(container,
        ["متریال","نوع موجودی","تعداد خارج‌شده"],rows.Select(x=>new[]{x.MaterialName,x.StockKind,x.Quantity.ToString("N0")}).ToList(),[2f,1.5f,1f]);

    private static void MovementTable(IContainer container, IReadOnlyList<MaterialMovementRow> rows) => SimpleTable(container,
        ["تاریخ","متریال","نوع","گردش","تعداد","ثبت‌کننده","علت"],rows.Select(x=>new[]{PersianDateTime(x.OccurredAt),x.MaterialName,x.StockKind,x.Direction,Math.Abs(x.Delta).ToString("N0"),x.PerformedBy,x.Reason}).ToList(),[1.2f,1.5f,1f,.7f,.7f,1.2f,1.8f]);

    private static void SimpleTable(IContainer container, IReadOnlyList<string> headers, IReadOnlyList<string[]> rows, IReadOnlyList<float> widths, float cellPadding=5) => container.ContentFromRightToLeft().Table(table =>
    {
        table.ColumnsDefinition(columns => { foreach(var width in widths)columns.RelativeColumn(width); });
        table.Header(header => { foreach(var title in headers)header.Cell().Background(Navy).Padding(6).AlignRight().Text(title).Bold().FontColor(Colors.White).FontSize(7.5f); });
        if(rows.Count==0)table.Cell().ColumnSpan((uint)headers.Count).Padding(18).AlignCenter().Text("اطلاعاتی برای نمایش وجود ندارد.").FontColor(Colors.Grey.Medium);
        foreach(var (row,index) in rows.Select((value,index)=>(value,index)))
            foreach(var value in row)
                table.Cell().Background(index%2==0?Colors.White:Pale).BorderBottom(1).BorderColor(Border).Padding(cellPadding).AlignRight().Text(value??"-").FontSize(7);
    });

    private static string RequestedRange(DateTime? from, DateTime? to) => from.HasValue||to.HasValue
        ? $"بازه انتخاب‌شده: {LocalPersianDate(from) ?? "ابتدا"} تا {LocalPersianDate(to) ?? "امروز"}"
        : "همه سوابق ثبت‌شده";
    private static string PersianRange(DateTime from, DateTime to) => $"بازه گزارش: {PersianDate(from)} تا {PersianDate(to)}";
    private static string? LocalPersianDate(DateTime? value) => value.HasValue ? PersianDate(value.Value, false) : null;
    private static string PersianDate(DateTime value, bool utc=true)
    {
        var local=utc?value.AddHours(3.5):value;var pc=new PersianCalendar();
        return $"{pc.GetYear(local):0000}/{pc.GetMonth(local):00}/{pc.GetDayOfMonth(local):00}";
    }
    private static string PersianDateTime(DateTime value)
    {
        var local=value.AddHours(3.5);return $"{PersianDate(value)} {local:HH:mm}";
    }
}
