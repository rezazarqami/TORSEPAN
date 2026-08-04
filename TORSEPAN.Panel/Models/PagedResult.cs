namespace TORSEPAN.Panel.Models;

public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalItems { get; set; }

    public int TotalPages =>
        PageSize == 0
            ? 0
            : (int)Math.Ceiling((double)TotalItems / PageSize);
}