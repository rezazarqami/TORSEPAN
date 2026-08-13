using MediatR;
using TORSEPAN.Application.Common.Results;

namespace TORSEPAN.Application.Bowls.Dimpling;

public sealed record AddProductionNoteCommand(string ProductionCode, string Description)
    : IRequest<Result<bool>>;
