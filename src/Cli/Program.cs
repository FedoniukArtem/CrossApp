using Core;
using Core.Abstractions;
using Core.Domain;
using Core.Dto;
using Core.Import;
using Core.Services;
using Core.Storage;
using Core.Events;
using Notifications;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ==========================================
// 1. ОСНОВНИЙ ВЕРХНЬОРІВНЕВИЙ КОД (ЕКЗЕКУЦІЯ)
// ==========================================

// Шукаємо перший аргумент, який НЕ є прапорцем (тобто не починається з '--')
string? customFilePath = args.FirstOrDefault(a => !a.StartsWith("--"));

string path = !string.IsNullOrEmpty(customFilePath)
    ? customFilePath
    : Path.Combine("data", "sample.csv");

if (!File.Exists(path))
{
    Console.WriteLine($"Файл не знайдено: {Path.GetFullPath(path)}");
    return 1;
}

ImportResult<BookDto> result =
    Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".csv" => BookCsvImporter.Load(path),
        ".json" => BookJsonImporter.Load(path),
        _ => new ImportResult<BookDto>([], [$"Непідтримуваний формат файлу: {Path.GetExtension(path)}"])
    };

Console.WriteLine($"Завантажено книг: {result.Items.Count}");
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
    Console.WriteLine($"Пропущено рядків: {result.Errors.Count}");

    foreach (string error in result.Errors)
    {
        Console.WriteLine($" ! {error}");
    }
}

int total = result.Items.Count + result.Errors.Count;
int accepted = result.Items.Count;
int skipped = result.Errors.Count;

double errorPercent = total == 0 ? 0 : skipped * 100.0 / total;

Console.WriteLine();
Console.WriteLine($"Усього: {total} | Прийнято: {accepted} | Пропущено: {skipped} | Помилки: {errorPercent:F1}%");

Console.WriteLine();
Console.WriteLine("Різнорідні записи");

string mixedPath = Path.Combine("data", "library-mixed.csv");

if (!File.Exists(mixedPath))
{
    Console.WriteLine($"Файл не знайдено: {mixedPath}");
}
else
{
    ImportResult<LibraryItem> mixedResult = LibraryMixedImporter.Load(mixedPath);

    Console.WriteLine($"Завантажено різнорідних записів: {mixedResult.Items.Count}");

    foreach (LibraryItem item in mixedResult.Items)
    {
        switch (item)
        {
            case BookItem book:
                Console.WriteLine($"Книга: {book.Book.Title} ({book.Book.Year})");
                break;

            case ReaderItem reader:
                Console.WriteLine($"Читач: {reader.Reader.Name} ({reader.Reader.Email})");
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

// Послідовний виклик усіх демонстраційних сценаріїв
DemonstrateDomain();
DemonstrateDomainImport();
DemonstrateLoanLimit();
DemonstrateOrder();
//DemonstrateLab5(args);



DemonstrateLab6();

return 0;


// ==========================================
// 2. ЛОКАЛЬНІ ФУНКЦІЇ ДЕМОНСТРАЦІЇ
// ==========================================

static void DemonstrateDomain()
{
    Console.WriteLine();
    Console.WriteLine("=== Доменна модель (Лаб 4) ===");

    var copy = BookCopy.Create("COPY-001", "978-617-000-0001", "Кобзар");
    var reader = Reader.Create("R-001", "Іван Петренко", "ivan@gmail.com");
    var service = new LibraryService();

    Console.WriteLine(copy);
    Console.WriteLine(reader);

    var loan = service.IssueCopy(copy, reader, "L-001", new DateTime(2026, 10, 10));

    Console.WriteLine();
    Console.WriteLine("Після видачі:");
    Console.WriteLine(copy);
    Console.WriteLine(reader);
    Console.WriteLine(loan);

    Console.WriteLine();
    Console.WriteLine("=== Перевірка інваріантів ===");

    TryDo("Повторна видача", () => copy.Issue());
    TryDo("Порожній ISBN", () => BookCopy.Create("COPY-002", " ", "Кобзар"));
    TryDo("Повернення до дати видачі", () => loan.Close(new DateTime(2026, 10, 9)));

    Console.WriteLine();
    Console.WriteLine("Стан після перевірок:");
    Console.WriteLine(copy);
    Console.WriteLine(reader);
}

static void DemonstrateDomainImport()
{
    Console.WriteLine();
    Console.WriteLine("=== Дод. завдання 1: Імпорт у доменну модель ===");

    string path = Path.Combine("data", "library-mixed.csv");

    if (!File.Exists(path))
    {
        Console.WriteLine($"Файл не знайдено: {path}");
        return;
    }

    ImportResult<LibraryItem> importResult = LibraryMixedImporter.Load(path);
    DomainImportResult<DomainLibraryItem> domainResult = DomainLibraryImporter.Convert(importResult);

    Console.WriteLine($"Успішно створено сутностей: {domainResult.Items.Count}");
    Console.WriteLine($"Кількість помилок: {domainResult.Errors.Count}");

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

static void DemonstrateLoanLimit()
{
    Console.WriteLine();
    Console.WriteLine("=== Дод. завдання 2: Перевірка ліміту видач ===");

    var reader = Reader.Create("R-100", "Олена Петренко");
    var service = new LibraryService();

    for (int i = 1; i <= 5; i++)
    {
        var copy = BookCopy.Create($"COPY-{i:000}", $"ISBN-{i:000}", $"Книга {i}");
        service.IssueCopy(copy, reader, $"L-{i:000}", new DateTime(2026, 10, 10));
    }

    Console.WriteLine(reader);

    var sixthCopy = BookCopy.Create("COPY-006", "ISBN-006", "Книга 6");

    TryDo("Спроба взяти шосту книгу", () => service.IssueCopy(sixthCopy, reader, "L-006", new DateTime(2026, 10, 10)));

    Console.WriteLine(reader);
    Console.WriteLine(sixthCopy);
}

static void DemonstrateOrder()
{
    Console.WriteLine();
    Console.WriteLine("=== Дод. завдання 3: Перевірка замовлення та станів ===");

    var order = Order.Create("ORD-001", "CUSTOMER-001");

    Console.WriteLine($"Початковий стан: {order.Status}");

    TryDo("Підтвердження порожнього замовлення", () => order.Confirm());

    order.AddLine("PROD-001", "Книга", 250m, 2);
    order.AddLine("PROD-002", "Зошит", 40m, 3);

    Console.WriteLine($"Сума замовлення: {order.Total} грн");

    order.Confirm();

    Console.WriteLine($"Після підтвердження: {order.Status}");

    TryDo("Додавання товару після підтвердження", () => order.AddLine("PROD-003", "Ручка", 20m, 1));
    TryDo("Повторне підтвердження", () => order.Confirm());

    order.Cancel();

    Console.WriteLine($"Після скасування: {order.Status}");
}

static void DemonstrateLab5(string[] args)
{
    Console.WriteLine();
    Console.WriteLine("=== Лабораторна робота №5 (Сервісний шар та сховища) ===");

    // Створення сховища через StoreFactory та обгортання в CachingBookStore
    IBookStore store = StoreFactory.Create(args);
    var service = new LendingService(store);

    Console.WriteLine($"Використовується сховище (Composition Root): {store.GetType().Name}");

    try
    {
        var newBook = service.AddBook($"COPY-10{service.All().Count + 1}", "978-617-111-0000", "Енеїда");
        Console.WriteLine($"Додано нову книгу: {newBook}");

        service.IssueBook(newBook.Id);
        Console.WriteLine($"Стан після видачі у сховищі: {service.Find(newBook.Id)}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка сховища: {ex.Message}");
    }

    // Демонстрація пошуку через Func<BookCopy, bool> предикат
    Console.WriteLine();
    Console.WriteLine("Пошук доступних книг (предикат Func<BookCopy, bool>):");
    var availableBooks = store.Find(b => !b.IsIssued);
    foreach (var b in availableBooks)
    {
        Console.WriteLine($" [Доступна] {b}");
    }

    Console.WriteLine();
    Console.WriteLine("Поточний повний список книг у сховищі:");
    foreach (var b in service.All())
    {
        Console.WriteLine($" - {b}");
    }
}

static void TryDo(string title, Action action)
{
    try
    {
        action();
        Console.WriteLine($"{title}: помилка не виникла!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{title}: {ex.GetType().Name} — {ex.Message}");
    }
}


static void DemonstrateLab6()
{
    Console.WriteLine();
    Console.WriteLine("=== Лабораторна робота №6: Домeнні події ===");

    string logPath = Path.Combine(
        AppContext.BaseDirectory,
        "logs",
        "app.log");

    Directory.CreateDirectory(
        Path.GetDirectoryName(logPath)!);

    var store = new InMemoryBookStore();
    var service = new LendingService(store);

    // Перший підписник: консоль і файл
    void OnCatalogChanged(
        object? sender,
        CatalogChangedEventArgs e)
    {
        string message = e.Kind switch
        {
            CatalogChangeKind.BookAdded =>
                $"{e.At:HH:mm:ss} Додано книгу: {e.Title}, ID: {e.CopyId}",

            CatalogChangeKind.CopyIssued =>
                $"{e.At:HH:mm:ss} Видано примірник {e.CopyId} читачу {e.ReaderId} до {e.DueDate}",

            CatalogChangeKind.CopyReturned =>
                $"{e.At:HH:mm:ss} Повернуто примірник {e.CopyId} від читача {e.ReaderId}",

            _ => "Невідома подія"
        };

        Console.WriteLine(message);

        File.AppendAllText(
            logPath,
            message + Environment.NewLine);
    }

    service.Changed += OnCatalogChanged;

    // Другий підписник: лічильник подій
    int changes = 0;

    service.Changed += (_, _) => changes++;

    // Додаємо книгу
    var book = service.AddBook(
        "LAB6-001",
        "978-617-000-0001",
        "Кобзар");

    // Видаємо книгу
    service.IssueBook(
        book.Id,
        "READER-001",
        DateOnly.FromDateTime(DateTime.Today.AddDays(14)));

    // Повертаємо книгу
    service.ReturnBook(
        book.Id,
        "READER-001");

    Console.WriteLine();
    Console.WriteLine($"Подій отримано: {changes}");

    // Відписуємо першого підписника
    service.Changed -= OnCatalogChanged;

    Console.WriteLine();
    Console.WriteLine("=== Перевірка відписки ===");

    // Операція відбудеться, але перший підписник вже не спрацює
    service.AddBook(
        "LAB6-002",
        "978-617-000-0002",
        "Лісова пісня");

    Console.WriteLine($"Подій отримано після відписки: {changes}");

    // Перевірка помилкової операції
    Console.WriteLine();
    Console.WriteLine("=== Перевірка помилки ===");

    try
    {
        service.IssueBook(
            "NON-EXISTENT",
            "READER-001",
            DateOnly.FromDateTime(DateTime.Today.AddDays(14)));
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Помилка: {ex.Message}");
    }

    Console.WriteLine();
    Console.WriteLine("=== Пошук через Func ===");

    var availableBooks = service.FindAll(b => !b.IsIssued);

    foreach (var item in availableBooks)
    {
        Console.WriteLine(item);
    }

    Console.WriteLine();
    Console.WriteLine($"Файл журналу: {logPath}");


    Console.WriteLine();
    Console.WriteLine("=== Додаткове завдання: NotificationSink ===");

    var sinkService = new LendingService(
        new InMemoryBookStore(),
        [
            new ConsoleSink(),
            new FileSink(logPath)
        ]);

    sinkService.AddBook(
        "SINK-001",
        "978-617-000-0003",
        "Тіні забутих предків");



    Console.WriteLine();
Console.WriteLine("=== Додаткове завдання: Action ===");

string csvPath = Path.Combine("data", "sample.csv");

if (File.Exists(csvPath))
{
    var importResult = BookCsvImporter.Load(
        csvPath,
        message => Console.WriteLine($"Пропущено: {message}"));

    Console.WriteLine(
        $"Успішно імпортовано: {importResult.Items.Count}");
}
}