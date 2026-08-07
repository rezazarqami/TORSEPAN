using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteHandpanFinalTuneCommand(string ProductionCode)
    : IRequest<Result<BowlDimpleDto>>;
