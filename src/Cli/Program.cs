using System;
using System.Runtime.InteropServices;

// Налаштування UTF-8 для правильного виводу кирилиці в консолі Windows
Console.OutputEncoding = System.Text.Encoding.UTF8;

Console.WriteLine("CrossApp - практикум з крос-платформного програмування");
Console.WriteLine("Студент: Федонюк Артем, група ФЕІ-34");
Console.WriteLine(new string('-', 55));
Console.WriteLine($"ОС (OSDescription): {RuntimeInformation.OSDescription}");
Console.WriteLine($"ОС (Environment) : {Environment.OSVersion}");
Console.WriteLine($"Архітектура процесу: {RuntimeInformation.ProcessArchitecture}");
Console.WriteLine($"Версія .NET (CLR): {Environment.Version}");
Console.WriteLine($"Runtime            : {RuntimeInformation.FrameworkDescription}");
Console.WriteLine($"Каталог застосунку: {AppContext.BaseDirectory}");
Console.WriteLine($"Поточний каталог  : {Environment.CurrentDirectory}");
Console.WriteLine(new string('-', 55));
Console.WriteLine("Предметна область: Бібліотека (Book, BookCopy, Reader, Loan)");