# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Предметна область
**Бібліотека**
* **Сутності:** `Book` (видання), `BookCopy` (примірник), `Reader` (читач), `Loan` (видача).
* **Призначення:** Облік видач примірників книг читачам та контроль їх повернення.

## Запуск проєкту
```bash
dotnet build
dotnet run --project src/Cli

## Self-Contained Публікація
* **RID:** `win-x64`
* **Команда:** `dotnet publish src/Cli -c Release -r win-x64 --self-contained true`
* **Запуск бінарника:** `.\src\Cli\bin\Release\net8.0\win-x64\publish\Cli.exe`


## Структура

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
└── src/
    ├── Core/
    │   ├── Core.csproj          (Multi-targeting: net8.0;net10.0)
    │   └── EnvironmentInfo.cs   (DTO EnvironmentReport + збір даних)
    └── Cli/
        ├── Cli.csproj           (ProjectReference на Core)
        └── Program.cs           (Лише форматування та вивід)


# Збірка бібліотеки Core (під всі TFM)
dotnet build src/Core/Core.csproj

# Запуск консольного застосунку
dotnet run --project src/Cli/Cli.csproj





# 1. Self-Contained (Автономна)
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net8.0 -o publish/win-x64-sc

# 2. Framework-Dependent (Залежна від .NET Runtime)
dotnet publish src/Cli -c Release -r win-x64 --self-contained false -f net8.0 -o publish/win-x64-fd

# 3. Single-File (Збірка в один файл)
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net8.0 -p:PublishSingleFile=true -o publish/win-x64-singlefile

# 4. Single-File + Trimming (Один файл + очищення коду)
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net8.0 -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish/win-x64-trimmed


Режим публікації,RID,Розмір (МБ),Кількість файлів,Потрібен runtime?
Self-Contained,win-x64,~70.67 МБ,70.52+,Ні
Framework-Dependent,win-x64,~0.17 МБ,0,18,Так (.NET 8/10)
Single-File,win-x64,~64.41 МБ,64,4,Ні
Single-File + Trimmed,win-x64,11,77 МБ,12,Ні