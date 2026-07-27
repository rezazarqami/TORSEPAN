using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserCountByStatus;

public sealed class GetUserCountByStatusQueryHandler
    : IRequestHandler<GetUserCountByStatusQuery, int>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserCountByStatusQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        GetUserCountByStatusQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .Count(x => x.IsActive == request.IsActive);
    }
}