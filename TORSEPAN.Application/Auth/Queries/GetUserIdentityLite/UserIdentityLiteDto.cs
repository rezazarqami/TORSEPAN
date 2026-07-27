namespace TORSEPAN.Application.Auth.Queries.GetUserIdentityLite;

public sealed class UserIdentityLiteDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;
}