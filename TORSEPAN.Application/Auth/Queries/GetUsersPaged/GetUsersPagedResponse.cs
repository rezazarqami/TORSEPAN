namespace TORSEPAN.Application.Auth.Queries.GetUsersPaged;

public sealed class GetUsersPagedResponse
{
    public int TotalCount { get; set; }

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public List<UserDto> Items { get; set; } = [];
}