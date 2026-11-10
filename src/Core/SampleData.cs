using Core.Domain;
using Core.Dto;

namespace Core;

public static class SampleData
{
    private static readonly (BookDto Edition, int Copies)[] Catalog =
    [
        (new("ED-01", "978-617-000-0001", "Кобзар", 2020), 3),
        (new("ED-02", "978-617-000-0002", "Тіні забутих предків", 2018), 2),
        (new("ED-03", "978-617-000-0003", "Місто", 2021), 4),
        (new("ED-04", "978-617-000-0004", "Захар Беркут", 2020), 1),
        (new("ED-05", "978-617-000-0005", "Кайдашева сім'я", 2018), 3),
        (new("ED-06", "978-617-000-0006", "Лісова пісня", 2021), 2),
        (new("ED-07", "978-617-000-0007", "Тигролови", 2023), 2),
        (new("ED-08", "978-617-000-0008", "Intermezzo", 2023), 1)
    ];

    public static IReadOnlyList<BookDto> Editions() =>
        Catalog.Select(item => item.Edition).ToList();

    public static List<BookCopy> Books()
    {
        var books = new List<BookCopy>();

        foreach (var (edition, _) in Catalog)
        {
            books.Add(BookCopy.Create($"COPY-{books.Count + 1:000}", edition.Isbn, edition.Title));
        }

        foreach (var (edition, copies) in Catalog)
        {
            for (int i = 1; i < copies; i++)
            {
                books.Add(BookCopy.Create($"COPY-{books.Count + 1:000}", edition.Isbn, edition.Title));
            }
        }

        return books;
    }

    public static IReadOnlyList<ScheduledLoan> Loans(DateOnly today)
    {
        return
        [
            CreateLoan("L-01", "COPY-001", "R-02", -5),
            CreateLoan("L-02", "COPY-002", "R-01", -2),
            CreateLoan("L-03", "COPY-003", "R-03", -1),
            CreateLoan("L-04", "COPY-004", "R-01", 0),
            CreateLoan("L-05", "COPY-005", "R-04", 3),
            CreateLoan("L-06", "COPY-006", "R-02", 5),
            CreateLoan("L-07", "COPY-001", "R-01", -40, -42),
            CreateLoan("L-08", "COPY-002", "R-02", -38, -35),
            CreateLoan("L-09", "COPY-003", "R-03", -35, -37),
            CreateLoan("L-10", "COPY-004", "R-05", -32, -34),
            CreateLoan("L-11", "COPY-005", "R-06", -30, -28),
            CreateLoan("L-12", "COPY-007", "R-06", -28, -29)
        ];

        ScheduledLoan CreateLoan(
            string id, string copyId, string readerId, int dueOffset, int? returnedOffset = null)
        {
            var dueDate = today.AddDays(dueOffset);
            var loan = Loan.Open(id, copyId, readerId, dueDate.AddDays(-14).ToDateTime(TimeOnly.MinValue));

            if (returnedOffset is int days)
            {
                loan.Close(today.AddDays(days).ToDateTime(TimeOnly.MinValue));
            }

            return new ScheduledLoan(loan, dueDate);
        }
    }
}
