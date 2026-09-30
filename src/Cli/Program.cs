using System;
using System.Text.Encodings.Web;
using System.Text.Json;
using Core;

// Налаштування UTF-8 для правильного виводу кирилиці в консолі
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Отримуємо скомпільований звіт про середовище безпосередньо з бібліотеки Core
EnvironmentReport report = EnvironmentInfo.Collect();

// Перевіряємо прапорець командного рядка --json
if (args.Length > 0 && args[0].Equals("--json", StringComparison.OrdinalIgnoreCase))
{
    var options = new JsonSerializerOptions 
    { 
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    string jsonOutput = JsonSerializer.Serialize(report, options);
    Console.WriteLine(jsonOutput);
}
else
{
    Console.WriteLine(report.AppName);
    Console.WriteLine($"Студент: {report.Student}, група: {report.Group}");
    Console.WriteLine(new string('-', 55));
    Console.WriteLine($"ОС (OSDescription) : {report.OsDescription}");
    Console.WriteLine($"ОС (Environment)    : {report.OsVersion}");
    Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR)   : {report.DotNetVersion}");
    Console.WriteLine($"Runtime             : {report.FrameworkDescription}");
    Console.WriteLine($"RID (визначено)     : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET)      : {report.ReportedRid}");
    Console.WriteLine($"Каталог застосунку  : {report.AppDirectory}");
    Console.WriteLine($"Поточний каталог    : {report.CurrentDirectory}");
    Console.WriteLine($"Примітка збірки     : {report.BuildNote}");
    Console.WriteLine(new string('-', 55));
    Console.WriteLine($"Предметна область   : {report.Domain}");
}