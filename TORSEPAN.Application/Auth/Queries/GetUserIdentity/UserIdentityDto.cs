namespace TORSEPAN.Application.Auth.Queries.GetUserIdentity;

public sealed class UserIdentityDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}