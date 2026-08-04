using System.ComponentModel.DataAnnotations;

namespace TORSEPAN.Panel.Models;

public sealed class CreateHandpanRequest
{
    [Required]
    public Guid TopBowlId { get; set; }

    [Required]
    public Guid BottomBowlId { get; set; }

    [Required]
    [StringLength(50)]
    public string SerialNumber { get; set; } = string.Empty;
}