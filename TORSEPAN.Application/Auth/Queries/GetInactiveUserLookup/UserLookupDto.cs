namespace TORSEPAN.Application.Auth.Queries.GetInactiveUserLookup;

public sealed class UserLookupDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;
}