using Core.Dto;

namespace Core.Import;

public abstract record LibraryItem;

public sealed record BookItem(BookDto Book) : LibraryItem;

public sealed record ReaderItem(ReaderDto Reader) : LibraryItem;

public static class LibraryMixedImporter
{
    public static ImportResult<LibraryItem> Load(string path)
    {
        var items = new List<LibraryItem>();
        var errors = new List<string>();

        string[] lines = File.ReadAllLines(path);

        for (int i = 0; i < lines.Length; i++)
        {
            int number = i + 1;
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;

            switch (ParseLine(line))
            {
                case ParseOk ok:
                    items.Add(ok.Value);
                    break;

                case ParseFailed failed:
                    errors.Add($"рядок {number}: {failed.Reason}");
                    break;
            }
        }

        return new ImportResult<LibraryItem>(
            items,
            errors);
    }

    private static ParseOutcome ParseLine(string line)
    {
        string[] parts = line.Split(
            ';',
            StringSplitOptions.TrimEntries);

        return parts switch
        {
            ["B", var id, var isbn, var title, var year, var author]
                when int.TryParse(year, out int parsedYear)
                     && parsedYear >= 1450
                     && parsedYear <= DateTime.Now.Year
                => new ParseOk(
                    new BookItem(
                        new BookDto(
                            id,
                            isbn,
                            title,
                            parsedYear,
                            string.IsNullOrWhiteSpace(author)
                                ? null
                                : author))),

            ["R", var id, var name, var email]
                => new ParseOk(
                    new ReaderItem(
                        new ReaderDto(
                            id,
                            name,
                            string.IsNullOrWhiteSpace(email)
                                ? null
                                : email))),

            ["B", ..]
                => new ParseFailed(
                    "неправильний запис книги"),

            ["R", ..]
                => new ParseFailed(
                    "неправильний запис читача"),

            _
                => new ParseFailed(
                    "невідомий тип запису")
        };
    }

    private abstract record ParseOutcome;

    private sealed record ParseOk(
        LibraryItem Value) : ParseOutcome;

    private sealed record ParseFailed(
        string Reason) : ParseOutcome;
}