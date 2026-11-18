using System.Text.Json;
using Core;
using Core.Abstractions;
using Core.Import;
using Core.Services;
using Core.Storage;

internal static class ImportCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string path = Path.Combine("data", "library-import.json");
        int timeoutMs = Timeout.Infinite;
        bool stream = args.Contains("import-stream");
        bool hasPath = false;

        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "import":
                    case "import-stream":
                    case "--file":
                        break;
                    case "--timeout-ms":
                        if (++i >= args.Length || !int.TryParse(args[i], out timeoutMs) || timeoutMs < 0)
                            throw new ArgumentException("Після --timeout-ms вкажіть невід'ємне число мілісекунд.");
                        break;
                    default:
                        if (hasPath || args[i].StartsWith("--"))
                            throw new ArgumentException($"Невідомий аргумент: {args[i]}");
                        path = args[i];
                        hasPath = true;
                        break;
                }
            }
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            return 1;
        }

        using var userCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(userCts.Token, timeoutCts.Token);
        if (timeoutMs == 0)
            timeoutCts.Cancel();
        else if (timeoutMs > 0)
            timeoutCts.CancelAfter(timeoutMs);

        void OnCancel(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            userCts.Cancel();
        }

        Console.CancelKeyPress += OnCancel;
        IBookStore store = args.Contains("--file")
            ? StoreFactory.Create(args)
            : new InMemoryBookStore();
        var service = new LendingService(store);
        var importer = new JsonBookImporter();
        var progress = new ConsoleImportProgress();
        int addedThisRun = 0;
        service.Changed += (_, _) => addedThisRun++;

        try
        {
            Console.WriteLine(stream ? "Потоковий імпорт каталогу" : "Асинхронний імпорт каталогу");
            int added = stream
                ? await service.ImportStreamAsync(importer, path, linkedCts.Token, progress)
                : await service.ImportAsync(importer, path, linkedCts.Token, progress);
            Console.WriteLine($"Імпортовано записів: {added}");
            Console.WriteLine($"Усього примірників у сховищі: {service.All().Count}");
            return 0;
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            Console.WriteLine("Імпорт скасовано.");
            Console.WriteLine(timeoutCts.IsCancellationRequested && !userCts.IsCancellationRequested
                ? "Причина: таймаут."
                : "Причина: Ctrl+C.");
            Console.WriteLine($"Збережено за цей запуск: {addedThisRun}");
            return 2;
        }
        catch (DomainException ex)
        {
            Console.WriteLine($"Дані відхилено: {ex.Message}");
            Console.WriteLine($"Збережено за цей запуск: {addedThisRun}");
            return 1;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Помилка JSON: рядок {ex.LineNumber + 1}, позиція {ex.BytePositionInLine + 1}.");
            Console.WriteLine($"Збережено за цей запуск: {addedThisRun}");
            return 1;
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Помилка файлу: {ex.Message}");
            return 1;
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Немає доступу до файлу: {ex.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= OnCancel;
        }
    }

    // Синхронний підписник забезпечує порядок виводу в консольному застосунку.
    private sealed class ConsoleImportProgress : IProgress<int>
    {
        private int _lastPercent = -1;

        public void Report(int value)
        {
            if (value != _lastPercent)
            {
                Console.WriteLine($"Прогрес: {value}%");
                _lastPercent = value;
            }
        }
    }
}
