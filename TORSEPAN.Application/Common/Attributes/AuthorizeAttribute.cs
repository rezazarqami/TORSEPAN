using System;

namespace TORSEPAN.Application.Common.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public sealed class AuthorizeAttribute : Attribute
{
    public AuthorizeAttribute()
    {
    }

    public AuthorizeAttribute(params string[] roles)
    {
        Roles = roles;
    }

    public IReadOnlyList<string> Roles { get; } = Array.Empty<string>();
}