using Core.Dto;
using Core.Import;
using Core.Domain;

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


















static void DemonstrateDomain()
{
    Console.WriteLine();
    Console.WriteLine("=== Доменна модель ===");

    var copy = BookCopy.Create(
        "COPY-001",
        "978-617-000-0001",
        "Кобзар");

    var reader = Reader.Create(
        "R-001",
        "Іван Петренко",
        "ivan@gmail.com");

    var service = new LibraryService();

    Console.WriteLine(copy);
    Console.WriteLine(reader);

    var loan = service.IssueCopy(
        copy,
        reader,
        "L-001",
        new DateTime(2026, 10, 10));

    Console.WriteLine();
    Console.WriteLine("Після видачі:");
    Console.WriteLine(copy);
    Console.WriteLine(reader);
    Console.WriteLine(loan);

    Console.WriteLine();
    Console.WriteLine("=== Перевірка інваріантів ===");

    TryDo(
        "Повторна видача",
        () => copy.Issue());

    TryDo(
        "Порожній ISBN",
        () => BookCopy.Create(
            "COPY-002",
            " ",
            "Кобзар"));

    TryDo(
        "Повернення до дати видачі",
        () => loan.Close(
            new DateTime(2026, 10, 9)));

    Console.WriteLine();
    Console.WriteLine("Стан після перевірок:");
    Console.WriteLine(copy);
    Console.WriteLine(reader);
}

static void TryDo(string title, Action action)
{
    try
    {
        action();

        Console.WriteLine(
            $"{title}: помилка не виникла!");
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"{title}: {ex.GetType().Name} — {ex.Message}");
    }
}

DemonstrateDomain();




static void DemonstrateDomainImport()
{
    Console.WriteLine();
    Console.WriteLine("=== Імпорт у доменну модель ===");

    string path = Path.Combine(
        "data",
        "library-mixed.csv");

    if (!File.Exists(path))
    {
        Console.WriteLine(
            $"Файл не знайдено: {path}");

        return;
    }

    ImportResult<LibraryItem> importResult =
        LibraryMixedImporter.Load(path);

    DomainImportResult<DomainLibraryItem> domainResult =
        DomainLibraryImporter.Convert(importResult);

    Console.WriteLine(
        $"Успішно створено сутностей: {domainResult.Items.Count}");

    Console.WriteLine(
        $"Кількість помилок: {domainResult.Errors.Count}");

    foreach (DomainLibraryItem item in domainResult.Items)
    {
        switch (item)
        {
            case DomainBookItem book:
                Console.WriteLine(book.Book);
                break;

            case DomainReaderItem reader:
                Console.WriteLine(reader.Reader);
                break;
        }
    }

    foreach (string error in domainResult.Errors)
    {
        Console.WriteLine($"Помилка: {error}");
    }
}

DemonstrateDomainImport();



static void DemonstrateLoanLimit()
{
    Console.WriteLine();
    Console.WriteLine("=== Перевірка ліміту видач ===");

    var reader = Reader.Create(
        "R-100",
        "Олена Петренко");

    var service = new LibraryService();

    for (int i = 1; i <= 5; i++)
    {
        var copy = BookCopy.Create(
            $"COPY-{i:000}",
            $"ISBN-{i:000}",
            $"Книга {i}");

        service.IssueCopy(
            copy,
            reader,
            $"L-{i:000}",
            new DateTime(2026, 10, 10));
    }

    Console.WriteLine(reader);

    var sixthCopy = BookCopy.Create(
        "COPY-006",
        "ISBN-006",
        "Книга 6");

    TryDo(
        "Спроба взяти шосту книгу",
        () => service.IssueCopy(
            sixthCopy,
            reader,
            "L-006",
            new DateTime(2026, 10, 10)));

    Console.WriteLine(reader);
    Console.WriteLine(sixthCopy);
}


DemonstrateLoanLimit();



static void DemonstrateOrder()
{
    Console.WriteLine();
    Console.WriteLine("=== Перевірка замовлення ===");

    var order = Order.Create(
        "ORD-001",
        "CUSTOMER-001");

    Console.WriteLine(
        $"Початковий стан: {order.Status}");

    TryDo(
        "Підтвердження порожнього замовлення",
        () => order.Confirm());

    order.AddLine(
        "PROD-001",
        "Книга",
        250m,
        2);

    order.AddLine(
        "PROD-002",
        "Зошит",
        40m,
        3);

    Console.WriteLine(
        $"Сума замовлення: {order.Total} грн");

    order.Confirm();

    Console.WriteLine(
        $"Після підтвердження: {order.Status}");

    TryDo(
        "Додавання товару після підтвердження",
        () => order.AddLine(
            "PROD-003",
            "Ручка",
            20m,
            1));

    TryDo(
        "Повторне підтвердження",
        () => order.Confirm());

    order.Cancel();

    Console.WriteLine(
        $"Після скасування: {order.Status}");
}

DemonstrateOrder();


return 0;