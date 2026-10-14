using Core.Dto;

namespace Core.Domain;

public sealed class Loan
{
    public string Id { get; }

    public string BookCopyId { get; }

    public string ReaderId { get; }

    public DateTime IssuedOn { get; }

    public DateTime? ReturnedOn { get; private set; }

    public bool IsClosed => ReturnedOn.HasValue;

    private Loan(
        string id,
        string bookCopyId,
        string readerId,
        DateTime issuedOn,
        DateTime? returnedOn)
    {
        Id = id;
        BookCopyId = bookCopyId;
        ReaderId = readerId;
        IssuedOn = issuedOn;
        ReturnedOn = returnedOn;
    }

    public static Loan Open(
        string id,
        string bookCopyId,
        string readerId,
        DateTime issuedOn)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Ідентифікатор видачі не може бути порожнім",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(bookCopyId))
        {
            throw new ArgumentException(
                "Ідентифікатор примірника не може бути порожнім",
                nameof(bookCopyId));
        }

        if (string.IsNullOrWhiteSpace(readerId))
        {
            throw new ArgumentException(
                "Ідентифікатор читача не може бути порожнім",
                nameof(readerId));
        }

        if (issuedOn == default)
        {
            throw new ArgumentException(
                "Дата видачі повинна бути вказана",
                nameof(issuedOn));
        }

        return new Loan(
            id.Trim(),
            bookCopyId.Trim(),
            readerId.Trim(),
            issuedOn,
            null);
    }

    public void Close(DateTime returnedOn)
    {
        if (IsClosed)
        {
            throw new InvalidOperationException(
                $"Видачу {Id} вже закрито");
        }

        if (returnedOn < IssuedOn)
        {
            throw new ArgumentOutOfRangeException(
                nameof(returnedOn),
                returnedOn,
                "Дата повернення не може передувати даті видачі");
        }

        ReturnedOn = returnedOn;
    }

    public LoanDto ToDto()
    {
        return new LoanDto(
            Id,
            BookCopyId,
            ReaderId,
            IssuedOn,
            ReturnedOn);
    }

    public static Loan FromDto(LoanDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Loan loan = Open(
            dto.Id,
            dto.BookCopyId,
            dto.ReaderId,
            dto.IssuedOn);

        if (dto.ReturnedOn.HasValue)
        {
            loan.Close(dto.ReturnedOn.Value);
        }

        return loan;
    }

    public override string ToString()
    {
        string status = IsClosed
            ? $"повернуто {ReturnedOn:dd.MM.yyyy}"
            : "відкрита видача";

        return $"Видача {Id}: примірник {BookCopyId}, " +
               $"читач {ReaderId}, {status}";
    }
}