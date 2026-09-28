namespace GridLab.Grid;

/// <summary>Runtime (non-generic) description of a grid, consumed by the Razor partials.</summary>
public sealed class GridSpec
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required GridColumnSpec Key { get; init; }
    public required IReadOnlyList<GridColumnSpec> Columns { get; init; }
    public GridEditMode EditMode { get; init; } = GridEditMode.Inline;
    public bool AllowAdd { get; init; } = true;
    public bool AllowEdit { get; init; } = true;
    public bool AllowDelete { get; init; } = true;
    public bool ConfirmDelete { get; init; } = true;
    public bool UseIcons { get; init; } = true;
    public bool ShowSearch { get; init; } = true;
    public string? CssClass { get; init; }
    public string EmptyText { get; init; } = "Nothing to show.";
    public string AddText { get; init; } = "Add";
    public string SearchPlaceholder { get; init; } = "Search…";
    public IReadOnlyList<int> PageSizes { get; init; } = [10, 25, 50];
    public int DefaultPageSize { get; init; } = 10;
    public string? DefaultSort { get; init; }
    public bool DefaultDesc { get; init; }
    public Func<object, string?>? RowClass { get; init; }
    public string IndexAction { get; init; } = "Index";
    public string SaveAction { get; init; } = "Save";
    public string DeleteAction { get; init; } = "Delete";

    public IEnumerable<GridColumnSpec> VisibleColumns => Columns.Where(c => c.Visible);
    public IEnumerable<GridColumnSpec> EditableColumns => Columns.Where(c => c.Editable && c != Key);

    public string KeyString(object row) => GridColumnSpec.ValueString(Key.Get(row));

    /// <summary>
    /// Returns a copy of the request query that is safe to hand to a data layer: the sort is a real, sortable column name
    /// (or the default sort), the page size is one of the allowed sizes and the page number is at least 1.
    /// </summary>
    public GridQuery Normalize(GridQuery query)
    {
        var column = Columns.FirstOrDefault(c => c.Sortable && c.Name.Equals(query.Sort, StringComparison.OrdinalIgnoreCase));
        return new GridQuery
        {
            Search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(),
            Sort = column?.Name ?? DefaultSort ?? Key.Name,
            Desc = column is null ? DefaultDesc : query.Desc,
            Page = Math.Max(1, query.Page),
            PageSize = PageSizes.Contains(query.PageSize) ? query.PageSize : DefaultPageSize,
            Win = query.Win,
        };
    }

    public bool IsNew(object row)
    {
        var k = Key.Get(row);
        return k is null || Equals(k, Key.PropertyType.IsValueType ? Activator.CreateInstance(Key.PropertyType) : null) || k is string { Length: 0 };
    }
}
