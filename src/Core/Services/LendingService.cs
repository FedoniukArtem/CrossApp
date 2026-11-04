using Core.Abstractions;
using Core.Domain;
using Core.Events;

namespace Core.Services;

public sealed class LendingService
{
    private readonly IBookStore _store;
    private readonly IReadOnlyList<INotificationSink> _sinks;

    public LendingService(
        IBookStore store,
        IEnumerable<INotificationSink>? sinks = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _sinks = (sinks ?? []).ToList();
    }

    public event EventHandler<CatalogChangedEventArgs>? Changed;

    private void OnChanged(
        CatalogChangeKind kind,
        BookCopy copy,
        string? readerId = null,
        DateOnly? dueDate = null)
    {
        var notification = new CatalogChangedEventArgs(
            kind,
            copy.Id,
            readerId,
            dueDate,
            copy.Title);

        Changed?.Invoke(this, notification);

        foreach (var sink in _sinks)
        {
            sink.Notify(notification);
        }
    }

    public BookCopy AddBook(string id, string isbn, string title)
    {
        var copy = BookCopy.Create(id, isbn, title);

        _store.Add(copy);

        OnChanged(CatalogChangeKind.BookAdded, copy);

        return copy;
    }

    public void IssueBook(
        string id,
        string readerId,
        DateOnly dueDate)
    {
        var book = _store.GetById(id)
            ?? throw new InvalidOperationException(
                $"Книгу з id={id} не знайдено.");

        book.Issue();

        _store.Update(book);

        OnChanged(
            CatalogChangeKind.CopyIssued,
            book,
            readerId,
            dueDate);
    }

    public void IssueBook(string id)
    {
        IssueBook(
            id,
            "Не вказано",
            DateOnly.FromDateTime(DateTime.Today.AddDays(14)));
    }

    public void ReturnBook(string id, string readerId)
    {
        var book = _store.GetById(id)
            ?? throw new InvalidOperationException(
                $"Книгу з id={id} не знайдено.");

        book.Return();

        _store.Update(book);

        OnChanged(
            CatalogChangeKind.CopyReturned,
            book,
            readerId);
    }

    public void ReturnBook(string id)
    {
        ReturnBook(id, "Не вказано");
    }

    public IReadOnlyList<BookCopy> All() => _store.List();

    public BookCopy? Find(string id) => _store.GetById(id);

    public IReadOnlyList<BookCopy> FindAll(
        Func<BookCopy, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return _store.List()
            .Where(predicate)
            .ToList();
    }
}