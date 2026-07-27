namespace TORSEPAN.Application.Auth.Queries.GetUserMiniList;

public sealed class UserMiniDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}