using GridLab.Shared.Models;

namespace GridLab.Shared.Data;

public interface IRepository<T> where T : class, IEntity
{
    IReadOnlyList<T> GetAll();
    T? Find(int id);
    T Add(T item);
    bool Update(T item);
    bool Delete(int id);
}
