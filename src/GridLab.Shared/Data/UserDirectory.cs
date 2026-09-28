using GridLab.Shared.Models;

namespace GridLab.Shared.Data;

/// <summary>The shape most existing data layers return: one page of items plus the total.</summary>
public sealed record PagedList<T>(IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize);

/// <summary>
/// Stands in for an existing data-access class with "standard" paging: it takes a page number and size,
/// does its own searching and sorting with LINQ, and returns the page with the total count.
/// Nothing in here knows about the grid.
/// </summary>
public sealed class UserDirectory(IRepository<User> repository)
{
    private static readonly Dictionary<string, Func<User, object?>> SortKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(User.Id)] = u => u.Id,
        [nameof(User.FirstName)] = u => u.FirstName,
        [nameof(User.LastName)] = u => u.LastName,
        [nameof(User.Email)] = u => u.Email,
        [nameof(User.Role)] = u => u.Role,
        [nameof(User.IsActive)] = u => u.IsActive,
        [nameof(User.CreatedOn)] = u => u.CreatedOn,
        [nameof(User.LastLogin)] = u => u.LastLogin,
    };

    public PagedList<User> GetPage(string? search, string? sortBy, bool descending, int pageNumber, int pageSize)
    {
        IEnumerable<User> users = repository.GetAll();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            users = users.Where(u =>
                u.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                u.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var key = SortKeys.GetValueOrDefault(sortBy ?? "", SortKeys[nameof(User.LastName)]);
        users = descending ? users.OrderByDescending(key).ThenBy(u => u.Id) : users.OrderBy(key).ThenBy(u => u.Id);

        var list = users.ToList();
        var items = list.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        return new PagedList<User>(items, list.Count, pageNumber, pageSize);
    }

    public User? GetById(int id) => repository.Find(id);
    public User Create(User user) => repository.Add(user);
    public bool Update(User user) => repository.Update(user);
    public bool Delete(int id) => repository.Delete(id);
}
