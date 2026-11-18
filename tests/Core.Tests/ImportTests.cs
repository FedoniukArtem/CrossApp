using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Import;
using Core.Services;
using Core.Storage;
using Xunit;

namespace Core.Tests;

public sealed class ImportTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_SkipsExistingIdsAndCanBeRepeated(bool streaming)
    {
        var service = new LendingService(new InMemoryBookStore());
        service.AddBook("COPY-1", "ISBN-1", "Збережена назва");
        var importer = new MemoryImporter();
        int events = 0;
        service.Changed += (_, _) => events++;

        int first = await Import(service, importer, streaming);
        int second = await Import(service, importer, streaming);

        Assert.Equal(1, first);
        Assert.Equal(0, second);
        Assert.Equal(1, events);
        Assert.Equal(2, service.All().Count);
        Assert.Equal("Збережена назва", service.Find("COPY-1")!.Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_AlreadyCancelled_DoesNotReadOrModifyStore(bool streaming)
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var service = new LendingService(new InMemoryBookStore());
        var importer = new MemoryImporter();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Import(service, importer, streaming, cts.Token));

        Assert.False(importer.WasCalled);
        Assert.Empty(service.All());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => new JsonBookImporter().LoadAsync("missing.json", cts.Token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Import_CancelledAfterFirstItem_KeepsCommittedItemAndCanResume(bool streaming)
    {
        using var cts = new CancellationTokenSource();
        var service = new LendingService(new InMemoryBookStore());
        var importer = new MemoryImporter();
        int events = 0;
        service.Changed += (_, _) =>
        {
            events++;
            cts.Cancel();
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Import(service, importer, streaming, cts.Token));
        Assert.Single(service.All());
        Assert.Equal(1, events);

        Assert.Equal(1, await Import(service, importer, streaming));
        Assert.Equal(2, service.All().Count);
        Assert.Equal(2, events);
    }

    [Fact]
    public async Task Import_WithLinkedTokens_IsCancelledByTimeoutOnly()
    {
        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        var importer = new BlockingImporter();
        var service = new LendingService(new InMemoryBookStore());
        var operation = service.ImportAsync(importer, "unused.json", linkedCts.Token);
        await importer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(20));

        var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => operation.WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.False(userCts.IsCancellationRequested);
        Assert.True(timeoutCts.IsCancellationRequested);
        Assert.True(linkedCts.IsCancellationRequested);
        Assert.Equal(linkedCts.Token, error.CancellationToken);
        Assert.Empty(service.All());
    }

    [Fact]
    public async Task Import_ReportsProgressAndPassesTokenToImporter()
    {
        using var cts = new CancellationTokenSource();
        var service = new LendingService(new InMemoryBookStore());
        var importer = new MemoryImporter();
        var progress = new CapturedProgress();

        Assert.Equal(2, await service.ImportAsync(importer, "unused.json", cts.Token, progress));

        Assert.Equal(cts.Token, importer.LastToken);
        Assert.Equal(0, progress.Values[0]);
        Assert.Equal(100, progress.Values[^1]);
        Assert.Contains(50, progress.Values);
    }

    [Theory]
    [InlineData("valid")]
    [InlineData("invalid-record")]
    [InlineData("invalid-json")]
    [InlineData("null-record")]
    [InlineData("null-root")]
    [InlineData("empty")]
    public async Task JsonImporter_WithTemporaryFile_ValidatesRecordsAndPreservesErrorType(string scenario)
    {
        string first = """{"Id":"COPY-1","Isbn":"ISBN-1","Title":"Кобзар","IsIssued":true}""";
        string json = scenario switch
        {
            "valid" => $"[{first}]",
            "invalid-record" => $$"""[{{first}},{"Id":"COPY-2","Isbn":"ISBN-2","Title":""}]""",
            "null-record" => $"[{first},null]",
            "invalid-json" => $"[{first},",
            "null-root" => "null",
            _ => "[]"
        };
        string path = Path.Combine(Path.GetTempPath(), $"crossapp-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, json);
        try
        {
            var importer = new JsonBookImporter();
            var service = new LendingService(new InMemoryBookStore());
            if (scenario is "invalid-record" or "null-record")
            {
                var error = await Assert.ThrowsAsync<DomainException>(() => service.ImportAsync(importer, path));
                Assert.Contains("Запис 2", error.Message);
                Assert.IsAssignableFrom<ArgumentException>(error.InnerException);
                Assert.Empty(service.All());
            }
            else if (scenario == "null-root")
            {
                await Assert.ThrowsAsync<DomainException>(() => importer.LoadAsync(path));
            }
            else if (scenario == "invalid-json")
            {
                await Assert.ThrowsAsync<JsonException>(() => importer.LoadAsync(path));
            }
            else
            {
                var progress = new CapturedProgress();
                int count = await service.ImportStreamAsync(importer, path, progress: progress);
                Assert.Equal(scenario == "empty" ? 0 : 1, count);
                Assert.Equal(100, progress.Values[^1]);
                if (scenario == "valid")
                    Assert.True(Assert.Single(service.All()).IsIssued);
            }
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Task<int> Import(
        LendingService service, IBookImporter importer, bool streaming, CancellationToken ct = default) =>
        streaming
            ? service.ImportStreamAsync(importer, "unused.json", ct)
            : service.ImportAsync(importer, "unused.json", ct);

    private sealed class CapturedProgress : IProgress<int>
    {
        public List<int> Values { get; } = [];
        public void Report(int value) => Values.Add(value);
    }

    private sealed class MemoryImporter : IBookImporter
    {
        public bool WasCalled { get; private set; }
        public CancellationToken LastToken { get; private set; }

        public Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
        {
            WasCalled = true;
            LastToken = ct;
            ct.ThrowIfCancellationRequested();
            IReadOnlyList<BookCopy> books =
            [
                BookCopy.Create("COPY-1", "ISBN-1", "Кобзар"),
                BookCopy.Create("COPY-2", "ISBN-2", "Місто")
            ];
            return Task.FromResult(books);
        }

        public async IAsyncEnumerable<BookCopy> StreamAsync(
            string path, [EnumeratorCancellation] CancellationToken ct = default, IProgress<int>? progress = null)
        {
            foreach (var book in await LoadAsync(path, ct))
            {
                ct.ThrowIfCancellationRequested();
                yield return book;
            }
        }
    }

    private sealed class BlockingImporter : IBookImporter
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return [];
        }

        public IAsyncEnumerable<BookCopy> StreamAsync(
            string path, CancellationToken ct = default, IProgress<int>? progress = null) =>
            throw new NotSupportedException();
    }
}
