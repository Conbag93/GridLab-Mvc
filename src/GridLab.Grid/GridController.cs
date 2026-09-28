using GridLab.Grid.Paging;
using Microsoft.AspNetCore.Mvc;

namespace GridLab.Grid;

/// <summary>
/// Drop-in CRUD controller for a grid over <typeparamref name="T"/>. Derive, implement <see cref="Define"/>, done.
/// Every step is virtual so a derived controller can swap in a real search, custom validation, audit stamps, etc.
/// </summary>
public abstract class GridController<T>(IGridStore<T> store) : Controller where T : class, new()
{
    protected IGridStore<T> Store { get; } = store;

    private GridDefinition<T>? _definition;
    protected GridDefinition<T> Definition => _definition ??= Define();

    /// <summary>Describe the grid. Called once per request, so it may use injected services (e.g. for drop-down options).</summary>
    protected abstract GridDefinition<T> Define();

    /// <summary>The view that hosts the grid; the library ships "Grid" so nothing is needed in the app.</summary>
    protected virtual string ViewName => "Grid";

    [HttpGet]
    public virtual IActionResult Index(GridQuery query, string? edit = null, bool add = false)
    {
        T? editing = add ? NewItem() : edit is not null ? Store.Find(edit) : null;
        return View(ViewName, BuildModel(query, editing));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public virtual IActionResult Save([Bind(Prefix = GridViewModel.Prefix)] T item, GridQuery query)
    {
        var spec = Definition.Spec;
        var isNew = spec.IsNew(item);

        if (!ModelState.IsValid)
            return View(ViewName, BuildModel(query, item));

        T target;
        if (isNew)
        {
            target = item;
        }
        else
        {
            target = Store.Find(spec.KeyString(item)) ?? throw new InvalidOperationException("Row no longer exists.");
            // Copy only what the grid lets the user edit; read-only fields on the stored row are preserved.
            foreach (var c in spec.EditableColumns) c.Set(target, c.Get(item));
        }

        OnSaving(target, isNew);
        if (isNew) Store.Add(target); else Store.Update(target);
        OnSaved(target, isNew);

        return RedirectToAction(spec.IndexAction, query.ToRouteValues());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public virtual IActionResult Delete(string id, GridQuery query)
    {
        Store.Delete(id);
        return RedirectToAction(Definition.Spec.IndexAction, query.ToRouteValues());
    }

    /// <summary>Loads one page of rows and wraps it for the view. Override to bypass the store entirely.</summary>
    protected virtual GridViewModel BuildModel(GridQuery query, T? editing) =>
        GridViewModel.Create(Definition, Load(query), query, editing);

    /// <summary>
    /// One page of rows, via whichever store flavour is registered:
    /// a paged store gets a normalised query and does its own search/sort/page;
    /// a queryable store gets the grid's LINQ pipeline applied to it.
    /// </summary>
    protected virtual PagedResult<T> Load(GridQuery query)
    {
        var spec = Definition.Spec;
        switch (Store)
        {
            case IPagedGridStore<T> paged:
            {
                var q = spec.Normalize(query);
                var page = paged.Load(q);
                // The requested page no longer exists (e.g. rows were deleted): fall back to the last one.
                if (page.Items.Count == 0 && page.TotalCount > 0 && q.Page > 1)
                {
                    q.Page = Paginator.PageCount(page.TotalCount, q.PageSize);
                    page = paged.Load(q);
                }
                return page;
            }
            case IQueryableGridStore<T> queryable:
                return Filter(queryable.Query(), query).ApplyGrid(spec, query);
            default:
                throw new InvalidOperationException(
                    $"{Store.GetType().Name} must implement IQueryableGridStore<{typeof(T).Name}> or IPagedGridStore<{typeof(T).Name}>, or the controller must override Load().");
        }
    }

    /// <summary>Extra filtering before the grid's own search/sort/page (tenant, soft-delete, permissions). Queryable stores only.</summary>
    protected virtual IQueryable<T> Filter(IQueryable<T> source, GridQuery query) => source;

    protected virtual T NewItem() => new();
    protected virtual void OnSaving(T item, bool isNew) { }
    protected virtual void OnSaved(T item, bool isNew) { }
}
