using Core.Dto;

namespace Core.Domain;

public sealed class BookCopy
{
    public string Id { get; }

    public string Isbn { get; }

    public string Title { get; }

    public bool IsIssued { get; private set; }

    private BookCopy(
        string id,
        string isbn,
        string title,
        bool isIssued)
    {
        Id = id;
        Isbn = isbn;
        Title = title;
        IsIssued = isIssued;
    }

    public static BookCopy Create(
        string id,
        string isbn,
        string title)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Ідентифікатор примірника не може бути порожнім",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(isbn))
        {
            throw new ArgumentException(
                "ISBN не може бути порожнім",
                nameof(isbn));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Назва книги не може бути порожньою",
                nameof(title));
        }

        return new BookCopy(
            id.Trim(),
            isbn.Trim(),
            title.Trim(),
            false);
    }

    public void Issue()
    {
        if (IsIssued)
        {
            throw new InvalidOperationException(
                $"Примірник {Id} вже виданий");
        }

        IsIssued = true;
    }

    public void Return()
    {
        if (!IsIssued)
        {
            throw new InvalidOperationException(
                $"Примірник {Id} не виданий, тому його не можна повернути");
        }

        IsIssued = false;
    }

    public BookCopyDto ToDto()
    {
        return new BookCopyDto(
            Id,
            Isbn,
            Title,
            IsIssued);
    }

    public static BookCopy FromDto(BookCopyDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        BookCopy copy = Create(
            dto.Id,
            dto.Isbn,
            dto.Title);

        if (dto.IsIssued)
        {
            copy.Issue();
        }

        return copy;
    }

    public override string ToString()
    {
        string status = IsIssued
            ? "видана"
            : "доступна";

        return $"{Id} — {Title} ({Isbn}), статус: {status}";
    }
}