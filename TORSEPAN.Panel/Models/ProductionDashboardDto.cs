namespace TORSEPAN.Panel.Models;

public sealed class ProductionDashboardDto
{
    public int TotalBowls { get; set; }

    public int TotalAssemblies { get; set; }

    public int TotalHandpans { get; set; }

    public int InProduction { get; set; }

    public int Finished { get; set; }

    public int Rejected { get; set; }

    public double CompletionRate { get; set; }

    public int WaitingForDimple { get; set; }

    public int WaitingForShape { get; set; }

    public int WaitingForHeatTreatment { get; set; }

    public int WaitingForFurnace { get; set; }

    public int WaitingForTune { get; set; }

    public int WaitingForGlue { get; set; }

    public int WaitingForFineTune { get; set; }

    public int WaitingForQualityControl { get; set; }

    public int WaitingForPackaging { get; set; }

    public List<ProductionQueueItemDto> Queues { get; set; } = [];
}

public sealed class ProductionQueueItemDto
{
    public string Stage { get; set; } = string.Empty;
    public List<string> Codes { get; set; } = [];
}
