using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TORSEPAN.Application.Common.Interfaces;

namespace TORSEPAN.Infrastructure.Authentication;

public sealed class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(value, out var id)
                ? id
                : null;
        }
    }

    public string? UserName =>
        User?.FindFirstValue(ClaimTypes.Name);

    public string? FullName =>
        User?.FindFirstValue("FullName");

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public IReadOnlyCollection<string> Roles =>
        User?
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value)
            .ToArray()
        ?? Array.Empty<string>();

    public bool IsInRole(string role) =>
        User?.IsInRole(role) ?? false;
}