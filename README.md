# GridLab

A lightweight, server-rendered data grid for ASP.NET Core MVC, plus a sample app that uses it for three different models.

- Bind a grid to any `IQueryable<T>` and get columns typed from the model automatically.
- Search, sort and page on the server; the whole grid state lives in the query string.
- Edit one row at a time: inline (default), in a form panel, or in a modal.
- Override any column, cell or editor with a fluent API; restyle with CSS variables; replace any partial.
- No JavaScript apart from the delete confirmation.

## Solution layout

| Project | What it is |
| --- | --- |
| `src/GridLab.Grid` | The grid library (Razor Class Library). `GridDefinition`, `GridController<T>`, `IGridStore<T>`, the query pipeline, the Razor partials and `gridlab.css`. |
| `src/GridLab.Shared` | Data models (`Product`, `Category`, `User`, `Address`), a generic in-memory repository and seed data. Stands in for a database. |
| `src/GridLab.HandRolled` | MVC app showing the grid for Products (inline editing, drop-down), Users (10,000 rows through a paged data-access class, modal editing, cell template) and Addresses (form editing, custom search). |

## Quick start

```bash
dotnet run --project src/GridLab.HandRolled
```

Then open <http://localhost:5100>.

## Using the grid

**1. Register a store per model.** A store does the editing side (`Find`, `Add`, `Update`, `Delete`) and supplies rows in one of two ways. Pick whichever matches your data layer:

```csharp
// (a) You can hand over an IQueryable (EF Core DbSet, in-memory list). The grid builds the
//     search / sort / Skip / Take expressions itself, so they run in the database.
public interface IQueryableGridStore<T> : IGridStore<T> { IQueryable<T> Query(); }

// (b) Your data access already has a "page N of size M" method that returns items + total.
//     The grid hands over a normalised query and takes the page back.
public interface IPagedGridStore<T> : IGridStore<T> { PagedResult<T> Load(GridQuery query); }
```

The sample uses both. Products and Addresses go through a six-line queryable adapter over the in-memory repository ([RepositoryGridStore.cs](src/GridLab.HandRolled/Data/RepositoryGridStore.cs)). Users go through a stand-in for a typical existing data-access class, [UserDirectory](src/GridLab.Shared/Data/UserDirectory.cs), whose `GetPage(search, sortBy, descending, pageNumber, pageSize)` does its own LINQ and returns a `PagedList<User>` with the total count. Plugging that into the grid is a one-method mapping:

```csharp
public sealed class UserDirectoryGridStore(UserDirectory directory) : IPagedGridStore<User>
{
    public PagedResult<User> Load(GridQuery q)
    {
        var page = directory.GetPage(q.Search, q.Sort, q.Desc, q.Page, q.PageSize);
        return new PagedResult<User>(page.Items, page.TotalCount, page.PageNumber - 1, page.PageSize);
    }
    // Find / Add / Update / Delete forward to the directory the same way
}
```

Before calling `Load`, the grid normalises the query: `Sort` is guaranteed to be a real, sortable column name (or the default sort), `PageSize` is one of the allowed sizes and `Page` is at least 1, so the data layer never sees junk from the URL. If the page comes back empty because rows were deleted, the grid re-requests the last page.

```csharp
builder.Services.AddSingleton(typeof(IGridStore<>), typeof(RepositoryGridStore<>)); // default: queryable
builder.Services.AddSingleton<IGridStore<User>, UserDirectoryGridStore>();            // Users: paged
builder.WebHost.UseStaticWebAssets(); // serves the library's stylesheet
```

**2. Link the stylesheet** in your layout:

```html
<link rel="stylesheet" href="~/_content/GridLab.Grid/gridlab.css" />
```

**3. Write a controller.** This is the entire Users grid:

```csharp
public sealed class UsersController(IGridStore<User> store) : GridController<User>(store)
{
    protected override GridDefinition<User> Define() => GridDefinition.For<User>(g => g
        .EditMode(GridEditMode.Modal)
        .Column(u => u.LastLogin, c => c.Format("dd MMM yyyy"))
        .DefaultSort(u => u.LastName)
        .PageSizes(10, 25, 50, 100));
}
```

`GridController<T>` provides `Index`, `Save` and `Delete`. The library ships the `Grid` view, and your app's `_ViewStart` and `_Layout` apply to it, so the app needs no views for a grid.

### What happens automatically

- **Columns from the type.** One column per public property in declaration order. Strings become text, numbers become right-aligned number inputs, `bool` a checkbox, `DateTime`/`DateOnly` date pickers, enums a drop-down. `[Display]`, `[DisplayFormat]`, `[DataType(DataType.Date)]`, `[Editable(false)]`, `[ScaffoldColumn(false)]` and `[Key]` are honoured. A property named `Id` is the key and is hidden.
- **Search** is a case-insensitive contains across text columns. **Sort** is any column, with the key as tiebreaker so pages are stable. **Paging** shows a page-size selector, a sliding window of page buttons and "Page x of y (n items)". All three are LINQ expressions on the store's `IQueryable`, so they translate to SQL.
- **Editing** uses DataAnnotations validation through `ModelState`; errors render under the field. On update only editable columns are copied onto the stored row, so read-only fields survive.
- **URL state.** `?search=&sort=&desc=&page=&pageSize=` is the whole grid state. Links are bookmarkable and nothing is kept in session.

### Overriding

| Want | Do |
| --- | --- |
| Rename, format, hide, read-only, width | `.Column(x => x.Prop, c => c.Title("…").Format("N2").Hidden().ReadOnly().Width("8rem"))` |
| Drop-down for a property | `c.Dropdown(categories, x => x.Id, x => x.Name)` or `c.Dropdown(IEnumerable<GridOption>)`; enums are automatic |
| Custom cell | `c.Template(u => new HtmlString("…"))` |
| Custom editor | `c.EditorTemplate(...)` rendering an input named `Item.<Prop>` |
| Choose and order columns | `.Only(x => x.A, x => x.B)` or `.Hide(...)` |
| Edit mode, buttons, texts | `.EditMode(GridEditMode.Inline / Form / Modal)`, `.AllowAdd/AllowEdit/AllowDelete(false)`, `.UseIcons(false)`, `.ConfirmDelete(false)`, `.EmptyText("…")` |
| Search handled by your code | implement `IPagedGridStore<T>`, or override `Filter(IQueryable<T>, GridQuery)` / `Load(GridQuery)`; see [AddressesController.cs](src/GridLab.HandRolled/Controllers/AddressesController.cs) |
| Audit stamps, extra validation | override `OnSaving` / `OnSaved`; see [ProductsController.cs](src/GridLab.HandRolled/Controllers/ProductsController.cs) |
| Appearance | set `--gl-*` variables on `.gl` (accent, borders, radius, padding, font size); every element has a `gl-` class; `.CssClass("…")` on the grid, `.RowClass(row => …)` per row |
| Different markup | copy any partial from `src/GridLab.Grid/Views/Shared/GridLab/` into your app at the same path; the app's copy wins |

### Library layout

```
src/GridLab.Grid/
  GridDefinition.cs        fluent builder → GridSpec / GridColumnSpec
  GridQuery.cs             request state and route-value helper
  GridQueryExtensions.cs   ApplySearch / ApplySort / ApplyPaging over IQueryable<T>
  GridController.cs        Index / Save / Delete with virtual hooks (Load, Filter, OnSaving…)
  IGridStore.cs            IGridStore (editing) + IQueryableGridStore / IPagedGridStore (rows)
  Paging/                  PagedResult, Paginator, PagerModel
  Views/Shared/Grid.cshtml            the page that hosts a grid
  Views/Shared/GridLab/_Grid.cshtml   toolbar and table; _GridRow, _GridEditRow, _GridEditForm, _GridEditor, _GridPager
  wwwroot/gridlab.css      theme via CSS variables
```

## Putting it into an existing app

### Keep your controller and repository

`GridController<T>` is a convenience, not a requirement. If you already have a controller and repository methods, keep them and call the library's pieces directly. A grid needs three actions:

```csharp
private static readonly GridDefinition<User> Grid = GridDefinition.For<User>(g => g
    .EditMode(GridEditMode.Modal)
    .Column(u => u.Role, c => c.Title("Access")));

public ActionResult Index(GridQuery query, int? edit, bool add = false)
{
    var q = Grid.Spec.Normalize(query);                                   // safe sort column, page size, page number
    var page = _users.GetPage(q.Search, q.Sort, q.Desc, q.Page, q.PageSize); // your existing repository method
    var editing = add ? new User() : edit is int id ? _users.GetById(id) : null;
    return View(GridViewModel.Create(Grid,
        new PagedResult<User>(page.Items, page.TotalCount, q.Page - 1, q.PageSize), query, editing));
}

[HttpPost, ValidateAntiForgeryToken]
public ActionResult Save([Bind(Prefix = "Item")] User item, GridQuery query)
{
    if (!ModelState.IsValid) return View("Index", /* same as above, with editing = item */);
    if (item.Id == 0) _users.Create(item); else _users.Update(item);      // your existing methods
    return RedirectToAction("Index", query.ToRouteValues());
}

[HttpPost, ValidateAntiForgeryToken]
public ActionResult Delete(int id, GridQuery query)
{
    _users.Delete(id);
    return RedirectToAction("Index", query.ToRouteValues());
}
```

The view is one line: `@Html.Partial("GridLab/_Grid", Model)`. Editors post fields named `Item.<Property>`, which is why `Save` binds with that prefix. `Normalize` is what keeps the URL's `sort=` from reaching your repository unchecked.

### ASP.NET Core versus ASP.NET MVC 5 on .NET Framework

**It matters, but less than it might look.** This library is an ASP.NET Core Razor Class Library targeting .NET 10. An MVC 5 app on .NET Framework 4.x cannot reference it: the assembly is the wrong target, Razor Class Libraries don't exist in MVC 5, and there are no tag helpers or static web assets. What you can do is copy it in. The dependency surface on ASP.NET Core is deliberately tiny and this is exactly what touches it:

| File | Depends on | In MVC 5 |
| --- | --- | --- |
| `GridQuery.cs` | `Microsoft.AspNetCore.Routing.RouteValueDictionary` | `System.Web.Routing.RouteValueDictionary`, same API |
| `GridColumnSpec.cs`, `GridDefinition.cs` | `Microsoft.AspNetCore.Html.IHtmlContent` for custom templates | `System.Web.IHtmlString` / `MvcHtmlString` |
| `GridController.cs` | `Microsoft.AspNetCore.Mvc.Controller`, `IActionResult`, `[Bind(Prefix)]`, `[ValidateAntiForgeryToken]` | `System.Web.Mvc.Controller`, `ActionResult`; the attributes exist with the same names. Or skip this file and use your own controller as above |
| `GridSpec.cs`, `GridQueryExtensions.cs`, `GridViewModel.cs`, `GridEnums.cs`, `IGridStore.cs`, `Paging/*` | nothing framework-specific | copy as-is |
| `Views/Shared/GridLab/*.cshtml` (6 partials) | `<partial name="…" model="…" />` tag helper, `ViewContext.ModelState`, `Html.AntiForgeryToken()`, `Url.Action(...)` | `@Html.Partial("…", model)`, `ViewData.ModelState`; the other two are identical. Views live in `Views/Shared/GridLab/` of the app instead of the library |
| `wwwroot/gridlab.css` | static web assets (`_content/…`) | drop it in `Content/` and link it normally |

Two language notes for the C# side. The library uses C# 12 (primary constructors, collection expressions, `required` and `init` members, records). None of that depends on the runtime: an SDK-style project targeting `net48` with a current compiler builds it, with the `PolySharp` package supplying the handful of attributes .NET Framework lacks for `required`/`init`. If the app is still an old-style `.csproj` compiled by Visual Studio, you get the same compiler, so this is mostly a question of which `LangVersion` the project allows. Razor views in MVC 5 are compiled at runtime by the `Microsoft.CodeDom.Providers.DotNetCompilerPlatform` package, whose C# level lags the SDK, so keep view code simple. The two partials that use property patterns and tuple deconstruction (`_Grid.cshtml`, `_GridRow.cshtml`) want a line or two rewritten; everything else in the views is Razor that MVC 5 already understands.

What behaves identically: model binding of `GridQuery` from the query string, `Item.*` prefix binding, `ModelState` validation from DataAnnotations, antiforgery, `RedirectToAction` with route values, and the URL-state design. What you lose: nothing functional. The delete confirm is the only JavaScript, and it's inline.

Realistically the port is a copy of about seven C# files with three type substitutions, six partials with `<partial>` swapped for `@Html.Partial`, and one stylesheet. Half a day for someone who knows the app, and afterwards the files are yours to modify, which fits the intent of this project better than a package dependency would anyway. The caveat: this port has been read through, not compiled on .NET Framework, so expect small compiler nudges rather than design problems.

If a move to ASP.NET Core is already planned, the calculation changes: reference the library as-is and use the controller pattern above against your existing repository methods.
