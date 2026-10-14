using Core.Dto;
using Core.Import;

namespace Core.Domain;

public abstract record DomainLibraryItem;

public sealed record DomainBookItem(
    BookCopy Book) : DomainLibraryItem;

public sealed record DomainReaderItem(
    Reader Reader) : DomainLibraryItem;

public static class DomainLibraryImporter
{
    public static DomainImportResult<DomainLibraryItem> Convert(
        ImportResult<LibraryItem> importResult)
    {
        ArgumentNullException.ThrowIfNull(importResult);

        var items = new List<DomainLibraryItem>();
        var errors = new List<string>(importResult.Errors);

        foreach (LibraryItem item in importResult.Items)
        {
            try
            {
                switch (item)
                {
                    case BookItem bookItem:
                    {
                        BookDto dto = bookItem.Book;

                        BookCopy copy = BookCopy.Create(
                            dto.Id,
                            dto.Isbn,
                            dto.Title);

                        items.Add(
                            new DomainBookItem(copy));

                        break;
                    }

                    case ReaderItem readerItem:
                    {
                        ReaderDto dto = readerItem.Reader;

                        Reader reader = Reader.Create(
                            dto.Id,
                            dto.Name,
                            dto.Email);

                        items.Add(
                            new DomainReaderItem(reader));

                        break;
                    }

                    default:
                        errors.Add(
                            "Невідомий тип імпортованого запису");

                        break;
                }
            }
            catch (ArgumentException ex)
            {
                errors.Add(ex.Message);
            }
        }

        return new DomainImportResult<DomainLibraryItem>(
            items,
            errors);
    }
}