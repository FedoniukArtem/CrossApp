namespace Core.Dto;

public record BookCopyDto(
    string Id,
    string Isbn,
    string Title,
    bool IsIssued,
    string? Author = null,
    int Year = 0);
