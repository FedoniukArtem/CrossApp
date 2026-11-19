using Core.Abstractions;
using Core.Domain;

namespace Core.Storage;

public sealed class FileBookStore(string path) : IBookStore
{
    private Dictionary<string, BookCopy> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _path = Path.GetFullPath(path);
    private bool _loaded;

    private void EnsureLoaded()
    {
        if (_loaded) return;
        var loaded = new Dictionary<string, BookCopy>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(_path))
        {
            foreach (var book in LibraryJson.Read(_path))
            {
                if (!loaded.TryAdd(book.Id, book))
                    throw new DomainException($"Повторюваний ID примірника: {book.Id}");
            }
        }
        _cache = loaded;
        _loaded = true;
    }

    private void Commit(Dictionary<string, BookCopy> next)
    {
        var saved = LibraryJson.WriteAtomic(_path, next.Values);
        _cache = saved.ToDictionary(book => book.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<BookCopy> List()
    {
        EnsureLoaded();
        return _cache.Values.Select(book => BookCopy.FromDto(book.ToDto())).ToList();
    }

    public BookCopy? GetById(string id)
    {
        EnsureLoaded();
        return _cache.TryGetValue(id, out var book) ? BookCopy.FromDto(book.ToDto()) : null;
    }

    public IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate) =>
        List().Where(predicate).ToList();

    public void Add(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        var next = new Dictionary<string, BookCopy>(_cache, StringComparer.OrdinalIgnoreCase);
        if (!next.TryAdd(item.Id, BookCopy.FromDto(item.ToDto())))
            throw new DomainException($"Запис з id={item.Id} вже існує.");
        Commit(next);
    }

    public void Update(BookCopy item)
    {
        ArgumentNullException.ThrowIfNull(item);
        EnsureLoaded();
        var next = new Dictionary<string, BookCopy>(_cache, StringComparer.OrdinalIgnoreCase)
        {
            [item.Id] = BookCopy.FromDto(item.ToDto())
        };
        Commit(next);
    }

    public bool Remove(string id)
    {
        EnsureLoaded();
        var next = new Dictionary<string, BookCopy>(_cache, StringComparer.OrdinalIgnoreCase);
        if (!next.Remove(id)) return false;
        Commit(next);
        return true;
    }
}
