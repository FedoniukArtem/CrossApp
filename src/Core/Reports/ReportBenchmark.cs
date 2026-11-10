using System.Diagnostics;
using Core.Domain;

namespace Core.Reports;

public static class ReportBenchmark
{
    public const int RecordCount = 100_000;

    public static ReportTiming Run(DateOnly today)
    {
        DateTime issuedOn = today.AddDays(-14).ToDateTime(TimeOnly.MinValue);
        var source = Enumerable.Range(0, RecordCount)
            .Select(i => new ScheduledLoan(
                Loan.Open($"L-{i}", $"COPY-{i}", $"R-{i % 1_000:0000}", issuedOn),
                today.AddDays(i % 3 - 1)))
            .ToList();
        var query = source.Where(item => item.DueDate < today);

        for (int i = 0; i < 3; i++)
        {
            Measure(query, materialize: false);
            Measure(query, materialize: true);
        }

        var streaming = new TimeSpan[7];
        var materialized = new TimeSpan[7];
        for (int i = 0; i < streaming.Length; i++)
        {
            streaming[i] = Measure(query, materialize: false);
            materialized[i] = Measure(query, materialize: true);
        }

        Array.Sort(streaming);
        Array.Sort(materialized);
        return new ReportTiming(streaming[3], materialized[3]);
    }

    private static TimeSpan Measure(IEnumerable<ScheduledLoan> query, bool materialize)
    {
        long started = Stopwatch.GetTimestamp();
        _ = ReportService.TopReaders(materialize ? query.ToList() : query, 5);
        return Stopwatch.GetElapsedTime(started);
    }
}
