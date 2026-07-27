namespace TORSEPAN.Application.Auth.Queries.GetUserStatusLookup;

public sealed class UserStatusLookupDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}