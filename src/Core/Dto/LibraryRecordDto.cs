namespace Core.Dto;

public record LibraryRecordDto(
    string Id,
    string Isbn,
    string Title,
    bool IsIssued);