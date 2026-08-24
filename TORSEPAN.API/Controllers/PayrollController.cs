using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TORSEPAN.Domain.Entities;
using TORSEPAN.Domain.Enums;
using TORSEPAN.Infrastructure.Persistence;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace TORSEPAN.API.Controllers;

[ApiController, Route("api/payroll"), Authorize(Roles = "Administrator,ProductionManager")]
public sealed class PayrollController(TORSEPANDbContext db, IHttpClientFactory httpFactory, IConfiguration configuration) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] bool readyForQc = false, [FromQuery] bool readyForPackaging = false,
        [FromQuery] bool enteredWarehouse = false, [FromQuery] bool readyForExportPackaging = false,
        [FromQuery] bool exportWarehouse = false, CancellationToken ct = default)
        => Ok(await CalculateAsync(from, to, readyForQc, readyForPackaging, enteredWarehouse,
            readyForExportPackaging, exportWarehouse, ct));

    [HttpPost("payments")]
    public async Task<IActionResult> MarkPaid(PayrollPaymentRequest request, CancellationToken ct)
    {
        var calculation = await CalculateAsync(request.From, request.To, request.ReadyForQc,
            request.ReadyForPackaging, request.EnteredWarehouse, request.ReadyForExportPackaging,
            request.ExportWarehouse, ct);
        if (calculation.HandpanIds.Count == 0 || calculation.Lines.Count == 0)
            return BadRequest("ساز پرداخت‌نشده‌ای در این بازه وجود ندارد.");
        var payment = new PayrollPayment(
            DateTime.SpecifyKind(calculation.From, DateTimeKind.Utc), DateTime.SpecifyKind(calculation.To, DateTimeKind.Utc),
            User.Identity?.Name ?? "کاربر سیستم", calculation.Lines.Sum(x => x.Total),
            JsonSerializer.Serialize(calculation.HandpanIds), JsonSerializer.Serialize(calculation.HandpanCodes),
            JsonSerializer.Serialize(calculation.Lines));
        db.PayrollPayments.Add(payment);
        await db.SaveChangesAsync(ct);
        return Ok(new { payment.Id });
    }

    [HttpGet("report.pdf")]
    public async Task<IActionResult> Pdf([FromQuery] DateTime? from,[FromQuery] DateTime? to,[FromQuery] bool readyForQc=false,[FromQuery] bool readyForPackaging=false,[FromQuery] bool enteredWarehouse=false,[FromQuery] bool readyForExportPackaging=false,[FromQuery] bool exportWarehouse=false,CancellationToken ct=default)
    { var c=await CalculateAsync(from,to,readyForQc,readyForPackaging,enteredWarehouse,readyForExportPackaging,exportWarehouse,ct);return File(BuildPdf(c),"application/pdf",$"torsepan-payroll-{c.From:yyyyMMdd}-{c.To:yyyyMMdd}.pdf"); }

    [HttpPost("report/telegram")]
    public async Task<IActionResult> SendPdf(PayrollPaymentRequest r,CancellationToken ct)
    {
        var c=await CalculateAsync(r.From,r.To,r.ReadyForQc,r.ReadyForPackaging,r.EnteredWarehouse,r.ReadyForExportPackaging,r.ExportWarehouse,ct);
        var relay=configuration["Telegram:BackupRelayUrl"]??configuration["Telegram:RelayUrl"];
        if(string.IsNullOrWhiteSpace(relay))return Problem("Telegram relay is not configured.");
        relay=relay.Replace("telegram-database-backup","telegram-payroll-report").Replace("telegram-inventory-alert","telegram-payroll-report")
            .Replace("/database-backup","/payroll-report").Replace("/inventory-alert","/payroll-report");
        using var form=new MultipartFormDataContent();var bytes=BuildPdf(c);form.Add(new ByteArrayContent(bytes),"report",$"torsepan-payroll-{c.From:yyyyMMdd}-{c.To:yyyyMMdd}.pdf");
        using var request=new HttpRequestMessage(HttpMethod.Post,relay){Content=form};request.Headers.Add("X-Relay-Secret",configuration["Telegram:RelaySecret"]);
        var response=await httpFactory.CreateClient().SendAsync(request,CancellationToken.None);return response.IsSuccessStatusCode?Ok():StatusCode((int)response.StatusCode);
    }

    [HttpGet("payments")]
    public async Task<IActionResult> Payments(CancellationToken ct)
    {
        var payments = await db.PayrollPayments.AsNoTracking().OrderByDescending(x => x.PaidAt).ToListAsync(ct);
        return Ok(payments.Select(x => new PayrollPaymentDto(x.Id, x.From, x.To, x.PaidAt, x.PaidBy,
            x.TotalAmount, Deserialize<string>(x.HandpanCodesJson), Deserialize<PayrollLine>(x.LinesJson))));
    }

    [HttpPost("rates")]
    public async Task<IActionResult> SaveRate(PayrollRateRequest request, CancellationToken ct)
    {
        var action = (ProductionAction)request.Action;
        var materialId = action == ProductionAction.Glue ? null : request.MaterialId;
        var bowlType = action == ProductionAction.Glue ? null : request.BowlType.HasValue ? (BowlType?)request.BowlType.Value : null;
        var scaleId = action == ProductionAction.Glue ? null : request.ScaleId;
        PayrollRate? rate = null;
        if (request.Id.HasValue)
        {
            rate = await db.PayrollRates.FirstOrDefaultAsync(x => x.Id == request.Id.Value, ct);
            if (rate is null) return NotFound();
            rate.Update(action, materialId, bowlType, scaleId, request.Amount);
        }
        var matches = await db.PayrollRates.Where(x => x.Action == action && x.MaterialId == materialId &&
            x.BowlType == bowlType && x.ScaleId == scaleId && (!request.Id.HasValue || x.Id != request.Id.Value)).ToListAsync(ct);
        if (rate is null && matches.Count > 0)
        {
            rate = matches[0];
            rate.SetAmount(request.Amount);
            matches.RemoveAt(0);
        }
        else if (rate is null)
        {
            db.PayrollRates.Add(new PayrollRate(action, materialId, bowlType, scaleId, request.Amount));
        }
        db.PayrollRates.RemoveRange(matches);
        await db.SaveChangesAsync(ct); return NoContent();
    }

    [HttpDelete("rates/{id:guid}")]
    public async Task<IActionResult> DeleteRate(Guid id, CancellationToken ct)
    {
        var rate = await db.PayrollRates.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (rate is null) return NotFound();
        db.PayrollRates.Remove(rate);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPut("users/order")]
    public async Task<IActionResult> SaveOrder(List<UserOrderRequest> items, CancellationToken ct)
    {
        var ids = items.Select(x => x.UserId).ToList();
        var users = await db.Users.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        foreach (var user in users) user.SetDisplayOrder(items.First(x => x.UserId == user.Id).Order);
        await db.SaveChangesAsync(ct); return NoContent();
    }

    private async Task<PayrollCalculation> CalculateAsync(DateTime? from, DateTime? to,
        bool readyForQc, bool readyForPackaging, bool enteredWarehouse,
        bool readyForExportPackaging, bool exportWarehouse, CancellationToken ct)
    {
        var now = DateTime.UtcNow.AddHours(3.5);
        var pc = new PersianCalendar();
        var start = from?.Date ?? pc.ToDateTime(pc.GetYear(now), pc.GetMonth(now), 1, 0, 0, 0, 0);
        var end = to?.Date.AddDays(1) ?? now.AddDays(1).Date;
        var startUtc = DateTime.SpecifyKind(start.AddHours(-3.5), DateTimeKind.Utc);
        var endUtc = DateTime.SpecifyKind(end.AddHours(-3.5), DateTimeKind.Utc);

        var handpanIds = new List<Guid>();
        var handpanCodes = new List<string>();
        var assemblyIds = new List<Guid>();
        var bowlIds = new List<Guid>();
        var exportBowlIds = new List<Guid>();
        var filterByHandpanStage = readyForQc || readyForPackaging || enteredWarehouse || readyForExportPackaging || exportWarehouse;
        if (filterByHandpanStage)
        {
            var alreadyPaid = (await db.PayrollPayments.AsNoTracking().Select(x => x.HandpanIdsJson).ToListAsync(ct))
                .SelectMany(Deserialize<Guid>).ToHashSet();
            var selectedActions = new List<ProductionAction>();
            if (readyForQc) selectedActions.Add(ProductionAction.FineTune);
            if (readyForPackaging) selectedActions.Add(ProductionAction.QualityCheck);
            if (enteredWarehouse) selectedActions.Add(ProductionAction.Packaging);
            var enteredIds = await db.ProductionEvents.AsNoTracking()
                .Where(x => x.EventDate >= startUtc && x.EventDate < endUtc && x.Result == EventResult.Completed &&
                            x.HandpanId.HasValue && selectedActions.Contains(x.Action))
                .Select(x => x.HandpanId!.Value).Distinct().ToListAsync(ct);
            handpanIds = enteredIds.Where(x => !alreadyPaid.Contains(x)).ToList();
            var handpans = await db.Handpans.AsNoTracking().Include(x => x.Assembly)
                .Where(x => handpanIds.Contains(x.Id)).ToListAsync(ct);
            handpanCodes = handpans.Select(x => x.SerialNumber).OrderBy(x => x).ToList();
            assemblyIds = handpans.Select(x => x.AssemblyId).ToList();
            bowlIds = handpans.SelectMany(x => new[] { x.Assembly.TopBowlId, x.Assembly.BottomBowlId }).ToList();
            if (readyForExportPackaging || exportWarehouse)
            {
                exportBowlIds = await db.ProductionEvents.AsNoTracking()
                    .Where(x => x.EventDate >= startUtc && x.EventDate < endUtc && x.Result == EventResult.Completed &&
                                x.BowlId.HasValue && !x.HandpanId.HasValue && x.Bowl != null &&
                                ((readyForExportPackaging && x.Action == ProductionAction.Tune && x.Bowl.Stage == ProductionStage.WaitingForExportPackaging) ||
                                 (exportWarehouse && x.Action == ProductionAction.Packaging && x.Bowl.Stage == ProductionStage.ExportWarehouse)))
                    .Select(x => x.BowlId!.Value).Distinct().ToListAsync(ct);
                exportBowlIds = exportBowlIds.Where(x => !alreadyPaid.Contains(x)).ToList();
                bowlIds.AddRange(exportBowlIds);
                handpanIds.AddRange(exportBowlIds);
                var exportCodes = await db.Bowls.AsNoTracking().Where(x => exportBowlIds.Contains(x.Id)).Select(x => x.ProductionCode).ToListAsync(ct);
                handpanCodes.AddRange(exportCodes.Select(x => $"صادراتی {x}"));
            }
        }

        var eventQuery = db.ProductionEvents.AsNoTracking()
            .Include(x => x.User).Include(x => x.Bowl)!.ThenInclude(x => x.Material)
            .Include(x => x.Bowl)!.ThenInclude(x => x.Scale)
            .Include(x => x.Assembly)!.ThenInclude(x => x.TopBowl).ThenInclude(x => x.Material)
            .Include(x => x.Handpan)!.ThenInclude(x => x.Scale)
            .Include(x => x.Handpan)!.ThenInclude(x => x.Assembly).ThenInclude(x => x.TopBowl).ThenInclude(x => x.Material)
            .Where(x => x.Result == EventResult.Completed && !x.Description.StartsWith("NOTE:") &&
                x.Description != "Released from glue room" &&
                (x.Action == ProductionAction.Dimple || x.Action == ProductionAction.Shape || x.Action == ProductionAction.Glue || x.Action == ProductionAction.Tune || x.Action == ProductionAction.FineTune));
        eventQuery = filterByHandpanStage
            ? eventQuery.Where(x => (x.HandpanId.HasValue && handpanIds.Contains(x.HandpanId.Value)) ||
                                    (x.AssemblyId.HasValue && assemblyIds.Contains(x.AssemblyId.Value)) ||
                                    (x.BowlId.HasValue && bowlIds.Contains(x.BowlId.Value)))
            : eventQuery.Where(x => x.EventDate >= startUtc && x.EventDate < endUtc);

        var events = await eventQuery.ToListAsync(ct);
        var rates = await db.PayrollRates.AsNoTracking().Include(x => x.Material).Include(x => x.Scale).ToListAsync(ct);
        var lines = events.GroupBy(x => new
        {
            x.UserId, x.User.FullName, x.User.UserName, x.User.DisplayOrder, x.Action,
            IsExport = x.BowlId.HasValue && exportBowlIds.Contains(x.BowlId.Value),
            MaterialId = x.Action == ProductionAction.Glue ? (Guid?)null : x.Bowl != null ? x.Bowl.MaterialId : x.Assembly != null ? x.Assembly.TopBowl.MaterialId : x.Handpan != null ? x.Handpan.Assembly.TopBowl.MaterialId : (Guid?)null,
            Material = x.Action == ProductionAction.Glue ? "" : x.Bowl != null ? x.Bowl.Material.Name : x.Assembly != null ? x.Assembly.TopBowl.Material.Name : x.Handpan != null ? x.Handpan.Assembly.TopBowl.Material.Name : "—",
            BowlType = x.Action == ProductionAction.Glue || x.Bowl == null ? (int?)null : (int)x.Bowl.BowlType,
            ScaleId = x.Action == ProductionAction.FineTune && x.Handpan != null ? x.Handpan.ScaleId
                : (x.Action == ProductionAction.Shape || x.Action == ProductionAction.Tune) && x.Bowl != null ? x.Bowl.ScaleId : (Guid?)null,
            Scale = x.Action == ProductionAction.FineTune && x.Handpan != null && x.Handpan.Scale != null ? x.Handpan.Scale.Name
                : (x.Action == ProductionAction.Shape || x.Action == ProductionAction.Tune) && x.Bowl != null && x.Bowl.Scale != null ? x.Bowl.Scale.Name : ""
        }).Select(g =>
        {
            var rate = rates.Where(r => r.Action == g.Key.Action && (!r.MaterialId.HasValue || r.MaterialId == g.Key.MaterialId) && (!r.BowlType.HasValue || (int)r.BowlType == g.Key.BowlType) && (!r.ScaleId.HasValue || r.ScaleId == g.Key.ScaleId))
                .OrderByDescending(r => r.MaterialId.HasValue).ThenByDescending(r => r.BowlType.HasValue).ThenByDescending(r => r.ScaleId.HasValue).FirstOrDefault()?.Amount ?? 0;
            var count = g.Key.Action == ProductionAction.Glue ? g.Where(x => x.HandpanId.HasValue).Select(x => x.HandpanId).Distinct().Count() : g.Count();
            return new PayrollLine(g.Key.UserId, string.IsNullOrWhiteSpace(g.Key.FullName) ? g.Key.UserName : g.Key.FullName, g.Key.DisplayOrder, (int)g.Key.Action, Title(g.Key.Action), g.Key.MaterialId, g.Key.Material, g.Key.BowlType, g.Key.ScaleId, g.Key.Scale, count, rate, count * rate, g.Key.IsExport);
        }).OrderBy(x => x.DisplayOrder).ThenBy(x => x.UserName)
            .ThenBy(x => ActionOrder(x.Action)).ThenBy(x => x.MaterialName)
            .ThenBy(x => x.BowlType).ThenBy(x => x.ScaleName).ToList();

        var users = await db.Users.AsNoTracking().OrderBy(x => x.DisplayOrder).ThenBy(x => x.FullName).Select(x => new PayrollUser(x.Id, x.FullName, x.DisplayOrder)).ToListAsync(ct);
        return new PayrollCalculation(start, end.AddDays(-1), lines, users,
            rates.Select(r => new PayrollRateDto(r.Id, (int)r.Action, Title(r.Action), r.MaterialId, r.Material?.Name ?? "همه متریال‌ها", r.BowlType.HasValue ? (int)r.BowlType : null, r.ScaleId, r.Scale?.Name ?? "", r.Amount)).ToList(),
            handpanIds, handpanCodes, readyForQc, readyForPackaging, enteredWarehouse,
            readyForExportPackaging, exportWarehouse);
    }

    private static List<T> Deserialize<T>(string json) { try { return JsonSerializer.Deserialize<List<T>>(json) ?? []; } catch { return []; } }
    private static byte[] BuildPdf(PayrollCalculation c)
    {
        string Bowl(int? type) => type == 1 ? "کاسه رو" : type == 2 ? "کاسه زیر" : "";
        string Desc(PayrollLine x) => string.Join(" — ", new[] { x.ActionTitle, x.MaterialName, Bowl(x.BowlType), x.ScaleName }
            .Where(v => !string.IsNullOrWhiteSpace(v)));
        var scales = c.Lines.Where(x => !x.IsExport && x.Action == 7 && !string.IsNullOrWhiteSpace(x.ScaleName))
            .GroupBy(x => x.ScaleName).OrderBy(x => x.Key)
            .Select(x => new { x.Key, Count = x.Sum(y => y.Count) }).ToList();

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(18);
            page.DefaultTextStyle(x => x.FontFamily("DejaVu Sans").FontSize(9.5f));
            page.Header().Column(header =>
            {
                header.Item().AlignCenter().Text("TORSEPAN — گزارش عملکرد و دستمزد تولید").FontSize(18).Bold().FontColor(Colors.Green.Darken3);
                header.Item().AlignCenter().Text($"از {c.From:yyyy/MM/dd} تا {c.To:yyyy/MM/dd}").FontSize(11).FontColor(Colors.Grey.Darken1);
            });
            page.Content().PaddingTop(10).Column(col =>
            {
                void AddSection(string title, IEnumerable<PayrollLine> source)
                {
                    var groups = source.GroupBy(x => new { x.UserName, x.DisplayOrder })
                        .OrderBy(x => x.Key.DisplayOrder).ThenBy(x => x.Key.UserName).ToList();
                    if (groups.Count == 0) return;
                    col.Item().PaddingTop(8).PaddingBottom(5).AlignRight().Text(title).FontSize(14).Bold().FontColor(Colors.Green.Darken3);
                    foreach (var group in groups)
                    {
                        var lines = group.OrderBy(x => ActionOrder(x.Action)).ThenBy(x => x.MaterialName)
                            .ThenBy(x => x.BowlType).ThenBy(x => x.ScaleName).ToList();
                        foreach (var chunk in lines.Chunk(6))
                        {
                            col.Item().PaddingBottom(5).Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(92);
                                    for (var i = 0; i < 6; i++) columns.RelativeColumn();
                                    columns.ConstantColumn(105);
                                });
                                table.Cell().Background(Colors.Green.Darken3).Padding(6).AlignMiddle().AlignCenter()
                                    .Text($"جمع کل\n{lines.Sum(x => x.Total):N0}").FontColor(Colors.White).FontSize(10).Bold();
                                for (var i = 0; i < 6; i++)
                                {
                                    var cell = table.Cell().Border(1).BorderColor(Colors.Green.Lighten3).Background(Colors.Grey.Lighten5).Padding(5).AlignMiddle().AlignCenter();
                                    if (i < chunk.Length)
                                    {
                                        var line = chunk[i];
                                        cell.Column(content =>
                                        {
                                            content.Item().Text(Desc(line)).FontSize(9).Bold();
                                            content.Item().Text($"تعداد: {line.Count:N0}").FontSize(9);
                                            content.Item().Text($"مبلغ: {line.Total:N0}").FontSize(9).FontColor(Colors.Green.Darken3).Bold();
                                        });
                                    }
                                    else cell.Text("");
                                }
                                table.Cell().Background(Colors.Green.Lighten4).Border(1).BorderColor(Colors.Green.Lighten2).Padding(6).AlignMiddle().AlignCenter()
                                    .Text(group.Key.UserName).FontSize(10.5f).Bold();
                            });
                        }
                    }
                }

                AddSection("دستمزد اعضای کارگاه", c.Lines.Where(x => !x.IsExport));
                AddSection("دستمزد تولید صادراتی", c.Lines.Where(x => x.IsExport));

                if (scales.Count > 0)
                {
                    col.Item().PaddingTop(10).Background(Colors.Green.Lighten4).Border(1.5f).BorderColor(Colors.Green.Darken2).Padding(9).Column(summary =>
                    {
                        summary.Item().AlignRight().Text("خلاصه سازهای تکمیل‌شده").FontSize(15).Bold().FontColor(Colors.Green.Darken3);
                        summary.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns => { for (var i = 0; i < 4; i++) columns.RelativeColumn(); });
                            foreach (var scale in scales)
                                table.Cell().Border(1).BorderColor(Colors.Green.Lighten2).Background(Colors.White).Padding(7).AlignCenter()
                                    .Text($"{scale.Key}\n{scale.Count:N0} ساز").FontSize(11).Bold();
                        });
                        summary.Item().PaddingTop(7).AlignCenter().Text($"جمع کل ساخت: {scales.Sum(x => x.Count):N0} ساز").FontSize(13).Bold();
                    });
                }
            });
            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("صفحه ");
                text.CurrentPageNumber();
                text.Span(" از ");
                text.TotalPages();
            });
        })).GeneratePdf();
    }
    private static int ActionOrder(int action) => action switch { 2 => 1, 3 => 2, 5 => 3, 6 => 4, 7 => 5, _ => 99 };
    private static string Title(ProductionAction action) => action switch { ProductionAction.Dimple => "دیمپل", ProductionAction.Shape => "شیپ", ProductionAction.Glue => "چسب", ProductionAction.Tune => "تیون", ProductionAction.FineTune => "فاین تیون", _ => action.ToString() };
}

public sealed record PayrollCalculation(DateTime From, DateTime To, List<PayrollLine> Lines, List<PayrollUser> Users, List<PayrollRateDto> Rates, List<Guid> HandpanIds, List<string> HandpanCodes, bool ReadyForQc, bool ReadyForPackaging, bool EnteredWarehouse, bool ReadyForExportPackaging, bool ExportWarehouse);
public sealed record PayrollLine(Guid UserId, string UserName, int DisplayOrder, int Action, string ActionTitle, Guid? MaterialId, string MaterialName, int? BowlType, Guid? ScaleId, string ScaleName, int Count, decimal Rate, decimal Total, bool IsExport = false);
public sealed record PayrollUser(Guid Id, string FullName, int DisplayOrder);
public sealed record PayrollRateDto(Guid Id, int Action, string ActionTitle, Guid? MaterialId, string MaterialName, int? BowlType, Guid? ScaleId, string ScaleName, decimal Amount);
public sealed record PayrollRateRequest(Guid? Id, int Action, Guid? MaterialId, int? BowlType, Guid? ScaleId, decimal Amount);
public sealed record UserOrderRequest(Guid UserId, int Order);
public sealed record PayrollPaymentRequest(DateTime From, DateTime To, bool ReadyForQc, bool ReadyForPackaging, bool EnteredWarehouse, bool ReadyForExportPackaging, bool ExportWarehouse);
public sealed record PayrollPaymentDto(Guid Id, DateTime From, DateTime To, DateTime PaidAt, string PaidBy, decimal TotalAmount, List<string> HandpanCodes, List<PayrollLine> Lines);
