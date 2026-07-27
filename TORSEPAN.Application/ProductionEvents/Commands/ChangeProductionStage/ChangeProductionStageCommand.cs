using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Commands.ChangeProductionStage;

public sealed class ChangeProductionStageCommand
    : IRequest<ChangeProductionStageCommandResponse>
{
    public Guid HandpanId { get; set; }

    public Guid UserId { get; set; }

    public string NextStage { get; set; } = string.Empty;

    public string? Description { get; set; }
}