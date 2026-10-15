using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class CachingBookStore(IBookStore innerStore) : IBookStore
{
    private readonly IBookStore _inner = innerStore ?? throw new ArgumentNullException(nameof(innerStore));
    private List<BookCopy>? _cache;

    public IReadOnlyList<BookCopy> List()
    {
        _cache ??= _inner.List().ToList();
        return _cache.AsReadOnly();
    }

    public BookCopy? GetById(string id)
    {
        return List().FirstOrDefault(b => b.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate)
    {
        return List().Where(predicate).ToList();
    }

    public void Add(BookCopy item)
    {
        _inner.Add(item);
        _cache = null;
    }

    public void Update(BookCopy item)
    {
        _inner.Update(item);
        _cache = null;
    }

    public bool Remove(string id)
    {
        bool removed = _inner.Remove(id);
        if (removed) _cache = null;
        return removed;
    }
}