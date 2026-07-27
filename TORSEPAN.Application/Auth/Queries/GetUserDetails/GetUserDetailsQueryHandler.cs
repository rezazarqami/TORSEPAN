using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserDetails;

public sealed class GetUserDetailsQueryHandler
    : IRequestHandler<GetUserDetailsQuery, UserDetailsDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserDetailsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDetailsDto?> Handle(
        GetUserDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserDetailsDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName,
            IsActive = user.IsActive
        };
    }
}