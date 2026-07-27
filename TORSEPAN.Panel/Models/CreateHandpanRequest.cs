namespace TORSEPAN.Panel.Models;

public sealed class CreateHandpanRequest
{
    public string SerialNumber { get; set; } = string.Empty;

    public string Scale { get; set; } = string.Empty;

    public int Notes { get; set; }

    public bool IsCustom { get; set; }
}