using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserNameDictionary;

public sealed class GetUserNameDictionaryQueryHandler
    : IRequestHandler<GetUserNameDictionaryQuery, Dictionary<Guid, string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserNameDictionaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<Guid, string>> Handle(
        GetUserNameDictionaryQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.UserName)
            .ToDictionary(x => x.Id, x => x.UserName);
    }
}