namespace GridLab.Grid.Paging;

/// <summary>One page of results plus the numbers a pager needs: total count, page index and page size.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int PageIndex, int PageSize)
{
    public int PageCount => Paginator.PageCount(TotalCount, PageSize);
}

public static class Paginator
{
    public static int PageCount(int totalCount, int pageSize) =>
        pageSize <= 0 ? 1 : Math.Max(1, (totalCount + pageSize - 1) / pageSize);

    /// <summary>
    /// Count the whole set, then Skip/Take one page.
    /// Also clamps the page index the way the DataGrid does when the requested page no longer exists (e.g. after a delete).
    /// </summary>
    public static PagedResult<T> Page<T>(IEnumerable<T> source, int pageIndex, int pageSize)
    {
        var all = source as IReadOnlyList<T> ?? source.ToList();
        if (pageSize <= 0) return new(all, all.Count, 0, all.Count);

        pageIndex = Math.Clamp(pageIndex, 0, PageCount(all.Count, pageSize) - 1);
        var items = all.Skip(pageIndex * pageSize).Take(pageSize).ToList();
        return new(items, all.Count, pageIndex, pageSize);
    }
}
