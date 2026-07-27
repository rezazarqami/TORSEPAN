namespace TORSEPAN.Application.Auth.Queries.GetUserHealth;

public sealed class UserHealthDto
{
    public bool HasUsers { get; set; }

    public bool HasActiveUsers { get; set; }

    public bool HasInactiveUsers { get; set; }
}