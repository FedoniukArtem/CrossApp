Ось повний, готовий **README.md** одним блоком — просто скопіюй його та заміни вміст у своєму файлі.

```markdown
# CrossApp

Наскрізний проєкт з крос-платформного програмування.

## Предметна область

* **Домен:** Бібліотека[cite: 1]
* **Сутності:** `Book` (видання), `BookCopy` (примірник), `Reader` (читач), `Loan` (видача).
* **Призначення:** Облік видач примірників книг читачам та контроль їх повернення.

---

## Опис Лабораторної роботи №3

У цій роботі реалізовано базові DTO-моделі предметної області та стійкий до помилок імпорт даних із текстових файлів (CSV, JSON):
1. **DTO (records):** `BookDto`, `ReaderDto` та узагальнений `ImportResult<T>` (для збереження зчитаних об'єктів і помилок з номерами рядків)[cite: 3, 4].
2. **Pattern Matching:** розбір рядків через `switch expression` з використанням list patterns, property patterns та охоронної умови `when`[cite: 2, 5, 9].
3. **Безпечний імпорт:** пошкоджені рядки у файлі пропускаються і фіксуються в списку помилок, не перериваючи завантаження інших записів[cite: 3].

---

## Структура проєкту

```text
CrossApp/
├── CrossApp.sln
├── README.md
├── .gitignore
├── data/
│   ├── sample.csv               (CSV-файл із 10+ коректними та 2-3 пошкодженими рядками)
│   └── sample.json              (JSON-файл для перевірки альтернативного імпорту)
└── src/
    ├── Core/
    │   ├── Core.csproj          (Multi-targeting: net8.0;net10.0)
    │   ├── EnvironmentInfo.cs   (DTO EnvironmentReport + збір даних)
    │   ├── Dto/
    │   │   ├── BookDto.cs       (DTO книги)
    │   │   ├── ReaderDto.cs     (DTO читача)
    │   │   └── ImportResult.cs  (Generic-результат імпорту з помилками)
    │   └── Import/
    │       ├── BookCsvImporter.cs       (Розбір CSV через switch & pattern matching)
    │       ├── BookJsonImporter.cs      (Імпорт JSON через System.Text.Json)
    │       └── LibraryMixedImporter.cs  (Імпорт різнорідних записів B;... та R;...)
    └── Cli/
        ├── Cli.csproj           (ProjectReference на Core)
        └── Program.cs           (Аргументи шляху, виклики імпортерів, вивід статистики)

```

---

## Швидкий запуск

### Збірка та запуск проєкту

```bash
# Збірка бібліотеки Core (під всі TFM)
dotnet build src/Core/Core.csproj

# Збірка всього рішення
dotnet build

# Запуск консольного застосунку (за замовчуванням зчитує data/sample.csv)
dotnet run --project src/Cli/Cli.csproj

# Запуск з власним файлом даних (CSV або JSON)
dotnet run --project src/Cli -- data/sample.csv
dotnet run --project src/Cli -- data/sample.json

```

---

## Варіанти Публікації (Self-Contained, Single-File, Trimming)

* **RID:** `win-x64`
* **Запуск бінарника:** `.\src\Cli\bin\Release\net8.0\win-x64\publish\Cli.exe` (або з папки `-o publish/...`)

### Команди публікації:

1. **Self-Contained (Автономна):**
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net8.0 -o publish/win-x64-sc

```


2. **Framework-Dependent (Залежна від .NET Runtime):**
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained false -f net8.0 -o publish/win-x64-fd

```


3. **Single-File (Збірка в один файл):**
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net8.0 -p:PublishSingleFile=true -o publish/win-x64-singlefile

```


4. **Single-File + Trimming (Один файл + очищення коду):**
```bash
dotnet publish src/Cli -c Release -r win-x64 --self-contained true -f net8.0 -p:PublishSingleFile=true -p:PublishTrimmed=true -o publish/win-x64-trimmed

```



---

## Порівняння режимів публікації (`win-x64`)

| Режим публікації | RID | Розмір (МБ) | Кількість файлів | Потрібен runtime? |
| --- | --- | --- | --- | --- |
| **Self-Contained** | `win-x64` | ~70.67 МБ | 705+ | Ні |
| **Framework-Dependent** | `win-x64` | ~0.17 МБ | 18 | Так (.NET 8/10) |
| **Single-File** | `win-x64` | ~64.41 МБ | 4 | Ні |
| **Single-File + Trimmed** | `win-x64` | ~11.77 МБ | 12 | Ні |

```

```