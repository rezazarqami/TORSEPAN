namespace TORSEPAN.Application.Auth.Queries.UserCount;

public sealed class UserCountResponse
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int InactiveUsers { get; set; }
}