using Core.Domain;
using Core.Events;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class LendingServiceTests
{
    [Fact]
    public void ReturnBook_WhenNotIssued_ThrowsAndKeepsState()
    {
        var service = new LendingService(new InMemoryBookStore());
        var book = service.AddBook("COPY-1", "ISBN-1", "Кобзар");
        int events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<DomainException>(() => service.ReturnBook(book.Id));

        Assert.False(book.IsIssued);
        Assert.Equal(0, events);
    }

    [Fact]
    public void IssueBook_Twice_ThrowsAndKeepsState()
    {
        var service = new LendingService(new InMemoryBookStore());
        var book = service.AddBook("COPY-1", "ISBN-1", "Кобзар");
        service.IssueBook(book.Id);
        int events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<DomainException>(() => service.IssueBook(book.Id));

        Assert.True(book.IsIssued);
        Assert.Equal(0, events);
    }

    [Fact]
    public void AddBook_RaisesChangedExactlyOnceAfterSaving()
    {
        var service = new LendingService(new InMemoryBookStore());
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) =>
        {
            Assert.NotNull(service.Find(e.CopyId));
            events.Add(e);
        };

        var book = service.AddBook("COPY-1", "ISBN-1", "Кобзар");

        var change = Assert.Single(events);
        Assert.Equal(CatalogChangeKind.BookAdded, change.Kind);
        Assert.Equal(book.Id, change.CopyId);
        Assert.Equal(book.Title, change.Title);
    }

    [Theory]
    [InlineData("", "ISBN-1", "Кобзар")]
    [InlineData("COPY-1", "", "Кобзар")]
    [InlineData("COPY-1", "ISBN-1", " ")]
    public void AddBook_InvalidArgument_DoesNotSaveOrNotify(string id, string isbn, string title)
    {
        var service = new LendingService(new InMemoryBookStore());
        int events = 0;
        service.Changed += (_, _) => events++;

        Assert.Throws<ArgumentException>(() => service.AddBook(id, isbn, title));

        Assert.Empty(service.All());
        Assert.Equal(0, events);
    }

    [Fact]
    public void IssueAndReturn_PublishReaderAndDueDate()
    {
        var service = new LendingService(new InMemoryBookStore());
        var book = service.AddBook("COPY-1", "ISBN-1", "Кобзар");
        var events = new List<CatalogChangedEventArgs>();
        service.Changed += (_, e) => events.Add(e);
        var dueDate = new DateOnly(2026, 12, 1);

        service.IssueBook(book.Id, "R-1", dueDate);
        service.ReturnBook(book.Id, "R-1");

        Assert.Equal(2, events.Count);
        Assert.Equal(CatalogChangeKind.CopyIssued, events[0].Kind);
        Assert.Equal("R-1", events[0].ReaderId);
        Assert.Equal(dueDate, events[0].DueDate);
        Assert.Equal(CatalogChangeKind.CopyReturned, events[1].Kind);
        Assert.False(book.IsIssued);
    }
}
