using GridLab.Grid.Paging;

namespace GridLab.Grid;

/// <summary>What the Razor partials render: the spec, one page of rows, the pager and the row being edited (if any).</summary>
public sealed class GridViewModel
{
    public required GridSpec Spec { get; init; }
    public required IReadOnlyList<object> Rows { get; init; }
    public required PagerModel Pager { get; init; }
    public required GridQuery Query { get; init; }
    public object? Editing { get; init; }
    public bool IsNew => Editing is not null && Spec.IsNew(Editing);
    public string? EditingKey => Editing is null || IsNew ? null : Spec.KeyString(Editing);

    /// <summary>Field name prefix used by every editor; the Save action binds with the same prefix.</summary>
    public const string Prefix = "Item";

    public static GridViewModel Create<T>(GridDefinition<T> definition, PagedResult<T> page, GridQuery query, T? editing = null) where T : class =>
        new()
        {
            Spec = definition.Spec,
            Rows = page.Items,
            Pager = new PagerModel(page.PageIndex, page.PageCount, page.PageSize, page.TotalCount, definition.Spec.PageSizes, query.Win),
            Query = query,
            Editing = editing,
        };
}

/// <summary>Partial-view model for one row.</summary>
public sealed record GridRowContext(GridViewModel Grid, object Row);

/// <summary>Partial-view model for one editor cell/field.</summary>
public sealed record GridEditorContext(GridViewModel Grid, GridColumnSpec Column, object Row, string? FormId);
