namespace TORSEPAN.Application.Auth.Queries.GetUserState;

public sealed class UserStateDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}