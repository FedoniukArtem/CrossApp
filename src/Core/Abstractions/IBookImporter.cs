using Core.Domain;

namespace Core.Abstractions;

public interface IBookImporter
{
    Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default);

    IAsyncEnumerable<BookCopy> StreamAsync(
        string path, CancellationToken ct = default, IProgress<int>? progress = null);
}
