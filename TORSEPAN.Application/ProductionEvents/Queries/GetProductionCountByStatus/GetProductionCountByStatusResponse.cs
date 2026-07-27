namespace TORSEPAN.Application.ProductionEvents.Queries.GetProductionCountByStatus;

public sealed class GetProductionCountByStatusResponse
{
    public int InProduction { get; set; }

    public int Finished { get; set; }

    public int Rejected { get; set; }

    public int ReadyForPackaging { get; set; }

    public int Warehouse { get; set; }
}