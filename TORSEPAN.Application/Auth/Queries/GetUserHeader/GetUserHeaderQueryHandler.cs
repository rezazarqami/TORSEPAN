using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserHeader;

public sealed class GetUserHeaderQueryHandler
    : IRequestHandler<GetUserHeaderQuery, UserHeaderDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserHeaderQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserHeaderDto?> Handle(
        GetUserHeaderQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserHeaderDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}