using System.ComponentModel.DataAnnotations;

namespace TaskForge.Api.Common;

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount)
{
    public bool HasMore => Page * PageSize < TotalCount;
}

// Query string parameters for endpoints that return a page of results.
public class PageQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;

    public int Skip => (Page - 1) * PageSize;
}
