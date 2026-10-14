namespace Core.Domain;

public sealed class LibraryService
{
    public Loan IssueCopy(
        BookCopy copy,
        Reader reader,
        string loanId,
        DateTime issuedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.OpenLoansCount >= 5)
        {
            throw new InvalidOperationException(
                $"Читач {reader.Id} вже має " +
                $"{reader.OpenLoansCount} відкритих видач. " +
                "Максимум — 5");
        }

        Loan loan = Loan.Open(
            loanId,
            copy.Id,
            reader.Id,
            issuedOn);

        copy.Issue();

        reader.RegisterLoan();

        return loan;
    }

    public void ReturnCopy(
        BookCopy copy,
        Reader reader,
        Loan loan,
        DateTime returnedOn)
    {
        ArgumentNullException.ThrowIfNull(copy);
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(loan);

        if (loan.IsClosed)
        {
            throw new InvalidOperationException(
                $"Видачу {loan.Id} вже закрито");
        }

        if (loan.BookCopyId != copy.Id ||
            loan.ReaderId != reader.Id)
        {
            throw new InvalidOperationException(
                "Примірник або читач не відповідає цій видачі");
        }

        if (!copy.IsIssued)
        {
            throw new InvalidOperationException(
                $"Примірник {copy.Id} не виданий");
        }

        if (returnedOn < loan.IssuedOn)
        {
            throw new ArgumentOutOfRangeException(
                nameof(returnedOn),
                returnedOn,
                "Дата повернення не може передувати даті видачі");
        }

        loan.Close(returnedOn);

        copy.Return();

        reader.RegisterReturn();
    }
}