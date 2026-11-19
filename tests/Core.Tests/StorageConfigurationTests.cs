using System.Text.Json;
using Core.Configuration;
using Core.Domain;
using Core.Import;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class StorageConfigurationTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "crossapp-lab09-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void StorageOptions_ResolvesRelativePathAgainstExplicitBase()
    {
        var options = new StorageOptions { DataPath = "nested", FileName = "books.json" };
        string path = options.ResolveFullPath(_directory);
        Assert.Equal(Path.Combine(_directory, "nested", "books.json"), path);
        Assert.True(Directory.Exists(Path.GetDirectoryName(path)));
    }

    [Fact]
    public void StorageOptions_PreservesAbsoluteDataDirectory()
    {
        var options = new StorageOptions { DataPath = _directory };
        Assert.Equal(Path.Combine(_directory, "library.json"), options.ResolveFullPath(AppContext.BaseDirectory));
    }

    [Theory]
    [InlineData("")]
    [InlineData("..")]
    [InlineData("../books.json")]
    public void StorageOptions_RejectsInvalidFileName(string name)
    {
        var options = new StorageOptions { FileName = name };
        Assert.Throws<ArgumentException>(() => options.ResolveFullPath(_directory));
    }

    [Fact]
    public async Task JsonRoundTrip_PreservesMetadataCopiesAndIssuedState()
    {
        string path = Path.Combine(_directory, "catalog.json");
        var first = BookCopy.Create("COPY-1", "ISBN-1", "Кобзар", "Тарас Шевченко", 2020);
        var second = BookCopy.Create("COPY-2", "ISBN-1", "Кобзар", "Тарас Шевченко", 2020);
        second.Issue();
        LibraryJson.WriteAtomic(path, [first, second]);

        var loaded = new FileBookStore(path).List();
        Assert.Equal(new[] { first.ToDto(), second.ToDto() }, loaded.Select(book => book.ToDto()));
        Assert.Contains("Кобзар", File.ReadAllText(path));
        Assert.DoesNotContain("\\u", File.ReadAllText(path));
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Single(document.RootElement.EnumerateArray());
        Assert.Equal(2, document.RootElement[0].GetProperty("copies").GetArrayLength());
        Assert.Equal(2, (await new JsonBookImporter().LoadAsync(path)).Count);
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void FileStore_ReadsOldFlatJsonAndWritesNewFormat()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "old.json");
        File.WriteAllText(path, """[{"Id":"OLD","Isbn":"ISBN-1","Title":"Кобзар","IsIssued":true}]""");
        var store = new FileBookStore(path);
        var book = Assert.Single(store.List());
        Assert.True(book.IsIssued);
        Assert.Equal(0, book.Year);
        book.Return();
        store.Update(book);
        Assert.False(Assert.Single(new FileBookStore(path).List()).IsIssued);
        Assert.Contains("\"copies\"", File.ReadAllText(path));
    }

    [Fact]
    public void FileStore_EnrichesOldCopyWithKnownMetadataForSameIsbn()
    {
        string path = Path.Combine(_directory, "catalog.json");
        var store = new FileBookStore(path);
        store.Add(BookCopy.Create("KNOWN", "ISBN-1", "Кобзар", "Тарас Шевченко", 2020));
        store.Add(BookCopy.Create("OLD", "ISBN-1", "Кобзар"));
        Assert.Equal(2020, store.GetById("OLD")!.Year);
        Assert.Equal("Тарас Шевченко", new FileBookStore(path).GetById("OLD")!.Author);
    }

    [Fact]
    public void WriteAtomic_InvalidCollection_PreservesPreviousFile()
    {
        string path = Path.Combine(_directory, "catalog.json");
        var book = BookCopy.Create("COPY-1", "ISBN-1", "Кобзар");
        LibraryJson.WriteAtomic(path, [book]);
        string before = File.ReadAllText(path);
        Assert.Throws<DomainException>(() => LibraryJson.WriteAtomic(path, [book, book]));
        Assert.Equal(before, File.ReadAllText(path));
    }

    [Fact]
    public void FileStore_FailedCommit_DoesNotChangeCacheOrLeaveTemporaryFile()
    {
        string path = Path.Combine(_directory, "occupied");
        Directory.CreateDirectory(path);
        var store = new FileBookStore(path);
        var error = Record.Exception(() => store.Add(BookCopy.Create("COPY-1", "ISBN-1", "Кобзар")));
        Assert.True(error is IOException or UnauthorizedAccessException);
        Assert.Empty(store.List());
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void FileStore_DamagedJson_CanBeRetriedAfterCorrection()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "catalog.json");
        File.WriteAllText(path, "[");
        var store = new FileBookStore(path);
        Assert.ThrowsAny<JsonException>(() => store.List());
        LibraryJson.WriteAtomic(path, [BookCopy.Create("COPY-1", "ISBN-1", "Кобзар")]);
        Assert.Single(store.List());
    }

    [Fact]
    public void LendingService_UsesConfiguredLoanDays()
    {
        var store = new InMemoryBookStore();
        var service = new LendingService(store, loanDays: 21);
        service.AddBook("COPY-1", "ISBN-1", "Кобзар");
        DateOnly? dueDate = null;
        service.Changed += (_, e) => dueDate = e.DueDate;
        service.IssueBook("COPY-1");
        Assert.Equal(DateOnly.FromDateTime(DateTime.Today.AddDays(21)), dueDate);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
