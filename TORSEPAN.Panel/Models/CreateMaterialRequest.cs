using System.ComponentModel.DataAnnotations;

namespace TORSEPAN.Panel.Models;

public sealed class CreateMaterialRequest
{
    [Required(ErrorMessage = "نام ماده اولیه الزامی است.")]
    [StringLength(100, ErrorMessage = "نام ماده اولیه حداکثر می‌تواند ۱۰۰ کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;

    public int Category { get; set; } = 3;

    [Range(0, int.MaxValue, ErrorMessage = "تعداد اولیه نمی‌تواند منفی باشد.")]
    public int InitialQuantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "تعداد کاسه رو نمی‌تواند منفی باشد.")]
    public int InitialTopBowlQuantity { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "تعداد کاسه زیر نمی‌تواند منفی باشد.")]
    public int InitialBottomBowlQuantity { get; set; }
}
