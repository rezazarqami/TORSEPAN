namespace TORSEPAN.Application.Auth.Queries.UserStatistics;

public sealed class UserStatisticsResponse
{
    public int TotalUsers { get; set; }

    public int ActiveUsers { get; set; }

    public int InactiveUsers { get; set; }

    public double ActivePercentage { get; set; }

    public double InactivePercentage { get; set; }
}