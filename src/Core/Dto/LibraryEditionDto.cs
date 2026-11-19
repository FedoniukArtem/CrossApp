namespace Core.Dto;

public sealed record LibraryEditionDto(
    string Isbn, string Title, string? Author, int Year, IReadOnlyList<BookCopyDto> Copies);
