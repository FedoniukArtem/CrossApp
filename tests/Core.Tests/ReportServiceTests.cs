using Core.Domain;
using Core.Dto;
using Core.Reports;
using Xunit;

namespace Core.Tests;

public sealed class ReportServiceTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    public void OverdueLoans_ExcludesClosedAndDueToday(int dayOffset, int expected)
    {
        var today = new DateOnly(2026, 11, 12);
        var closed = CreateLoan("CLOSED", "R-1", today.AddDays(-2));
        closed.Loan.Close(today.ToDateTime(TimeOnly.MinValue));
        ScheduledLoan[] loans =
        [
            CreateLoan("FIRST", "R-1", today.AddDays(-1)),
            CreateLoan("SECOND", "R-2", today),
            closed
        ];

        var result = ReportService.OverdueLoans(loans, today.AddDays(dayOffset));

        Assert.Equal(expected, result.Count);
        Assert.DoesNotContain(result, row => row.LoanId == "CLOSED");
    }

    [Fact]
    public void TopReaders_BreaksTiesByReaderIdRegardlessOfInputOrder()
    {
        var date = new DateOnly(2026, 11, 12);
        ScheduledLoan[] loans =
        [
            CreateLoan("L-1", "R-B", date),
            CreateLoan("L-2", "R-A", date),
            CreateLoan("L-3", "R-C", date),
            CreateLoan("L-4", "R-C", date)
        ];

        var result = ReportService.TopReaders(loans, 2);

        Assert.Equal(new[] { "R-C", "R-A" }, result.Select(row => row.ReaderId));
        Assert.Equal(result, ReportService.TopReaders(loans.Reverse(), 2));
    }

    [Fact]
    public void CopyTotals_EmptySource_ReturnsZeros()
    {
        Assert.Equal(new CopySummary(0, 0, 0, 0), ReportService.CopyTotals([]));
        Assert.Empty(ReportService.OverdueLoans([], new DateOnly(2026, 11, 12)));
    }

    [Fact]
    public void CopyTotals_CountsDistinctEditionsAndFractionalAverage()
    {
        BookCopy[] copies =
        [
            BookCopy.Create("C-1", "ISBN-1", "Кобзар"),
            BookCopy.Create("C-2", "ISBN-1", "Кобзар"),
            BookCopy.Create("C-3", "ISBN-2", "Місто")
        ];

        Assert.Equal(new CopySummary(2, 3, 1.5, 2), ReportService.CopyTotals(copies));
    }

    [Fact]
    public void ByPublicationYear_CountsCopiesAndPlacesUnknownYearLast()
    {
        BookCopy[] copies =
        [
            BookCopy.Create("C-1", "ISBN-1", "Кобзар"),
            BookCopy.Create("C-2", "ISBN-1", "Кобзар"),
            BookCopy.Create("C-3", "UNKNOWN", "Невідоме видання")
        ];
        BookDto[] editions = [new("ED-1", "ISBN-1", "Кобзар", 2020)];

        Assert.Equal(
            new[] { new PublicationYearSummary(2020, 1, 2), new PublicationYearSummary(null, 1, 1) },
            ReportService.ByPublicationYear(copies, editions));
    }

    private static ScheduledLoan CreateLoan(string id, string readerId, DateOnly dueDate) =>
        new(Loan.Open(id, "COPY-1", readerId, dueDate.AddDays(-14).ToDateTime(TimeOnly.MinValue)), dueDate);
}
