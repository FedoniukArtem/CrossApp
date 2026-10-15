using Core.Domain;

namespace Core;

public static class SampleData
{
    public static List<BookCopy> Books() =>
    [
        BookCopy.Create("COPY-001", "978-617-000-0001", "Кобзар"),
        BookCopy.Create("COPY-002", "978-617-000-0002", "Тіні забутих предків"),
        BookCopy.Create("COPY-003", "978-617-000-0003", "Місто"),
        BookCopy.Create("COPY-004", "978-617-000-0004", "Захар Беркут"),
        BookCopy.Create("COPY-005", "978-617-000-0005", "Кайдашева сім'я")
    ];
}