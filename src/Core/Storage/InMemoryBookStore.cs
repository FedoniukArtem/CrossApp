using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class InMemoryBookStore(IEnumerable<BookCopy>? seed = null) : IBookStore
{
    private readonly Dictionary<string, BookCopy> _items =
        (seed ?? []).ToDictionary(b => b.Id, StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<BookCopy> List() => _items.Values.ToList();

    public BookCopy? GetById(string id) => _items.GetValueOrDefault(id);

    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) =>
        List().Where(predicate).ToList();

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (_items.ContainsKey(item.Id))
            throw new InvalidOperationException($"Запис з id={item.Id} вже існує.");

        _items.Add(item.Id, item);
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _items[item.Id] = item;
    }

    public bool Remove(string id) => _items.Remove(id);
}