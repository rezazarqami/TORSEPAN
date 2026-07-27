using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserActivationState;

public sealed class GetUserActivationStateQueryHandler
    : IRequestHandler<GetUserActivationStateQuery, UserActivationStateDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserActivationStateQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserActivationStateDto?> Handle(
        GetUserActivationStateQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserActivationStateDto
        {
            Id = user.Id,
            IsActive = user.IsActive
        };
    }
}