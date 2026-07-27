using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserSelection;

public sealed class GetUserSelectionQueryHandler
    : IRequestHandler<GetUserSelectionQuery, List<UserSelectionDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserSelectionQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UserSelectionDto>> Handle(
        GetUserSelectionQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
            .Select(x => new UserSelectionDto
            {
                Value = x.Id,
                Text = $"{x.FullName} ({x.UserName})"
            })
            .ToList();
    }
}