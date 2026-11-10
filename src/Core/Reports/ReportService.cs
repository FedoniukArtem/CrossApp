using Core.Domain;
using Core.Dto;

namespace Core.Reports;

public static class ReportService
{
    public static IReadOnlyList<ReaderLoanLine> TopReaders(IEnumerable<ScheduledLoan> loans, int count) =>
        loans.GroupBy(item => item.Loan.ReaderId, StringComparer.Ordinal)
            .Select(g => new ReaderLoanLine(g.Key, g.Count()))
            .TopBy(row => row.LoanCount, row => row.ReaderId, count);

    public static IReadOnlyList<OverdueLoanLine> OverdueLoans(IEnumerable<ScheduledLoan> loans, DateOnly today) =>
        OpenPastDue(loans, today)
            .OrderBy(item => item.DueDate)
            .ThenBy(item => item.Loan.Id, StringComparer.Ordinal)
            .Select(item => new OverdueLoanLine(item.Loan.Id, item.Loan.BookCopyId, item.Loan.ReaderId, item.DueDate))
            .ToList();

    public static IReadOnlyList<PublicationYearSummary> ByPublicationYear(
        IEnumerable<BookCopy> copies, IEnumerable<BookDto> editions)
    {
        var yearsByIsbn = editions.ToDictionary(edition => edition.Isbn, edition => (int?)edition.Year,
            StringComparer.OrdinalIgnoreCase);
        var query =
            from copy in copies
            group copy by yearsByIsbn.GetValueOrDefault(copy.Isbn) into year
            orderby year.Key == null, year.Key
            select new PublicationYearSummary(year.Key,
                year.Select(copy => copy.Isbn).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                year.Count());

        return query.ToList();
    }

    public static CopySummary CopyTotals(IEnumerable<BookCopy> copies)
    {
        var counts = copies.GroupBy(copy => copy.Isbn, StringComparer.OrdinalIgnoreCase)
            .Select(edition => edition.Count())
            .ToList();

        return counts.Count == 0
            ? new CopySummary(0, 0, 0, 0)
            : new CopySummary(counts.Count, counts.Sum(), counts.Average(), counts.Max());
    }

    public static IReadOnlyList<EditionLoanLine> TopEditions(
        IEnumerable<BookCopy> copies, IEnumerable<ScheduledLoan> loans, int count)
    {
        var catalog = copies.ToList();
        var loansByIsbn = loans.Join(catalog, item => item.Loan.BookCopyId, copy => copy.Id,
            (_, copy) => copy.Isbn, StringComparer.OrdinalIgnoreCase);

        return catalog.DistinctBy(copy => copy.Isbn, StringComparer.OrdinalIgnoreCase)
            .GroupJoin(loansByIsbn, copy => copy.Isbn, isbn => isbn,
                (copy, matches) => new EditionLoanLine(copy.Isbn, copy.Title, matches.Count()),
                StringComparer.OrdinalIgnoreCase)
            .TopBy(row => row.LoanCount, row => row.Isbn, count);
    }

    public static DeferredReportDemo DemonstrateDeferredExecution(IEnumerable<ScheduledLoan> loans, DateOnly today)
    {
        var source = loans.ToList();
        var query = OpenPastDue(source, today);
        int before = query.Count();

        var loan = Loan.Open("DEMO", "COPY-DEMO", "R-DEMO", today.AddDays(-14).ToDateTime(TimeOnly.MinValue));
        source.Add(new ScheduledLoan(loan, today.AddDays(-1)));
        int after = query.Count();

        var snapshot = query.ToList();
        source.Clear();
        return new DeferredReportDemo(before, after, snapshot.Count, query.Count());
    }

    private static IEnumerable<ScheduledLoan> OpenPastDue(IEnumerable<ScheduledLoan> loans, DateOnly today) =>
        loans.Where(item => !item.Loan.IsClosed && item.DueDate < today);
}
