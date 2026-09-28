using System.Collections.Concurrent;
using GridLab.Shared.Models;

namespace GridLab.Shared.Data;

/// <summary>Thread-safe in-memory store; stands in for a database so the web projects stay trim.</summary>
public sealed class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly ConcurrentDictionary<int, T> _items = new();
    private int _nextId;

    public InMemoryRepository(IEnumerable<T> seed)
    {
        foreach (var item in seed)
        {
            _items[item.Id] = item;
            _nextId = Math.Max(_nextId, item.Id);
        }
    }

    public IReadOnlyList<T> GetAll() => _items.Values.OrderBy(i => i.Id).ToList();

    public T? Find(int id) => _items.GetValueOrDefault(id);

    public T Add(T item)
    {
        item.Id = Interlocked.Increment(ref _nextId);
        _items[item.Id] = item;
        return item;
    }

    public bool Update(T item)
    {
        if (!_items.ContainsKey(item.Id)) return false;
        _items[item.Id] = item;
        return true;
    }

    public bool Delete(int id) => _items.TryRemove(id, out _);
}
