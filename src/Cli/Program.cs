using Core.Dto;
using Core.Import;

Console.OutputEncoding = System.Text.Encoding.UTF8;

string path = args.Length > 0
    ? args[0]
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine(
        $"Файл не знайдено: {Path.GetFullPath(path)}");

    return 1;
}

ImportResult<BookDto> result =
    Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => BookCsvImporter.Load(path),
        ".json" => BookJsonImporter.Load(path),

        _ => new ImportResult<BookDto>(
            [],
            [$"Непідтримуваний формат файлу: {Path.GetExtension(path)}"])
    };

Console.WriteLine(
    $"Завантажено книг: {result.Items.Count}");

Console.WriteLine();
Console.WriteLine("Перші книги:");

foreach (BookDto book in result.Items.Take(5))
{
    Console.WriteLine(
        $" {book.Id,-6} " +
        $"{book.Isbn,-20} " +
        $"{book.Title,-26} " +
        $"{book.Year,5} " +
        $"{book.Author}");
}

if (result.Errors.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine(
        $"Пропущено рядків: {result.Errors.Count}");

    foreach (string error in result.Errors)
    {
        Console.WriteLine($" ! {error}");
    }
}


int total = result.Items.Count + result.Errors.Count;
int accepted = result.Items.Count;
int skipped = result.Errors.Count;

double errorPercent = total == 0
    ? 0
    : skipped * 100.0 / total;

Console.WriteLine();
Console.WriteLine(
    $"Усього: {total} | " +
    $"Прийнято: {accepted} | " +
    $"Пропущено: {skipped} | " +
    $"Помилки: {errorPercent:F1}%");








Console.WriteLine();
Console.WriteLine("Різнорідні записи");

string mixedPath = Path.Combine("data", "library-mixed.csv");

if (!File.Exists(mixedPath))
{
    Console.WriteLine($"Файл не знайдено: {mixedPath}");
}
else
{
    ImportResult<LibraryItem> mixedResult =
        LibraryMixedImporter.Load(mixedPath);

    Console.WriteLine(
        $"Завантажено різнорідних записів: {mixedResult.Items.Count}");

    foreach (LibraryItem item in mixedResult.Items)
    {
        switch (item)
        {
            case BookItem book:
                Console.WriteLine(
                    $"Книга: {book.Book.Title} ({book.Book.Year})");
                break;

            case ReaderItem reader:
                Console.WriteLine(
                    $"Читач: {reader.Reader.Name} " +
                    $"({reader.Reader.Email})");
                break;
        }
    }

    if (mixedResult.Errors.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Помилки:");

        foreach (string error in mixedResult.Errors)
        {
            Console.WriteLine($" ! {error}");
        }
    }
}


return 0;