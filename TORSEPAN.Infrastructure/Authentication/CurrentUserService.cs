using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TORSEPAN.Application.Interfaces;

namespace TORSEPAN.Infrastructure.Authentication;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (Guid.TryParse(value, out var id))
                return id;

            return null;
        }
    }

    public string? UserName =>
        _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue(ClaimTypes.Name);

    public string? FullName =>
        _httpContextAccessor.HttpContext?
            .User
            .FindFirstValue("FullName");

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role)
    {
        return _httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
    }

    public IEnumerable<string> Roles =>
        _httpContextAccessor.HttpContext?
            .User
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
        ?? Enumerable.Empty<string>();
}