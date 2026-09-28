using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Html;

namespace GridLab.Grid;

/// <summary>Entry point: <c>GridDefinition.For&lt;User&gt;(g =&gt; g.Column(u =&gt; u.Role, c =&gt; c.Title("Access")))</c>.</summary>
public static class GridDefinition
{
    /// <summary>Builds a grid for <typeparamref name="T"/>: one column per public property, in declaration order, typed from the property type and DataAnnotations. Configure to override.</summary>
    public static GridDefinition<T> For<T>(Action<GridBuilder<T>>? configure = null) where T : class
    {
        var b = new GridBuilder<T>();
        configure?.Invoke(b);
        return b.Build();
    }
}

/// <summary>Immutable, reusable description of a grid for <typeparamref name="T"/>. Create once, use per request.</summary>
public sealed class GridDefinition<T> where T : class
{
    internal GridDefinition(GridSpec spec) => Spec = spec;
    public GridSpec Spec { get; }
}

public sealed class GridBuilder<T> where T : class
{
    private readonly List<GridColumnSpec> _columns;
    private GridColumnSpec? _key;
    private string _id = "grid-" + typeof(T).Name.ToLowerInvariant();
    private string _title = GridColumnSpec.Humanize(typeof(T).Name) + "s";
    private GridEditMode _editMode = GridEditMode.Inline;
    private bool _allowAdd = true, _allowEdit = true, _allowDelete = true, _confirmDelete = true, _useIcons = true, _showSearch = true;
    private string? _cssClass, _emptyText, _addText, _searchPlaceholder, _defaultSort;
    private bool _defaultDesc;
    private IReadOnlyList<int> _pageSizes = [10, 25, 50];
    private int _defaultPageSize = 10;
    private Func<object, string?>? _rowClass;
    private string _indexAction = "Index", _saveAction = "Save", _deleteAction = "Delete";

    internal GridBuilder()
    {
        _columns = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
            .Select(p => new GridColumnSpec(p))
            .ToList();

        _key = _columns.FirstOrDefault(c => c.Property.GetCustomAttribute<System.ComponentModel.DataAnnotations.KeyAttribute>() is not null)
            ?? _columns.FirstOrDefault(c => c.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            ?? _columns.FirstOrDefault(c => c.Name.Equals(typeof(T).Name + "Id", StringComparison.OrdinalIgnoreCase));
        if (_key is not null) { _key.Visible = false; _key.Editable = false; }
    }

    public GridBuilder<T> Id(string id) { _id = id; return this; }
    public GridBuilder<T> Title(string title) { _title = title; return this; }
    public GridBuilder<T> EditMode(GridEditMode mode) { _editMode = mode; return this; }
    public GridBuilder<T> AllowAdd(bool allow = true) { _allowAdd = allow; return this; }
    public GridBuilder<T> AllowEdit(bool allow = true) { _allowEdit = allow; return this; }
    public GridBuilder<T> AllowDelete(bool allow = true) { _allowDelete = allow; return this; }
    public GridBuilder<T> ReadOnly() { _allowAdd = _allowEdit = _allowDelete = false; return this; }
    public GridBuilder<T> ConfirmDelete(bool confirm = true) { _confirmDelete = confirm; return this; }
    public GridBuilder<T> UseIcons(bool icons = true) { _useIcons = icons; return this; }
    public GridBuilder<T> ShowSearch(bool show = true) { _showSearch = show; return this; }
    public GridBuilder<T> CssClass(string cssClass) { _cssClass = cssClass; return this; }
    public GridBuilder<T> EmptyText(string text) { _emptyText = text; return this; }
    public GridBuilder<T> AddText(string text) { _addText = text; return this; }
    public GridBuilder<T> SearchPlaceholder(string text) { _searchPlaceholder = text; return this; }
    public GridBuilder<T> PageSizes(params int[] sizes) { _pageSizes = sizes; if (!sizes.Contains(_defaultPageSize)) _defaultPageSize = sizes[0]; return this; }
    public GridBuilder<T> DefaultPageSize(int size) { _defaultPageSize = size; return this; }
    public GridBuilder<T> RowClass(Func<T, string?> rowClass) { _rowClass = o => rowClass((T)o); return this; }
    public GridBuilder<T> Actions(string index = "Index", string save = "Save", string delete = "Delete") { _indexAction = index; _saveAction = save; _deleteAction = delete; return this; }

    public GridBuilder<T> DefaultSort<TProp>(Expression<Func<T, TProp>> property, bool desc = false)
    {
        _defaultSort = PropertyName(property); _defaultDesc = desc; return this;
    }

    public GridBuilder<T> Key<TProp>(Expression<Func<T, TProp>> property)
    {
        if (_key is not null) { _key.Visible = true; _key.Editable = true; }
        _key = Find(property);
        _key.Visible = false; _key.Editable = false;
        return this;
    }

    /// <summary>Customise the auto-generated column for a property.</summary>
    public GridBuilder<T> Column<TProp>(Expression<Func<T, TProp>> property, Action<GridColumnBuilder<T, TProp>> configure)
    {
        configure(new GridColumnBuilder<T, TProp>(Find(property)));
        return this;
    }

    /// <summary>Hide one or more properties from the grid (they are still bound and saved).</summary>
    public GridBuilder<T> Hide(params Expression<Func<T, object?>>[] properties)
    {
        foreach (var p in properties) Find(p).Visible = false;
        return this;
    }

    /// <summary>Show only these properties, in this order.</summary>
    public GridBuilder<T> Only(params Expression<Func<T, object?>>[] properties)
    {
        var keep = properties.Select(Find).ToList();
        foreach (var c in _columns) c.Visible = keep.Contains(c);
        _columns.Sort((a, b) => Index(a).CompareTo(Index(b)));
        return this;
        int Index(GridColumnSpec c) => keep.IndexOf(c) is var i and >= 0 ? i : int.MaxValue;
    }

    internal GridDefinition<T> Build()
    {
        var key = _key ?? throw new InvalidOperationException($"{typeof(T).Name} has no key. Add an Id / [Key] property or call Key(...).");
        return new GridDefinition<T>(new GridSpec
        {
            Id = _id, Title = _title, Key = key, Columns = _columns.AsReadOnly(),
            EditMode = _editMode, AllowAdd = _allowAdd, AllowEdit = _allowEdit, AllowDelete = _allowDelete,
            ConfirmDelete = _confirmDelete, UseIcons = _useIcons, ShowSearch = _showSearch, CssClass = _cssClass,
            EmptyText = _emptyText ?? "Nothing to show.", AddText = _addText ?? "Add", SearchPlaceholder = _searchPlaceholder ?? "Search…",
            PageSizes = _pageSizes, DefaultPageSize = _defaultPageSize, DefaultSort = _defaultSort, DefaultDesc = _defaultDesc,
            RowClass = _rowClass, IndexAction = _indexAction, SaveAction = _saveAction, DeleteAction = _deleteAction,
        });
    }

    private GridColumnSpec Find(LambdaExpression property)
    {
        var name = PropertyName(property);
        return _columns.FirstOrDefault(c => c.Name == name)
            ?? throw new ArgumentException($"{typeof(T).Name} has no public property '{name}'.");
    }

    internal static string PropertyName(LambdaExpression property)
    {
        var body = property.Body is UnaryExpression { NodeType: ExpressionType.Convert } u ? u.Operand : property.Body;
        return body is MemberExpression m ? m.Member.Name
            : throw new ArgumentException("Expected a property access like x => x.Name.", nameof(property));
    }
}

public sealed class GridColumnBuilder<T, TProp> where T : class
{
    private readonly GridColumnSpec _c;
    internal GridColumnBuilder(GridColumnSpec c) => _c = c;

    public GridColumnBuilder<T, TProp> Title(string title) { _c.Title = title; return this; }
    /// <summary>.NET format string used for display, e.g. "N2", "C", "d", "yyyy-MM-dd".</summary>
    public GridColumnBuilder<T, TProp> Format(string format) { _c.Format = format; return this; }
    public GridColumnBuilder<T, TProp> Type(GridColumnType type) { _c.Type = type; return this; }
    public GridColumnBuilder<T, TProp> ReadOnly() { _c.Editable = false; return this; }
    public GridColumnBuilder<T, TProp> Hidden() { _c.Visible = false; return this; }
    public GridColumnBuilder<T, TProp> Sortable(bool sortable = true) { _c.Sortable = sortable; return this; }
    public GridColumnBuilder<T, TProp> Searchable(bool searchable = true) { _c.Searchable = searchable; return this; }
    public GridColumnBuilder<T, TProp> Align(GridAlign align) { _c.Align = align; return this; }
    public GridColumnBuilder<T, TProp> CssClass(string cssClass) { _c.CssClass = cssClass; return this; }
    public GridColumnBuilder<T, TProp> Width(string width) { _c.Width = width; return this; }
    public GridColumnBuilder<T, TProp> Placeholder(string text) { _c.Placeholder = text; return this; }

    /// <summary>Render and edit the value as a drop-down of these options; display shows the option text.</summary>
    public GridColumnBuilder<T, TProp> Dropdown(IEnumerable<GridOption> options)
    {
        _c.Options = options.ToList(); _c.Type = GridColumnType.Select; return this;
    }

    public GridColumnBuilder<T, TProp> Dropdown<TItem>(IEnumerable<TItem> items, Func<TItem, object?> value, Func<TItem, string> text) =>
        Dropdown(items.Select(i => new GridOption(GridColumnSpec.ValueString(value(i)), text(i))));

    /// <summary>Custom display cell. Works with inline Razor templates: <c>c.Template(@&lt;a href="mailto:@item.Email"&gt;@item.Email&lt;/a&gt;)</c>.</summary>
    public GridColumnBuilder<T, TProp> Template(Func<T, IHtmlContent> template)
    {
        _c.Template = o => template((T)o); return this;
    }

    /// <summary>Custom editor. Render an input named <c>Item.&lt;Property&gt;</c> so it binds back.</summary>
    public GridColumnBuilder<T, TProp> EditorTemplate(Func<T, IHtmlContent> template)
    {
        _c.EditorTemplate = o => template((T)o); return this;
    }
}
