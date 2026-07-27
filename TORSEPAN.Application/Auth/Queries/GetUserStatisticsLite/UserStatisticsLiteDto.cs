namespace TORSEPAN.Application.Auth.Queries.GetUserStatisticsLite;

public sealed class UserStatisticsLiteDto
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int InactiveUsers { get; set; }
}