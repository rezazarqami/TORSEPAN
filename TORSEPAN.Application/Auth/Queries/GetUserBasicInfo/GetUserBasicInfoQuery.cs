using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserBasicInfo;

public sealed record GetUserBasicInfoQuery()
    : IRequest<List<UserBasicInfoDto>>;