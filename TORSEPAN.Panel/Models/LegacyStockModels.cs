using System.ComponentModel.DataAnnotations;
namespace TORSEPAN.Panel.Models;

public sealed class LegacyStockDraft
{
    [Required(ErrorMessage = "کد کاسه رو را وارد کنید."), StringLength(50)] public string Code { get; set; } = "";
    [Required(ErrorMessage = "متریال را انتخاب کنید.")] public Guid? MaterialId { get; set; }
    [Required(ErrorMessage = "اسکیل ساز را انتخاب کنید.")] public Guid? ScaleId { get; set; }
    [Required(ErrorMessage = "تعداد نت رو را وارد کنید."), Range(1, 100, ErrorMessage = "تعداد نت رو باید ۱ تا ۱۰۰ باشد.")] public int? TopNotes { get; set; }
    [Required(ErrorMessage = "تعداد نت زیر را وارد کنید؛ بدون نت: صفر."), Range(0, 100, ErrorMessage = "تعداد نت زیر باید صفر تا ۱۰۰ باشد.")] public int? BottomNotes { get; set; }
    public int? WarehouseId { get; set; }
    public int? ExportLocation { get; set; }
    public string Destination { get; set; } = "domestic";
    public Guid? DimpleUserId { get; set; }
    public Guid? ShapeUserId { get; set; }
    public Guid? TuneUserId { get; set; }
    public Guid? FineTuneUserId { get; set; }
    public Guid? DesignUserId { get; set; }
    public Guid? DesignTypeId { get; set; }
    public Guid? TopScaleId { get; set; }
    public Guid? BottomScaleId { get; set; }
}
public sealed record LegacyStockRequest(Guid RegistrationId, string Code, Guid MaterialId, Guid ScaleId, int TopNotes, int BottomNotes,
    int WarehouseId, int? ExportLocation, Guid? DimpleUserId, Guid? ShapeUserId, Guid? TuneUserId, Guid? FineTuneUserId, Guid? DesignUserId,
    Guid? TopScaleId = null, Guid? BottomScaleId = null, Guid? DesignTypeId = null);
public sealed record LegacyStockReceipt(Guid HandpanId, string Code, int Stage, int WarehouseId, int? ExportLocation);
