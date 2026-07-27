using MediatR;

namespace TORSEPAN.Application.Auth.Queries.GetUserNameDictionary;

public sealed record GetUserNameDictionaryQuery()
    : IRequest<Dictionary<Guid, string>>;