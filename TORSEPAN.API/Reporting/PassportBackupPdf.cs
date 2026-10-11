using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TORSEPAN.API.Reporting;

public static class PassportBackupPdf
{
    private static readonly string[] Actions = ["ثبت اولیه", "دیمپل", "شیپ", "پخت", "تیون", "چسب", "فاین‌تیون", "کنترل کیفیت", "بسته‌بندی", "ورود به انبار", "فروش"];
    public static byte[] Build(PassportSnapshot snapshot) => Document.Create(document => document.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape()); page.Margin(18); page.ContentFromRightToLeft();
        page.DefaultTextStyle(x => x.FontFamily("Vazirmatn").FontSize(7).FontColor("#243746"));
        page.Header().PaddingBottom(6).Column(c =>
        {
            c.Item().Text("TORSEPAN | آرشیو شناسنامه‌ها و موجودی").FontSize(12).Bold();
            c.Item().Text($"زمان تهیه: {Date(snapshot.CapturedUtc)} (تهران) | سازها: {snapshot.Records.Count(x => x.Kind == "ساز")} | کاسه‌های مستقل: {snapshot.Records.Count(x => x.Kind != "ساز")}");
        });
        page.Footer().PaddingTop(5).AlignCenter().Text(t =>
        { t.Span("صفحه "); t.CurrentPageNumber(); t.Span(" از "); t.TotalPages(); });
        page.Content().Column(c =>
        {
            c.Spacing(5);
            foreach (var group in PassportBackupSnapshot.Groups)
            {
                var records = snapshot.Records.Where(x => x.Group == group).OrderBy(x => x.Kind).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase).ToArray();
                if (records.Length == 0) continue;
                c.Item().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(45); cols.ConstantColumn(58); cols.ConstantColumn(54); cols.ConstantColumn(35); cols.ConstantColumn(45);
                        foreach (var _ in Actions) cols.RelativeColumn();
                    });
                    table.Header(h =>
                    {
                        h.Cell().ColumnSpan(16).Background("#DDE8EC").Padding(5).Text($"{group} ({records.Length})").FontSize(10).Bold();
                        foreach (var title in new[] { "کد / نوع", "اسکیل", "وضعیت", "دیزاین", "کاسه زیر" }.Concat(Actions))
                            h.Cell().Element(HeaderCell).Text(title).Bold();
                    });
                    for (var index = 0; index < records.Length; index++)
                    {
                        var r = records[index];
                        var background = index % 2 == 0 ? "#FFFFFF" : "#F2F5F6";
                        IContainer Cell(IContainer x) => x.Background(background).BorderBottom(.4f).BorderRight(.3f).BorderColor("#C9D4DA").PaddingHorizontal(3).PaddingVertical(4).AlignCenter();
                        table.Cell().Element(Cell).Repeat().Text($"{r.Code}\n{r.Kind}").Bold();
                        table.Cell().Element(Cell).Repeat().Text(r.Scale);
                        table.Cell().Element(Cell).Repeat().Text(r.Stage);
                        table.Cell().Element(Cell).Repeat().Text(r.Design);
                        table.Cell().Element(Cell).Repeat().Text(string.IsNullOrWhiteSpace(r.BottomCode) ? "-" : $"{r.BottomCode}\n({r.BottomDesign})");
                        foreach (var action in Actions)
                        {
                            var operations = r.Operations.Where(x => x.Action == action).OrderBy(x => x.Bowl.StartsWith("کاسه رو", StringComparison.Ordinal) ? 0 : x.Bowl.StartsWith("کاسه زیر", StringComparison.Ordinal) ? 1 : 2).ThenBy(x => x.Date).ThenBy(x => x.Id).ToArray();
                            table.Cell().Element(Cell).Text(operations.Length == 0 ? "-" : string.Join("\n\n", operations.Select(CompactOperation)));
                        }
                        // Keep non-stage events and sale details without repeating verbose passport cards.
                        var extras = r.Operations.Where(x => !Actions.Contains(x.Action)).OrderBy(x => x.Date)
                            .Select(x => $"{x.Action}: {CompactOperation(x).Replace("\n", " ")}").ToList();
                        var sale = r.Details.IndexOf(" | فروش:", StringComparison.Ordinal);
                        if (sale >= 0) extras.Add(r.Details[(sale + 3)..]);
                        if (extras.Count > 0)
                            table.Cell().ColumnSpan(16).Background(background).BorderBottom(.4f).BorderColor("#C9D4DA").Padding(3)
                                .Text($"{r.Code} | {string.Join(" | ", extras)}").FontSize(6.5f);
                    }
                });
            }
            c.Item().EnsureSpace(55).PaddingTop(4).Text("موجودی فعلی مواد اولیه").FontSize(10).Bold();
            if (snapshot.Materials.Count == 0) c.Item().Text("موجودی ثبت نشده است.");
            else c.Item().Table(table =>
            {
                table.ColumnsDefinition(cols => { cols.RelativeColumn(3); cols.RelativeColumn(); cols.RelativeColumn(); cols.RelativeColumn(); });
                table.Header(h => { foreach (var title in new[] { "متریال", "موجودی", "کاسه رو", "کاسه زیر" }) h.Cell().Element(HeaderCell).Text(title).Bold(); });
                foreach (var m in snapshot.Materials)
                    foreach (var value in new[] { m.Name, m.Quantity.ToString(), m.Top.ToString(), m.Bottom.ToString() })
                        table.Cell().BorderBottom(.4f).BorderColor("#C9D4DA").Padding(3).AlignCenter().Text(value);
            });
        });
    })).GeneratePdf();

    private static IContainer HeaderCell(IContainer x) => x.Background("#E9EFF2").BorderBottom(.5f).BorderColor("#B2C4CE").PaddingHorizontal(2).PaddingVertical(4).AlignCenter();
    internal static string CompactOperation(PassportOperation e)
    {
        var bowl = e.Bowl.StartsWith("کاسه رو", StringComparison.Ordinal) ? "رو: " : e.Bowl.StartsWith("کاسه زیر", StringComparison.Ordinal) ? "زیر: " : "";
        var result = e.Result == "انجام شد" ? "" : $" ({e.Result})";
        var contributors = e.Description.Split('|').Where(x => x.Contains("توسط", StringComparison.Ordinal)).Select(x => x.Trim()).ToArray();
        return $"{bowl}{e.Actor}{result}\n{Date(e.Date)[..10]}" + (contributors.Length == 0 ? "" : "\n" + string.Join("، ", contributors));
    }
    public static string Date(DateTime utc)
    {
        var time = DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(3.5);
        var p = new PersianCalendar();
        return $"{p.GetYear(time):0000}/{p.GetMonth(time):00}/{p.GetDayOfMonth(time):00} {time:HH:mm}";
    }
}
