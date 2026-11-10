namespace Core.Reports;

internal static class ReportExtensions
{
    internal static IReadOnlyList<T> TopBy<T>(
        this IEnumerable<T> source, Func<T, int> score, Func<T, string> key, int count) =>
        source.OrderByDescending(score)
            .ThenBy(key, StringComparer.Ordinal)
            .Take(count)
            .ToList();
}
