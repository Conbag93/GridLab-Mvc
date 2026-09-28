using GridLab.Grid.Paging;

namespace GridLab.Grid;

/// <summary>
/// The persistence contract a grid needs for editing. Keys cross this boundary as strings (they come from the URL); the store converts.
/// Implement one of the two derived interfaces to supply rows:
/// <see cref="IQueryableGridStore{T}"/> when you can expose an IQueryable and let the grid search/sort/page it, or
/// <see cref="IPagedGridStore{T}"/> when your data layer already has a "give me page N of size M" method.
/// </summary>
public interface IGridStore<T> where T : class
{
    T? Find(string key);
    void Add(T item);
    void Update(T item);
    bool Delete(string key);
}

/// <summary>Rows come from an IQueryable (EF Core DbSet, in-memory list); the grid applies search, sort and paging as LINQ expressions.</summary>
public interface IQueryableGridStore<T> : IGridStore<T> where T : class
{
    IQueryable<T> Query();
}

/// <summary>Rows come from your own paged data-access method. The grid hands over a normalised query (validated sort column, page size and page number) and expects one page back with the total count.</summary>
public interface IPagedGridStore<T> : IGridStore<T> where T : class
{
    PagedResult<T> Load(GridQuery query);
}
