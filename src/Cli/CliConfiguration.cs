using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

internal sealed record CliConfiguration(AppOptions Options, string? ApiKey, string[] CommandArguments)
{
    internal static CliConfiguration Load(string[] args)
    {
        var settings = new List<string>();
        var commands = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            string key = args[i].Split('=', 2)[0];
            bool isSetting = key.StartsWith("--Storage:", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("--Logging:", StringComparison.OrdinalIgnoreCase) ||
                key.StartsWith("--Library:", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("--EnvironmentName", StringComparison.OrdinalIgnoreCase);
            if (isSetting)
            {
                settings.Add(args[i]);
                if (!args[i].Contains('='))
                {
                    if (++i == args.Length)
                        throw new ArgumentException($"Для {key} потрібне значення.");
                    settings.Add(args[i]);
                }
            }
            else
            {
                commands.Add(args[i]);
            }
        }

        var bootstrap = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables("CROSSAPP_")
            .AddCommandLine(settings.ToArray())
            .Build();
        string environment = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT")
            ?? bootstrap["EnvironmentName"] ?? "Development";
        if (environment.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
            throw new ArgumentException("Некоректна назва середовища.");

        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EnvironmentName"] = environment });
        if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            builder.AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true);
        var config = builder.AddEnvironmentVariables("CROSSAPP_")
            .AddCommandLine(settings.ToArray()).Build();
        var options = config.Get<AppOptions>() ?? new AppOptions();
        string? simplePath = Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH");
        if (!string.IsNullOrWhiteSpace(simplePath))
            options.Storage.DataPath = simplePath;
        if (options.Library.LoanDays <= 0)
            throw new ArgumentException("Library:LoanDays має бути більшим за нуль.");
        string[] levels = ["Trace", "Debug", "Information", "Warning", "Error", "Critical", "None"];
        if (!levels.Contains(options.Logging.MinimumLevel, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Невідомий Logging:MinimumLevel.");

        return new CliConfiguration(options, config["Integration:ApiKey"], commands.ToArray());
    }

    internal void Print()
    {
        Console.WriteLine($"Середовище: {Options.EnvironmentName}");
        Console.WriteLine($"Файл даних: {Options.Storage.ResolveFullPath(AppContext.BaseDirectory)}");
        Console.WriteLine($"Рівень логування: {Options.Logging.MinimumLevel}");
        Console.WriteLine($"Термін видачі: {Options.Library.LoanDays} днів");
        Console.WriteLine(string.IsNullOrEmpty(ApiKey) ? "ApiKey: не заданий" : $"ApiKey: присутній, довжина {ApiKey.Length}");
        Debug("Конфігурацію завантажено.");
    }

    internal void Debug(string message)
    {
        if (Options.Logging.MinimumLevel.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
            Options.Logging.MinimumLevel.Equals("Trace", StringComparison.OrdinalIgnoreCase))
            Console.WriteLine($"[Debug] {message}");
    }
}
