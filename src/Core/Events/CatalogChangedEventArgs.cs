namespace Core.Events;

public sealed class CatalogChangedEventArgs(
    CatalogChangeKind kind,
    string copyId,
    string? readerId = null,
    DateOnly? dueDate = null,
    string? title = null) : EventArgs
{
    public CatalogChangeKind Kind { get; } = kind;
    public string CopyId { get; } = copyId;
    public string? ReaderId { get; } = readerId;
    public DateOnly? DueDate { get; } = dueDate;
    public string? Title { get; } = title;
    public DateTimeOffset At { get; } = DateTimeOffset.Now;
}