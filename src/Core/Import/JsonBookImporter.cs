using System.Runtime.CompilerServices;
using System.Text.Json;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;

namespace Core.Import;

public sealed class JsonBookImporter : IBookImporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<BookCopy>> LoadAsync(string path, CancellationToken ct = default)
    {
        var books = new List<BookCopy>();
        await foreach (var book in StreamAsync(path, ct))
        {
            ct.ThrowIfCancellationRequested();
            books.Add(book);
        }

        return books;
    }

    public async IAsyncEnumerable<BookCopy> StreamAsync(
        string path, [EnumeratorCancellation] CancellationToken ct = default, IProgress<int>? progress = null)
    {
        ct.ThrowIfCancellationRequested();
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 4096, useAsync: true);
        using (var reader = new StreamReader(stream, leaveOpen: true))
        {
            var first = new char[1];
            do
            {
                if (await reader.ReadAsync(first.AsMemory(), ct) == 0)
                    throw new JsonException("JSON-файл порожній.");
            }
            while (char.IsWhiteSpace(first[0]));

            if (first[0] != '[')
                throw new DomainException("Каталог має містити JSON-масив примірників.");
        }
        stream.Position = 0;
        progress?.Report(0);
        int index = 0;
        int lastPercent = 0;

        await foreach (var dto in JsonSerializer.DeserializeAsyncEnumerable<BookCopyDto>(
            stream, Options, ct))
        {
            ct.ThrowIfCancellationRequested();
            index++;
            BookCopy book;
            try
            {
                book = BookCopy.FromDto(dto!);
            }
            catch (ArgumentException ex)
            {
                throw new DomainException($"Запис {index}: {ex.Message}", ex);
            }

            yield return book;

            ct.ThrowIfCancellationRequested();
            // Позиція враховує буфер читання. 100% повідомляємо лише після завершення імпорту.
            int percent = (int)Math.Min(99, stream.Position * 100d / Math.Max(1, stream.Length));
            if (percent != lastPercent)
            {
                progress?.Report(percent);
                lastPercent = percent;
            }
        }

        ct.ThrowIfCancellationRequested();
        progress?.Report(100);
    }
}
