using System.Text.Json;
using Core;
using Core.Storage;

internal static class LibraryCommands
{
    internal static int Run(string command, string[] args, CliConfiguration configuration)
    {
        try
        {
            string path = configuration.Options.Storage.ResolveFullPath(AppContext.BaseDirectory);
            switch (command)
            {
                case "config":
                    return 0;
                case "export":
                    if (args.Length != 1)
                        throw new ArgumentException("export використовує шлях Storage:DataPath / FileName.");
                    var books = File.Exists(path) ? new FileBookStore(path).List() : SampleData.Books();
                    LibraryJson.WriteAtomic(path, books);
                    Console.WriteLine($"Експортовано видань: {books.Select(book => book.Isbn).Distinct().Count()}");
                    Console.WriteLine($"Експортовано примірників: {books.Count}");
                    configuration.Debug("JSON збережено атомарно.");
                    return 0;
                case "import-json":
                    if (args.Length > 2)
                        throw new ArgumentException("Використання: import-json [шлях до JSON].");
                    string input = args.Length == 2 ? Path.GetFullPath(args[1]) : path;
                    var imported = LibraryJson.Read(input);
                    LibraryJson.WriteAtomic(path, imported);
                    Console.WriteLine($"Імпортовано примірників: {imported.Count}");
                    configuration.Debug("Колекцію перевірено та збережено.");
                    return 0;
                default:
                    throw new ArgumentException($"Невідома команда: {command}");
            }
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Помилка JSON: {ex.Message}");
            return 1;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or DomainException)
        {
            Console.WriteLine($"Помилка: {ex.Message}");
            return 1;
        }
    }
}
