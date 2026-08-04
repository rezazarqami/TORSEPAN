using MediatR;
using TORSEPAN.Application.Common.Interfaces;

namespace TORSEPAN.Application.Common.Behaviors;

public sealed class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IUserContext _userContext;

    public AuthorizationBehavior(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_userContext.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        return await next();
    }
}