using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetRoles;

public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, List<RoleDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetRolesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<RoleDto>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        var roles = await _unitOfWork.Roles.GetAllAsync();

        return roles
            .Select(role => new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                DisplayName = role.DisplayName
            })
            .OrderBy(role => role.DisplayName)
            .ToList();
    }
}
