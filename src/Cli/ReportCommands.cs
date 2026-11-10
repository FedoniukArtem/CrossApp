using Core;
using Core.Domain;
using Core.Reports;
using Core.Services;
using Core.Storage;

internal static class ReportCommands
{
    internal static int Run(string command, string[] args)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (command == "report-benchmark")
        {
            var timing = ReportBenchmark.Run(today);
            Console.WriteLine($"Вхідних видач: {ReportBenchmark.RecordCount}");
            Console.WriteLine("Медіана семи вимірювань:");
            Console.WriteLine($"Без проміжного ToList(): {timing.StreamingInput.TotalMilliseconds:F2} мс");
            Console.WriteLine($"З проміжним ToList():   {timing.MaterializedInput.TotalMilliseconds:F2} мс");
            return 0;
        }

        bool useFile = args.Contains("--file");
        var service = new LendingService(StoreFactory.Create(args));
        IReadOnlyList<ScheduledLoan> loans = useFile ? [] : SampleData.Loans(today);

        switch (command)
        {
            case "report-top":
                if (useFile)
                    Console.WriteLine("Файлове сховище не містить історії видач.");

                Console.WriteLine($"{"Читач",-12} {"Видачі",6}");
                foreach (var row in ReportService.TopReaders(loans, 5))
                    Console.WriteLine($"{row.ReaderId,-12} {row.LoanCount,6}");
                break;

            case "report-low":
            case "report-overdue":
                if (useFile)
                    Console.WriteLine("Файлове сховище не містить строків повернення.");

                var overdue = ReportService.OverdueLoans(loans, today);
                Console.WriteLine($"Прострочені видачі: {overdue.Count}");
                Console.WriteLine($"{"Видача",-10} {"Примірник",-12} {"Читач",-10} {"Повернути до",12}");
                foreach (var row in overdue)
                    Console.WriteLine($"{row.LoanId,-10} {row.CopyId,-12} {row.ReaderId,-10} {row.DueDate:yyyy-MM-dd}");
                break;

            case "report-groups":
                Console.WriteLine($"{"Рік",-12} {"Видань",8} {"Примірників",12}");
                foreach (var row in ReportService.ByPublicationYear(service.All(), SampleData.Editions()))
                    Console.WriteLine($"{row.Year?.ToString() ?? "Невідомий",-12} {row.EditionCount,8} {row.CopyCount,12}");
                break;

            case "report-sum":
                var total = ReportService.CopyTotals(service.All());
                Console.WriteLine($"Видань: {total.EditionCount} | Примірників: {total.TotalCopies}");
                Console.WriteLine($"Середнє на видання: {total.AverageCopies:F2} | Максимум: {total.MaxCopies}");
                break;

            case "report-editions":
                if (useFile)
                    Console.WriteLine("Файлове сховище не містить історії видач.");
                else
                {
                    Console.WriteLine($"{"ISBN",-19} {"Видання",-24} {"Видачі",6}");
                    foreach (var row in ReportService.TopEditions(service.All(), loans, 5))
                        Console.WriteLine($"{row.Isbn,-19} {row.Title,-24} {row.LoanCount,6}");
                }
                break;

            case "report-deferred":
                var demo = ReportService.DemonstrateDeferredExecution(loans, today);
                Console.WriteLine($"Прострочених до змін: {demo.Before}");
                Console.WriteLine($"Прострочених після додавання: {demo.After}");
                Console.WriteLine($"Після очищення джерела: запит {demo.AfterClear}, знімок {demo.SnapshotCount}");
                break;

            default:
                Console.WriteLine("Команди: report-top | report-overdue | report-groups | report-sum | " +
                    "report-editions | report-deferred | report-benchmark");
                return 1;
        }

        return 0;
    }
}
