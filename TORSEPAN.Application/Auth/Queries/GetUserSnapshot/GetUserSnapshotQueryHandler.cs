using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserSnapshot;

public sealed class GetUserSnapshotQueryHandler
    : IRequestHandler<GetUserSnapshotQuery, UserSnapshotDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserSnapshotQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSnapshotDto?> Handle(
        GetUserSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserSnapshotDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}