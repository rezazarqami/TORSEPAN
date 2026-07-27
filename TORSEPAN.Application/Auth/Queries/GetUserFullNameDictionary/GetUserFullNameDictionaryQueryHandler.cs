using MediatR;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Application.Auth.Queries.GetUserFullNameDictionary;

public sealed class GetUserFullNameDictionaryQueryHandler
    : IRequestHandler<GetUserFullNameDictionaryQuery, Dictionary<Guid, string>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetUserFullNameDictionaryQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<Guid, string>> Handle(
        GetUserFullNameDictionaryQuery request,
        CancellationToken cancellationToken)
    {
        return (await _unitOfWork.Users.GetAllAsync())
            .OrderBy(x => x.FullName)
            .ToDictionary(x => x.Id, x => x.FullName);
    }
}