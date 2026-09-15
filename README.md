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