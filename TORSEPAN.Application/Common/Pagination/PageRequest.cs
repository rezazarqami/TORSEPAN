namespace TORSEPAN.Application.Common.Pagination;

public sealed class PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;

    public PageRequest(
        int page = DefaultPage,
        int pageSize = DefaultPageSize)
    {
        Page = page < 1
            ? DefaultPage
            : page;

        PageSize = pageSize switch
        {
            < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize
        };
    }
}