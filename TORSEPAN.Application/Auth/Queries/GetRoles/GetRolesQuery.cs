using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetRoles;

public sealed record GetRolesQuery : IRequest<List<RoleDto>>;
