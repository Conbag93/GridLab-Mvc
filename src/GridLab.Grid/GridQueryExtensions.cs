using System.Linq.Expressions;
using GridLab.Grid.Paging;

namespace GridLab.Grid;

/// <summary>Applies a <see cref="GridQuery"/> to an IQueryable. Everything is expression-based so EF Core can translate it.</summary>
public static class GridQueryExtensions
{
    /// <summary>Search + sort + page in one go.</summary>
    public static PagedResult<T> ApplyGrid<T>(this IQueryable<T> source, GridSpec spec, GridQuery query) where T : class =>
        source.ApplySearch(spec, query).ApplySort(spec, query).ApplyPaging(spec, query);

    /// <summary>OR-ed case-insensitive "contains" over the searchable text columns.</summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> source, GridSpec spec, GridQuery query) where T : class
    {
        var term = query.Search?.Trim();
        if (string.IsNullOrEmpty(term)) return source;

        var columns = spec.Columns.Where(c => c.Searchable && c.PropertyType == typeof(string)).ToList();
        if (columns.Count == 0) return source;

        var x = Expression.Parameter(typeof(T), "x");
        var lowered = Expression.Constant(term.ToLowerInvariant());
        var toLower = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
        var contains = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        Expression? body = null;
        foreach (var c in columns)
        {
            var prop = Expression.Property(x, c.Property);
            var test = Expression.AndAlso(
                Expression.NotEqual(prop, Expression.Constant(null, typeof(string))),
                Expression.Call(Expression.Call(prop, toLower), contains, lowered));
            body = body is null ? test : Expression.OrElse(body, test);
        }
        return source.Where(Expression.Lambda<Func<T, bool>>(body!, x));
    }

    /// <summary>Sort by the requested column (validated against the spec), falling back to the default sort; the key is always the tiebreaker so pages are stable.</summary>
    public static IOrderedQueryable<T> ApplySort<T>(this IQueryable<T> source, GridSpec spec, GridQuery query) where T : class
    {
        var column = spec.Columns.FirstOrDefault(c => c.Sortable && c.Name.Equals(query.Sort, StringComparison.OrdinalIgnoreCase));
        var desc = query.Desc;
        if (column is null)
        {
            column = spec.Columns.FirstOrDefault(c => c.Name == spec.DefaultSort);
            desc = spec.DefaultDesc;
        }
        var ordered = column is null
            ? OrderBy(source, spec.Key.Property.Name, desc: false, then: false)
            : OrderBy(source, column.Property.Name, desc, then: false);
        return column is null || column == spec.Key ? ordered : OrderBy(ordered, spec.Key.Property.Name, desc: false, then: true);
    }

    public static PagedResult<T> ApplyPaging<T>(this IQueryable<T> source, GridSpec spec, GridQuery query)
    {
        var size = spec.PageSizes.Contains(query.PageSize) ? query.PageSize : spec.DefaultPageSize;
        var total = source.Count();
        var pageIndex = Math.Clamp(query.Page - 1, 0, Paginator.PageCount(total, size) - 1);
        var items = source.Skip(pageIndex * size).Take(size).ToList();
        return new PagedResult<T>(items, total, pageIndex, size);
    }

    private static IOrderedQueryable<T> OrderBy<T>(IQueryable<T> source, string property, bool desc, bool then)
    {
        var x = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(x, property);
        var lambda = Expression.Lambda(member, x);
        var method = then ? (desc ? "ThenByDescending" : "ThenBy") : (desc ? "OrderByDescending" : "OrderBy");
        var call = Expression.Call(typeof(Queryable), method, [typeof(T), member.Type], source.Expression, Expression.Quote(lambda));
        return (IOrderedQueryable<T>)source.Provider.CreateQuery<T>(call);
    }
}
