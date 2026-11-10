namespace Core.Domain;

public sealed record ScheduledLoan(Loan Loan, DateOnly DueDate);
