namespace TORSEPAN.Application.Auth.Queries.GetUserCard;

public sealed class UserCardDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}