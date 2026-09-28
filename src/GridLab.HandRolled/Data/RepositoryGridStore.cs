using GridLab.Grid;
using GridLab.Shared.Data;
using GridLab.Shared.Models;

namespace GridLab.HandRolled.Data;

/// <summary>Adapts the shared in-memory repository to the grid's store contract. An EF Core version would be DbSet-backed and just as short.</summary>
public sealed class RepositoryGridStore<T>(IRepository<T> repository) : IQueryableGridStore<T> where T : class, IEntity
{
    public IQueryable<T> Query() => repository.GetAll().AsQueryable();
    public T? Find(string key) => int.TryParse(key, out var id) ? repository.Find(id) : null;
    public void Add(T item) => repository.Add(item);
    public void Update(T item) => repository.Update(item);
    public bool Delete(string key) => int.TryParse(key, out var id) && repository.Delete(id);
}
