using Core.Abstractions;
using Core.Domain;

namespace Core.Services;

public sealed class LendingService(IBookStore store)
{
    private readonly IBookStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public BookCopy AddBook(string id, string isbn, string title)
    {
        var copy = BookCopy.Create(id, isbn, title);
        _store.Add(copy);
        return copy;
    }

    public void IssueBook(string id)
    {
        var book = _store.GetById(id)
            ?? throw new InvalidOperationException($"Книгу з id={id} не знайдено.");

        book.Issue();
        _store.Update(book);
    }

    public void ReturnBook(string id)
    {
        var book = _store.GetById(id)
            ?? throw new InvalidOperationException($"Книгу з id={id} не знайдено.");

        book.Return();
        _store.Update(book);
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);
}