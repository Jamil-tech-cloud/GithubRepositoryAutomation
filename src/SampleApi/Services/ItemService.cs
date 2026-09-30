using System.Collections.Concurrent;
using SampleApi.Models;

namespace SampleApi.Services;

/// <summary>In-memory item store. Sample domain for demo purposes only — not persisted.</summary>
public class ItemService
{
    private readonly ConcurrentDictionary<int, Item> _items = new();
    private int _nextId = 1;

    public ItemService()
    {
        Add("Write POC docs");
        Add("Demo the workflow");
    }

    public IReadOnlyCollection<Item> GetAll() => _items.Values.OrderBy(i => i.Id).ToList();

    public Item? GetById(int id) => _items.GetValueOrDefault(id);

    public Item Add(string name)
    {
        var id = Interlocked.Increment(ref _nextId) - 1;
        var item = new Item(id, name, Done: false);
        _items[id] = item;
        return item;
    }

    public Item? Complete(int id)
    {
        if (!_items.TryGetValue(id, out var existing))
        {
            return null;
        }

        var updated = existing with { Done = true };
        _items[id] = updated;
        return updated;
    }
}
