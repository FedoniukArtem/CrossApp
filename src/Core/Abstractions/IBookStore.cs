using Core.Domain;

namespace Core.Abstractions;

public interface IBookStore
{
    IReadOnlyList<BookCopy> List();
    BookCopy? GetById(string id);
    IReadOnlyList<BookCopy> Find(Func<BookCopy, bool> predicate);
    void Add(BookCopy item);
    void Update(BookCopy item);
    bool Remove(string id);
}