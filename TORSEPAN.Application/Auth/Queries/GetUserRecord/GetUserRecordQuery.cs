using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserRecord;

public sealed record GetUserRecordQuery(Guid UserId)
    : IRequest<UserRecordDto?>;