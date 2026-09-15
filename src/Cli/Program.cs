using System;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;

// Налаштування UTF-8 для правильного виводу кирилиці в консолі
Console.OutputEncoding = System.Text.Encoding.UTF8;

// Збираємо дані в об'єкт
var info = new
{
    App = "CrossApp - практикум з крос-платформного програмування",
    Student = "Федонюк Артем",
    Group = "ФЕІ-34",            
    OSDescription = RuntimeInformation.OSDescription,
    OSVersion = Environment.OSVersion.ToString(),
    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    DotNetVersion = Environment.Version.ToString(),
    Runtime = RuntimeInformation.FrameworkDescription,
    AppDirectory = AppContext.BaseDirectory,
    CurrentDirectory = Environment.CurrentDirectory,
    Domain = "Бібліотека (Book, BookCopy, Reader, Loan)"
};

// Перевіряємо прапорець командного рядка --json
if (args.Length > 0 && args[0].Equals("--json", StringComparison.OrdinalIgnoreCase))
{
    var options = new JsonSerializerOptions 
    { 
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    string jsonOutput = JsonSerializer.Serialize(info, options);
    Console.WriteLine(jsonOutput);
}
else
{
    Console.WriteLine(info.App);
    Console.WriteLine($"Студент: {info.Student}, група: {info.Group}");
    Console.WriteLine(new string('-', 55));
    Console.WriteLine($"ОС (OSDescription): {info.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {info.OSVersion}");
    Console.WriteLine($"Архітектура процесу: {info.Architecture}");
    Console.WriteLine($"Версія .NET (CLR): {info.DotNetVersion}");
    Console.WriteLine($"Runtime            : {info.Runtime}");
    Console.WriteLine($"Каталог застосунку: {info.AppDirectory}");
    Console.WriteLine($"Поточний каталог  : {info.CurrentDirectory}");
    Console.WriteLine(new string('-', 55));
    Console.WriteLine($"Предметна область: {info.Domain}");
}