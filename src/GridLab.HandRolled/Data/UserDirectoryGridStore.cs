using GridLab.Grid;
using GridLab.Grid.Paging;
using GridLab.Shared.Data;
using GridLab.Shared.Models;

namespace GridLab.HandRolled.Data;

/// <summary>
/// Plugs an existing paged data-access class into the grid. The grid passes a normalised query
/// (sort already validated against the columns, page size already validated) and takes back a page + total.
/// </summary>
public sealed class UserDirectoryGridStore(UserDirectory directory) : IPagedGridStore<User>
{
    public PagedResult<User> Load(GridQuery query)
    {
        var page = directory.GetPage(query.Search, query.Sort, query.Desc, query.Page, query.PageSize);
        return new PagedResult<User>(page.Items, page.TotalCount, page.PageNumber - 1, page.PageSize);
    }

    public User? Find(string key) => int.TryParse(key, out var id) ? directory.GetById(id) : null;
    public void Add(User item) => directory.Create(item);
    public void Update(User item) => directory.Update(item);
    public bool Delete(string key) => int.TryParse(key, out var id) && directory.Delete(id);
}
