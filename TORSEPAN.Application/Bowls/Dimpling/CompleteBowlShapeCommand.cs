using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteBowlShapeCommand(
    string ProductionCode,
    OperationDuration Duration,
    Guid? ScaleId = null,
    Guid? StretchUserId = null,
    Guid? NoteAreaUserId = null,
    Guid? EditUserId = null) : IRequest<Result<BowlDimpleDto>>;
