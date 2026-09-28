namespace GridLab.Grid.Paging;

public enum PagerItemKind { Page, Separator }

/// <summary>A page button or a ". . ." separator in the pager.</summary>
public readonly record struct PagerItem(PagerItemKind Kind, int PageIndex, bool Selected)
{
    public static PagerItem Page(int index, bool selected) => new(PagerItemKind.Page, index, selected);
    public static readonly PagerItem Separator = new(PagerItemKind.Separator, -1, false);
}

/// <summary>
/// Framework-agnostic pager model. Rules:
///  - visible: "auto" => shown only when there is more than one page;
///  - all page buttons are listed while pageCount &lt;= MaxPagesCount (10);
///  - otherwise: first page, a sliding window of 4 consecutive pages, last page, with ". . ." separators;
///  - the window is reused while the current page stays inside it (excluding the ends next to a separator),
///    so clicking neighbouring pages does not make the buttons jump around;
///  - info text "Page {0} of {1} ({2} items)".
/// </summary>
public sealed class PagerModel
{
    public const int DefaultMaxPagesCount = 10;
    public const int SlidingWindowSize = 4;
    public const string DefaultInfoText = "Page {0} of {1} ({2} items)";

    public PagerModel(
        int pageIndex, int pageCount, int pageSize, int totalCount,
        IReadOnlyList<int>? allowedPageSizes = null,
        int? previousWindowStart = null,
        int maxPagesCount = DefaultMaxPagesCount)
    {
        PageCount = Math.Max(1, pageCount);
        PageIndex = Math.Clamp(pageIndex, 0, PageCount - 1);
        PageSize = pageSize;
        TotalCount = totalCount;
        AllowedPageSizes = allowedPageSizes ?? [5, 10, 20];
        (Items, WindowStart) = Build(PageIndex, PageCount, maxPagesCount, previousWindowStart);
    }

    /// <summary>Zero-based page index.</summary>
    public int PageIndex { get; }
    public int PageCount { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public IReadOnlyList<int> AllowedPageSizes { get; }
    public IReadOnlyList<PagerItem> Items { get; }

    /// <summary>First index of the current sliding window, or null when every page is listed. Feed it back as previousWindowStart on the next request to keep the window stable.</summary>
    public int? WindowStart { get; }

    /// <summary>pager.visible = "auto".</summary>
    public bool Visible => PageCount > 1;
    public bool HasPrevious => PageIndex > 0;
    public bool HasNext => PageIndex < PageCount - 1;
    public string InfoText => string.Format(DefaultInfoText, PageIndex + 1, PageCount, TotalCount);

    /// <summary>Automatic page-size choices: half, current, double.</summary>
    public static IReadOnlyList<int> AutoPageSizes(int pageSize) => [Math.Max(1, pageSize / 2), pageSize, pageSize * 2];

    private enum Delimiter { None, Low, High, Both }

    private static (IReadOnlyList<PagerItem> Items, int? WindowStart) Build(int pageIndex, int pageCount, int maxPagesCount, int? previousStart)
    {
        const int w = SlidingWindowSize;

        // isSlidingWindowMode(): pageCount <= 4 || pageCount <= maxPagesCount => plain list
        if (pageCount <= w || pageCount <= maxPagesCount)
            return (Enumerable.Range(0, pageCount).Select(i => PagerItem.Page(i, i == pageIndex)).ToList(), null);

        int start;
        if (previousStart is int prev)
        {
            prev = Math.Clamp(prev, 1, pageCount - w - 1);
            var prevLast = prev + w - 1;
            var prevDelimiter = DelimiterFor(prev, pageCount);
            // indexesForReuse: the window minus the buttons that sit next to a separator
            var reuseFirst = prevDelimiter is Delimiter.Both or Delimiter.Low ? prev + 1 : prev;
            var reuseLast = prevDelimiter is Delimiter.Both or Delimiter.High ? prevLast - 1 : prevLast;

            if (reuseLast < pageCount - 1 && pageIndex >= reuseFirst && pageIndex <= reuseLast)
                start = prev;                       // canReuseSlidingWindow
            else if (pageIndex == prev)
                start = pageIndex - 1;              // stepped onto the window's first button: slide left by one
            else if (pageIndex == prevLast)
                start = pageIndex + 2 - w;          // stepped onto the last button: slide right by one
            else
                start = FreshStart(pageIndex, pageCount);
        }
        else
        {
            start = FreshStart(pageIndex, pageCount);
        }

        start = Math.Clamp(start, 1, pageCount - w - 1);
        var delimiter = DelimiterFor(start, pageCount);

        var items = new List<PagerItem>(w + 4) { PagerItem.Page(0, pageIndex == 0) };
        if (delimiter is Delimiter.Both or Delimiter.Low) items.Add(PagerItem.Separator);
        for (var i = start; i < start + w; i++) items.Add(PagerItem.Page(i, i == pageIndex));
        if (delimiter is Delimiter.Both or Delimiter.High) items.Add(PagerItem.Separator);
        items.Add(PagerItem.Page(pageCount - 1, pageIndex == pageCount - 1));

        return (items, start);

        static int FreshStart(int pageIndex, int pageCount) =>
            pageIndex < w ? 1
            : pageIndex >= pageCount - w ? pageCount - w - 1
            : pageIndex - 1;

        static Delimiter DelimiterFor(int start, int pageCount) =>
            start == 1 ? Delimiter.High
            : start + w == pageCount - 1 ? Delimiter.Low
            : Delimiter.Both;
    }
}
