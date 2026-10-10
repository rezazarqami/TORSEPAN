using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TORSEPAN.API.Reporting;

public static class PassportBackupPdf
{
    public static byte[] Build(PassportSnapshot snapshot) => Document.Create(document => document.Page(page =>
    {
        page.Size(PageSizes.A4); page.Margin(26); page.ContentFromRightToLeft();
        page.DefaultTextStyle(x => x.FontFamily("Vazirmatn").FontSize(8).FontColor("#284457"));
        page.Header().PaddingBottom(12).Column(c =>
        {
            c.Item().Background("#143B59").Padding(12).Text("TORSEPAN | آرشیو شناسنامه‌ها و موجودی").FontSize(15).Bold().FontColor(Colors.White);
            c.Item().PaddingTop(5).Text($"زمان تهیه: {Date(snapshot.CapturedUtc)} (تهران) | شامل تمام سوابق ثبت‌شده؛ بدون تصویر").FontSize(8);
        });
        page.Footer().PaddingTop(10).AlignCenter().Text(t =>
        { t.Span("آرشیو اطلاعات - صفحه "); t.CurrentPageNumber(); t.Span(" از "); t.TotalPages(); });
        page.Content().Column(c =>
        {
            c.Spacing(6);
            c.Item().Text($"کل سازها: {snapshot.Records.Count(x => x.Kind == "ساز")} | کاسه‌های مستقل: {snapshot.Records.Count(x => x.Kind != "ساز")}").Bold();
            foreach (var group in PassportBackupSnapshot.Groups)
                c.Item().Text($"{group}: {snapshot.Records.Count(x => x.Group == group && x.Kind == "ساز")} ساز، {snapshot.Records.Count(x => x.Group == group && x.Kind != "ساز")} کاسه");
            c.Item().PaddingTop(8).Text("موجودی فعلی مواد اولیه").FontSize(11).Bold();
            if (snapshot.Materials.Count == 0) c.Item().Text("موجودی ثبت نشده است.");
            foreach (var m in snapshot.Materials)
                c.Item().Text($"{m.Name}: موجودی {m.Quantity} | کاسه رو {m.Top} | کاسه زیر {m.Bottom}");
            foreach (var group in PassportBackupSnapshot.Groups)
            {
                c.Item().EnsureSpace(105).PaddingTop(12).Background("#E7F1F4").Padding(8).Text(group).FontSize(12).Bold();
                var records = snapshot.Records.Where(x => x.Group == group).OrderBy(x => x.Kind).ThenBy(x => x.Code, StringComparer.OrdinalIgnoreCase).ToArray();
                if (records.Length == 0) c.Item().Text("موردی ثبت نشده است.");
                foreach (var r in records)
                {
                    c.Item().EnsureSpace(85).PaddingTop(6).Decoration(decoration =>
                    {
                        // Repeat the passport code on continuation pages of a long history.
                        decoration.Before().BorderBottom(1).BorderColor("#B4CCD8").PaddingBottom(4)
                            .Text($"{r.Kind} {r.Code} | {r.Stage} | اسکیل: {r.Scale} | دیزاین: {r.Design}").FontSize(9).Bold();
                        decoration.Content().PaddingTop(4).Column(card =>
                        {
                            card.Spacing(3);
                            card.Item().Text(r.Details);
                            if (r.Operations.Count == 0) card.Item().Text("عملیات ثبت‌شده‌ای وجود ندارد.");
                            foreach (var e in r.Operations)
                                card.Item().EnsureSpace(35).Column(operation =>
                                {
                                    operation.Item().Text($"{Date(e.Date)} | {e.Action} | {e.Bowl} | {e.Actor} | {e.Result}" +
                                        (string.IsNullOrWhiteSpace(e.Duration) ? "" : $" | مدت: {e.Duration}"));
                                    if (!string.IsNullOrWhiteSpace(e.Description))
                                        operation.Item().PaddingTop(2).PaddingRight(8).Text(e.Description).FontSize(7).FontColor("#5B7180");
                                });
                        });
                    });
                }
            }
        });
    })).GeneratePdf();
    public static string Date(DateTime utc)
    {
        var time = DateTime.SpecifyKind(utc, DateTimeKind.Utc).AddHours(3.5);
        var p = new PersianCalendar();
        return $"{p.GetYear(time):0000}/{p.GetMonth(time):00}/{p.GetDayOfMonth(time):00} {time:HH:mm}";
    }
}
