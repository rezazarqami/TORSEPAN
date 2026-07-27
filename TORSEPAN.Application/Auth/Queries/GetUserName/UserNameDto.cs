namespace TORSEPAN.Application.Auth.Queries.GetUserName;

public sealed class UserNameDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;
}