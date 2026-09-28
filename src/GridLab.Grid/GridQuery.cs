using Microsoft.AspNetCore.Routing;

namespace GridLab.Grid;

/// <summary>
/// Everything the grid needs from the request: search text, sort, page and page size.
/// Bound straight from the query string (?search=&amp;sort=&amp;desc=&amp;page=&amp;pageSize=).
/// The same object is the source of truth for every link the grid renders, so state survives navigation.
/// </summary>
public sealed class GridQuery
{
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public bool Desc { get; set; }
    /// <summary>1-based page number.</summary>
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
    /// <summary>Pager sliding-window start, echoed back so the page buttons stay put (see PagerModel).</summary>
    public int? Win { get; set; }

    /// <summary>Route values for a link that keeps the current state; pass overrides to change parts of it.</summary>
    public RouteValueDictionary ToRouteValues(
        int? page = null, int? pageSize = null, string? sort = null, bool? desc = null, int? win = null,
        bool keepSearch = true, bool keepWin = true, string? edit = null, bool add = false, string? id = null)
    {
        var r = new RouteValueDictionary();
        if (keepSearch && !string.IsNullOrWhiteSpace(Search)) r["search"] = Search;
        var s = sort ?? Sort;
        if (!string.IsNullOrEmpty(s)) r["sort"] = s;
        if (desc ?? Desc) r["desc"] = true;
        var p = page ?? Page;
        if (p > 1) r["page"] = p;
        var ps = pageSize ?? PageSize;
        if (ps > 0) r["pageSize"] = ps;
        var w = win ?? (keepWin ? Win : null);
        if (w is not null) r["win"] = w;
        if (edit is not null) r["edit"] = edit;
        if (add) r["add"] = true;
        if (id is not null) r["id"] = id;
        return r;
    }
}
