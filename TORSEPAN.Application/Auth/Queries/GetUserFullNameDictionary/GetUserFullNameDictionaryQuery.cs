using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserFullNameDictionary;

public sealed record GetUserFullNameDictionaryQuery()
    : IRequest<Dictionary<Guid, string>>;