using MediatR;
using TORSEPAN.Application.Common.Results;
using TORSEPAN.Domain.Enums;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteHandpanFinalTuneCommand(string ProductionCode, OperationDuration Duration)
    : IRequest<Result<BowlDimpleDto>>;
