namespace TORSEPAN.Application.Auth.Queries.GetUserNameMap;

public sealed class UserNameMapDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
}