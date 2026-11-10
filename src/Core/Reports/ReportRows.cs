namespace Core.Reports;

public sealed record ReaderLoanLine(string ReaderId, int LoanCount);

public sealed record OverdueLoanLine(string LoanId, string CopyId, string ReaderId, DateOnly DueDate);

public sealed record PublicationYearSummary(int? Year, int EditionCount, int CopyCount);

public sealed record CopySummary(int EditionCount, int TotalCopies, double AverageCopies, int MaxCopies);

public sealed record EditionLoanLine(string Isbn, string Title, int LoanCount);

public sealed record DeferredReportDemo(int Before, int After, int SnapshotCount, int AfterClear);

public sealed record ReportTiming(TimeSpan StreamingInput, TimeSpan MaterializedInput);
