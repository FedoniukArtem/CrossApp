namespace Core.Domain;

public sealed record DomainImportResult<T>(
    IReadOnlyList<T> Items,
    IReadOnlyList<string> Errors);