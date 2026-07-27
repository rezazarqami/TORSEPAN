using MediatR;

namespace TORSEPAN.Application.ProductionEvents.Commands.CreateProductionEvent;

public sealed class CreateProductionEventCommand
    : IRequest<CreateProductionEventCommandResponse>
{
    public Guid HandpanId { get; set; }

    public Guid? AssemblyId { get; set; }

    public Guid? BowlId { get; set; }

    public Guid? UserId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? Duration { get; set; }
}