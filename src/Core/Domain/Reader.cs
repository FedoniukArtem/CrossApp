using Core.Dto;

namespace Core.Domain;

public sealed class Reader
{
    public string Id { get; }

    public string Name { get; }

    public string? Email { get; }

    public int OpenLoansCount { get; private set; }

    private Reader(
        string id,
        string name,
        string? email,
        int openLoansCount)
    {
        Id = id;
        Name = name;
        Email = email;
        OpenLoansCount = openLoansCount;
    }

    public static Reader Create(
        string id,
        string name,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "Ідентифікатор читача не може бути порожнім",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Ім'я читача не може бути порожнім",
                nameof(name));
        }

        return new Reader(
            id.Trim(),
            name.Trim(),
            string.IsNullOrWhiteSpace(email)
                ? null
                : email.Trim(),
            0);
    }

    public void RegisterLoan()
    {
        OpenLoansCount++;
    }

    public void RegisterReturn()
    {
        if (OpenLoansCount <= 0)
        {
            throw new InvalidOperationException(
                $"У читача {Id} немає відкритих видач");
        }

        OpenLoansCount--;
    }

    public ReaderDto ToDto()
    {
        return new ReaderDto(
            Id,
            Name,
            Email);
    }

    public static Reader FromDto(ReaderDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return Create(
            dto.Id,
            dto.Name,
            dto.Email);
    }

    public override string ToString()
    {
        return $"{Id} — {Name}, відкритих видач: {OpenLoansCount}";
    }
}