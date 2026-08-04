using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Features.Bowls.Commands.CreateBowl;

public sealed class CreateBowlCommand : IRequest<Result<Guid>>
{
    public string ProductionCode { get; set; } = string.Empty;

    public BowlType BowlType { get; set; }

    public bool HasNotes { get; set; }

    public InstrumentType InstrumentType { get; set; }

    public Guid MaterialId { get; set; }
}