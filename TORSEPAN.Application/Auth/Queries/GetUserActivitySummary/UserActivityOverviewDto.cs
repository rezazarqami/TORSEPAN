namespace TORSEPAN.Application.Auth.Queries.GetUserActivityOverview;

public sealed class UserActivityOverviewDto
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int InactiveUsers { get; set; }
}