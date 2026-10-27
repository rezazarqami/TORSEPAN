namespace TORSEPAN.Panel.Models;
public sealed class InstrumentWarehouseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid Version { get; set; }
    public static List<InstrumentWarehouseDto> Defaults() => [new() { Id=1, Name="Economy" }, new() { Id=2, Name="Studio" }, new() { Id=3, Name="Signature" }];
}
public sealed record WarehouseMoveItem(Guid HandpanId, Guid ExpectedVersion);
public sealed class WarehouseMoveResult
{
    public string Stage { get; set; } = "";
    public Guid HandpanId { get; set; }
    public int QualityWarehouseId { get; set; }
    public Guid Version { get; set; }
}
