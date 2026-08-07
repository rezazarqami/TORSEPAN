using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record CompleteBowlGlueCommand(
    string ProductionCode,
    string PairedProductionCode) : IRequest<Result<BowlDimpleDto>>;
