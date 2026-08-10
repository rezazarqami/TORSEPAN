using System.ComponentModel.DataAnnotations;

namespace TORSEPAN.Panel.Models;

public sealed class CreateMaterialRequest
{
    [Required(ErrorMessage = "نام ماده اولیه الزامی است.")]
    [StringLength(100, ErrorMessage = "نام ماده اولیه حداکثر می‌تواند ۱۰۰ کاراکتر باشد.")]
    public string Name { get; set; } = string.Empty;
}
