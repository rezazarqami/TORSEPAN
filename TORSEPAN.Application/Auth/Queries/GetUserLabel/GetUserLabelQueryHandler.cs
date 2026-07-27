using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserLabel;

public sealed class GetUserLabelQueryHandler
    : IRequestHandler<GetUserLabelQuery, UserLabelDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserLabelQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<UserLabelDto?> Handle(
        GetUserLabelQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(request.UserId);

        if (user is null)
            return null;

        return new UserLabelDto
        {
            Id = user.Id,
            UserName = user.UserName,
            FullName = user.FullName
        };
    }
}