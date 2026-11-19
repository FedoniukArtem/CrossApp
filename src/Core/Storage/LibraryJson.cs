using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

public static class LibraryJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static IReadOnlyList<BookCopy> Read(string path)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new JsonException("Каталог має містити JSON-масив.");
        var result = new List<BookCopy>();
        int index = 0;
        foreach (var record in document.RootElement.EnumerateArray())
            result.AddRange(ReadRecord(record, ++index));
        return result;
    }

    public static IReadOnlyList<BookCopy> ReadRecord(JsonElement record, int index)
    {
        try
        {
            if (record.ValueKind == JsonValueKind.Object &&
                (record.TryGetProperty("copies", out _) || record.TryGetProperty("Copies", out _)))
            {
                var edition = record.Deserialize<LibraryEditionDto>(Options)!;
                ArgumentNullException.ThrowIfNull(edition.Copies);
                return edition.Copies.Select(copy =>
                {
                    ArgumentNullException.ThrowIfNull(copy);
                    if (!string.Equals(copy.Isbn, edition.Isbn, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("ISBN примірника не відповідає виданню.");
                    return BookCopy.FromDto(copy with
                    {
                        Title = edition.Title, Author = edition.Author, Year = edition.Year
                    });
                }).ToList();
            }
            return [BookCopy.FromDto(record.Deserialize<BookCopyDto>(Options)!)];
        }
        catch (ArgumentException ex)
        {
            throw new DomainException($"Запис {index}: {ex.Message}", ex);
        }
    }

    public static IReadOnlyList<BookCopy> WriteAtomic(string path, IEnumerable<BookCopy> books)
    {
        var list = books.ToList();
        if (list.Select(book => book.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != list.Count)
            throw new DomainException("Каталог містить повторювані ID примірників.");
        var editions = list.GroupBy(book => book.Isbn, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var first = group.First();
                var authors = group.Select(book => book.Author).Where(author => !string.IsNullOrWhiteSpace(author)).Distinct().ToList();
                var years = group.Select(book => book.Year).Where(year => year != 0).Distinct().ToList();
                if (group.Any(book => book.Title != first.Title) || authors.Count > 1 || years.Count > 1)
                    throw new DomainException($"Різні дані видання для ISBN {group.Key}.");
                string? author = authors.FirstOrDefault();
                int year = years.FirstOrDefault();
                return new LibraryEditionDto(group.Key, first.Title, author, year,
                    group.Select(book => book.ToDto() with { Author = author, Year = year }).ToList());
            })
            .ToList();
        string fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        string temporaryPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, editions, Options);
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporaryPath, fullPath, overwrite: true);
            return editions.SelectMany(edition => edition.Copies).Select(BookCopy.FromDto).ToList();
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
