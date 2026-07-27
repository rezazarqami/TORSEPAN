namespace TORSEPAN.Panel.Models;

public sealed class ProductionDashboardDto
{
    public int TotalHandpans { get; set; }

    public int InProduction { get; set; }

    public int Finished { get; set; }

    public int Rejected { get; set; }

    public double CompletionRate { get; set; }
}