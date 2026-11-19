# Лабораторна робота 9

Команди виконуйте з каталогу CrossApp.

```powershell
dotnet run --project src/Cli -- config
dotnet run --project src/Cli -- export
dotnet run --project src/Cli -- import-json
dotnet test
```

Команда config показує активні параметри та факт наявності ApiKey. export зберігає всю поточну колекцію. Якщо файл ще відсутній, бере 18 демонстраційних примірників восьми видань. import-json [файл] перевіряє та замінює всю колекцію у налаштованому сховищі. Без аргументу вона читає налаштований файл. Команди import та import-stream із №8 продовжують працювати та розуміють новий формат.

## Профілі та ключі

| Ключ | Development | Production |
| --- | --- | --- |
| EnvironmentName | Development | Production |
| Storage:DataPath | data | data/production |
| Storage:FileName | library.json | library.json |
| Logging:MinimumLevel | Debug | Warning |
| Library:LoanDays | 14 | 14 |
| Integration:ApiKey | user-secrets або env | env |

Library:LoanDays використовується сервісом для видачі з типовим строком повернення. Рівні Debug і Trace показують додаткові діагностичні повідомлення в Cli. У Core пакети Microsoft.Extensions.Configuration не підключаються.

Відносний Storage:DataPath рахується від AppContext.BaseDirectory. Файл appsettings.json і профілі копіюються до каталогів збірки та публікації. Запуск з кореня solution, src/Cli або іншої папки дає той самий файл для тієї самої збірки.

Порядок пріоритету задається так — appsettings.json, профіль, user-secrets у Development, змінні CROSSAPP_, аргументи конфігурації. Проста CROSSAPP_DATA_PATH перевизначає Storage:DataPath останньою, включно з аргументом командного рядка. Профіль обирається через CROSSAPP_ENVIRONMENT або EnvironmentName.

## Приклади для PowerShell

```powershell

зміна профілю:
$env:CROSSAPP_ENVIRONMENT = "Production"
dotnet run --project src/Cli -- config
Remove-Item Env:CROSSAPP_ENVIRONMENT


зміна папки:
$env:CROSSAPP_Storage__DataPath = "demo-data"
dotnet run --project src/Cli -- config
Remove-Item Env:CROSSAPP_Storage__DataPath

$env:CROSSAPP_DATA_PATH = "demo-data"
dotnet run --project src/Cli -- export
dotnet run --project src/Cli -- import-json
Remove-Item Env:CROSSAPP_DATA_PATH

dotnet run --project src/Cli -- config --Library:LoanDays=21 --Storage:DataPath=demo-data
```

## Приклади для bash

```bash
CROSSAPP_ENVIRONMENT=Production dotnet run --project src/Cli -- config
CROSSAPP_Storage__DataPath=demo-data dotnet run --project src/Cli -- config
CROSSAPP_DATA_PATH=demo-data dotnet run --project src/Cli -- export
dotnet run --project src/Cli -- config --Library:LoanDays=21
```

## User-secrets

У Cli.csproj є UserSecretsId. Навчальний ключ можна задати або замінити командою нижче.

```powershell
dotnet user-secrets set "Integration:ApiKey" "local-dev-key" --project src/Cli
dotnet user-secrets list --project src/Cli
dotnet run --project src/Cli -- config

$env:CROSSAPP_Integration__ApiKey = "from-env"
dotnet run --project src/Cli -- config
Remove-Item Env:CROSSAPP_Integration__ApiKey
```

Застосунок друкує лише факт наявності й довжину. На Windows secrets.json лежить у %APPDATA%/Microsoft/UserSecrets/<UserSecretsId>/secrets.json, у Linux і macOS — ~/.microsoft/usersecrets/<UserSecretsId>/secrets.json. У репозиторії зберігається ідентифікатор. Значення лишається в профілі ОС. У Production user-secrets не підключаються.

## Формат та збереження JSON

Кожне видання має isbn, title, author, year та масив copies. Примірники зберігають власні ID і isIssued, тому експорт та повторний імпорт відновлюють стан видачі. Старі плоскі JSON-файли лабораторних №5–8 також читаються. Рік 0 означає, що старий запис не містив року.

LibraryJson використовує camelCase, форматування та читабельну кирилицю. Запис створює окремий файл із суфіксом .tmp у тому самому каталозі, завершує запис і виконує File.Move з overwrite. При помилці тимчасовий файл прибирається. FileBookStore оновлює кеш після успішного запису.

Runtime-каталоги data та локальні secrets.json ігноруються Git. Існуючі файли прикладів у кореневій data залишаються відстежуваними з попередніх робіт. Під час збірки потрібні приклади копіюються в samples поруч із застосунком.

## Перевірки

40 тестових сценаріїв охоплюють попередню логіку, збереження й відновлення метаданих, старий формат, шляхи, атомарний запис і налаштований строк видачі. Тести створюють власні тимчасові каталоги та прибирають їх після виконання.

Перевірка однакового шляху з двох каталогів у PowerShell.

```powershell
dotnet run --project src/Cli -- config
Push-Location src/Cli
dotnet run -- config
Pop-Location
```

## Лабораторна робота №8

Робочий каталог цієї версії — `CrossApp`. Команди нижче виконуйте з нього.

```powershell
dotnet run --project src/Cli -- import
dotnet run --project src/Cli -- import-stream
dotnet run --project src/Cli -- import data/library-invalid.json
dotnet run --project src/Cli -- import --timeout-ms 0
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

### Асинхронний імпорт

`IBookImporter` визначає методи `LoadAsync` та `StreamAsync`. `JsonBookImporter` відкриває `FileStream` з `useAsync: true` і читає JSON-масив через `DeserializeAsyncEnumerable`. Кожен DTO перетворюється на `BookCopy` з перевіркою інваріантів. `LoadAsync` накопичує перевірені записи перед передаванням у сервіс, тому помилка даних під час читання залишає сховище без нових записів.

`LendingService.ImportAsync` приймає імпортер через параметр і додає перевірені примірники. `ImportStreamAsync` обробляє `IAsyncEnumerable<BookCopy>` через `await foreach`, зберігаючи записи по одному. Подія `Changed` піднімається один раз після кожного успішного додавання. ID порівнюються правилами сховища. Повторний імпорт пропускає наявні ID та зберігає їхній попередній стан.

За замовчуванням імпорт використовує порожнє сховище в пам’яті та файл `data/library-import.json` із 18 примірниками. Прапорець `--file` вибирає файлове сховище з попередніх робіт, яке зберігає дані між запусками.

```powershell
dotnet run --project src/Cli -- import data/library-import.json --file
dotnet run --project src/Cli -- import data/library-import.json --file
```

Другий запуск додає 0 записів, якщо перший уже додав усі ID. Файл сховища розташований у `src/Cli/bin/Debug/net8.0/data/library.json` для звичайного запуску Debug.

### Скасування та помилки

`CancellationToken` передається з Cli через сервіс до імпортера, файлового читання та JSON-десеріалізації. У циклах перевіряється `ThrowIfCancellationRequested`. Ctrl+C надсилає сигнал через користувацький токен, а `--timeout-ms N` задає таймаут у мілісекундах. `CreateLinkedTokenSource` об’єднує обидва джерела.

`--timeout-ms 0` відтворювано демонструє повідомлення «Імпорт скасовано.» та причину «таймаут». Для Ctrl+C запускайте потоковий імпорт великого JSON-файлу й натисніть Ctrl+C під час обробки. Невеликий приклад із 18 записами може завершитися раніше.

Щоб створити великий файл для демонстрації, виконайте в PowerShell наведені команди.

```powershell
$items = 1..100000 | ForEach-Object {
    @{ Id = "LARGE-$_"; Isbn = "ISBN-$_"; Title = "Книга $_" }
}
$items | ConvertTo-Json -Depth 3 | Set-Content data/library-large.json -Encoding utf8
dotnet run --project src/Cli -- import-stream data/library-large.json --timeout-ms 20
```

Під час скасування або помилки потокового імпорту вже збережені записи залишаються у сховищі. Cli показує їхню кількість за поточний запуск. Для продовження повторіть імпорт із `--file`, використовуючи те саме файлове сховище. У режимі в пам’яті кожен новий запуск створює нове сховище. Коди завершення дорівнюють 0 для успіху, 1 для помилки та 2 для скасування.

`DomainException` описує бізнес-помилки, наприклад повторну видачу примірника. Він успадковує `InvalidOperationException`, зберігаючи сумісність із наявними обробниками. Некоректні аргументи фабрик залишаються `ArgumentException`. Під час імпорту такі помилки даних обгортаються в `DomainException` із номером запису та `InnerException`. Технічні `JsonException`, `IOException` і помилки доступу обробляються окремо в Cli.

### Додаткові завдання

1. Потокове читання реалізовано в `StreamAsync` через `IAsyncEnumerable<BookCopy>`, `await foreach` та `[EnumeratorCancellation]`.
2. Зв’язані токени користувача й таймауту створюються в Cli. Тест `Import_WithLinkedTokens_IsCancelledByTimeoutOnly` очікує початку операції, запускає таймаут і перевіряє, що користувацький токен лишився активним.
3. `IProgress<int>` передає відсотки в Cli. Звичайний імпорт показує частку опрацьованих примірників на етапі збереження. Потоковий оцінює прочитані байти, тому буферизація може давати великі стрибки. 100% повідомляється після завершення.
4. Покриття збирається командою `dotnet test --collect:"XPlat Code Coverage"`. Звіт `coverage.cobertura.xml` з’являється у підкаталозі `TestResults`. Відсоток і межі покриття наведені у звіті Word №8.

Тести використовують власні фіксовані дані та нове сховище в пам’яті для кожного сценарію. Перевірки JSON працюють із тимчасовим файлом, який видаляється в `finally`. Проєкт тестів посилається лише на Core. Звіти №7 можна запускати їхніми підкомандами, наприклад `report-top`.

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

## Лабораторна робота №4 — Доменна модель

У цій роботі до проєкту додано доменні сутності, які контролюють
коректність даних і не дозволяють виконувати недопустимі операції.

### Створені сутності

- `BookCopy` — примірник книги.
- `Reader` — читач бібліотеки.
- `Loan` — видача книги.
- `LibraryService` — сервіс для координації видач і повернень.
- `Order` — замовлення для додаткового завдання.
- `OrderLine` — рядок замовлення.

### Інваріанти

1. Ідентифікатор примірника не може бути порожнім.
2. ISBN примірника не може бути порожнім.
3. Назва книги не може бути порожньою.
4. Не можна повторно видати вже виданий примірник.
5. Не можна повернути невиданий примірник.
6. Ідентифікатор читача не може бути порожнім.
7. Ідентифікатор видачі не може бути порожнім.
8. Дата повернення не може передувати даті видачі.
9. Не можна повторно закрити видачу.
10. Читач не може мати більше п'яти відкритих видач.
11. Кількість товару в рядку замовлення повинна бути більшою за нуль.
12. Ціна товару не може бути від'ємною.
13. Не можна додавати товари до підтвердженого або скасованого замовлення.
14. Не можна підтверджувати порожнє замовлення.
15. Не можна повторно підтверджувати підтверджене замовлення.

### Інкапсуляція

Для створення сутностей використовуються фабричні методи
`Create` та `Open`.

Конструктори сутностей закриті від зовнішнього коду.

Критичні властивості не мають публічних сеттерів.
Зміна стану відбувається через доменні методи.

### DTO

DTO використовуються для передавання та збереження даних.
Доменні сутності відповідають за поведінку й перевірку інваріантів.

Для `BookCopy`, `Reader` і `Loan` реалізовано методи
`ToDto()` та `FromDto()`.

### Додаткові завдання

1. Реалізовано перетворення результату імпорту на доменні сутності
   зі збереженням помилок.
2. Додано перевірку ліміту відкритих видач читача.
3. Реалізовано `OrderStatus` і перевірку переходів стану через
   `switch expression`.












## Лабораторна робота №6 — Доменні події, делегати та лямбди

У роботі реалізовано механізм сповіщення про зміни стану бібліотеки
за допомогою подій.

### Основні зміни

- `CatalogChangeKind` — типи подій бібліотеки.
- `CatalogChangedEventArgs` — дані про подію.
- `LendingService` — генерує події після успішних операцій.
- `Changed` — подія для підписників.
- `OnChanged` — метод виклику події.
- `FindAll` — пошук книг за умовою через `Func`.

### Підписники

У Cli реалізовано два підписники:
1. Локальний метод для виведення повідомлень у консоль і файл.
2. Лямбда-вираз для підрахунку кількості подій.

Також реалізовано відписку від події через `-=`.

### Додаткові завдання

1. Створено `INotificationSink`, `ConsoleSink` та `FileSink`
   для демонстрації ручного Dependency Injection.
2. Додано `Action<string>` до CSV-імпортера для повідомлення
   про пропущені рядки.
3. Розглянуто особливості та недоліки асинхронних обробників подій `async void`.

## Лабораторна робота №7 — Звіти бібліотеки через LINQ

Запускайте команди з каталогу `CrossApp`.

```powershell
dotnet run --project src/Cli -- report-top
dotnet run --project src/Cli -- report-overdue
dotnet run --project src/Cli -- report-groups
dotnet run --project src/Cli -- report-sum
dotnet run --project src/Cli -- report-editions
dotnet run --project src/Cli -- report-deferred
dotnet run --project src/Cli -- report-benchmark
```

| Команда | Результат |
| --- | --- |
| `report-top` | П’ять читачів із найбільшою кількістю видач. За однакової кількості порядок визначає ID читача. |
| `report-overdue` (також `report-low`) | Відкриті видачі, строк повернення яких минув. |
| `report-groups` | Кількість видань і примірників за роками. |
| `report-sum` | Кількість видань і примірників, середнє та максимум примірників на одне видання. |
| `report-editions` | П’ять найпопулярніших видань за кількістю видач. За однакової кількості порядок визначає ISBN. |
| `report-deferred` | Повторний перебір запиту після зміни джерела та перевірка знімка. |
| `report-benchmark` | Вимірювання часу звіту на 100 000 згенерованих видачах із проміжним `ToList()` і без нього. |

У `SampleData` є 18 примірників восьми видань за чотири роки й окрема демонстраційна історія видач. Строки повернення в ній задаються відносно дати запуску. Прапорець `--file` до або після команди вибирає наявне файлове сховище примірників. Воно зберігає ISBN, але не рік видання, тому звіт підставляє рік для відомих ISBN, а решту відносить до групи «Невідомий». Історія видач та строки повернення у цьому файлі відсутні. Без команди звіту CLI запускає попередні демонстрації.

У `report-deferred` виклик `Where` створює запит і залишає перебір на потім. Перший `Count()` знаходить три прострочені видачі, а другий перебирає змінений список і знаходить чотири. `ToList()` фіксує чотири результати в окремому списку. Після очищення джерела запит повертає нуль, а знімок усе ще містить чотири результати.

`ByPublicationYear` використовує синтаксис запитів `from … group … into … orderby … select`, у якому групування та проєкцію видно як окремі кроки. У методному синтаксисі еквівалентний ланцюжок має вигляд `GroupBy(...).OrderBy(...).ThenBy(...).Select(...)`. Методний запис зручний для топ-N, де після сортування потрібні `Take` і `ToList`. `TopReaders` і `TopEditions` використовують спільне розширення `TopBy`, а `TopEditions` пов’язує примірники з видачами через `Join` і `GroupJoin`. Для кожного варіанта вимірювання виводиться медіана семи замірів після генерації даних і прогріву. Час залежить від середовища виконання.
