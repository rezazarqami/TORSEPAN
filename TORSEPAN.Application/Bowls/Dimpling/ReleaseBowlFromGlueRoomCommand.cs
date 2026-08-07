using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record ReleaseBowlFromGlueRoomCommand(
    string ProductionCode) : IRequest<Result<BowlDimpleDto>>;
