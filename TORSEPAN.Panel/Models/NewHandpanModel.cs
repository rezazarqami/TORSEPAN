using System.ComponentModel.DataAnnotations;

namespace TORSEPAN.Panel.Models;

public sealed class NewHandpanModel
{
    [Required]
    public string ProductionCode { get; set; } = string.Empty;

    public string Scale { get; set; } = string.Empty;

    [Range(1, 30)]
    public int Notes { get; set; }

    public string Type { get; set; } = "Standard";
}