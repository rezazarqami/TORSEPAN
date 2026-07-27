using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserFullName;

public sealed class GetUserFullNameQueryHandler
    : IRequestHandler<GetUserFullNameQuery, UserFullNameDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserFullNameQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserFullNameDto?> Handle(
        GetUserFullNameQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserFullNameDto
        {
            Id = user.Id,
            FullName = user.FullName
        };
    }
}