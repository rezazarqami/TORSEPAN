namespace TORSEPAN.Application.Auth.Queries.UserSummary;

public sealed class UserSummaryResponse
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int InactiveUsers { get; set; }

    public string Status =>
        $"{ActiveUsers} Active / {InactiveUsers} Inactive";
}