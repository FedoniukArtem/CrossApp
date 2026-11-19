namespace Core.Configuration;

public sealed class AppOptions
{
    public string EnvironmentName { get; set; } = "Development";
    public StorageOptions Storage { get; set; } = new();
    public LoggingOptions Logging { get; set; } = new();
    public LibraryOptions Library { get; set; } = new();
}

public sealed class StorageOptions
{
    public string DataPath { get; set; } = "data";
    public string FileName { get; set; } = "library.json";

    public string ResolveFullPath(string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(DataPath) || string.IsNullOrWhiteSpace(FileName))
            throw new ArgumentException("Storage:DataPath і Storage:FileName повинні бути заповнені.");
        if (Path.GetFileName(FileName) != FileName || FileName is "." or "..")
            throw new ArgumentException("Storage:FileName має бути назвою файлу.");

        string root = Path.GetFullPath(DataPath, baseDirectory);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }
}

public sealed class LoggingOptions
{
    public string MinimumLevel { get; set; } = "Information";
}

public sealed class LibraryOptions
{
    public int LoanDays { get; set; } = 14;
}
