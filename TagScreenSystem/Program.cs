using TagScreenSystem;
using TagScreenSystem.Models;

// Главный класс программы
class Program
{
    // Сервис для работы с тегами - хранит 2000 тегов и обновляет их
    static TagService? _tagService;
    
    // Сервис для работы с экранами - хранит 12 экранов и графики
    static ScreenService? _screenService;
    
    // Текущий пользователь: Admin или Operator. Admin может редактировать теги
    static string _currentUser = "Operator";
    
    // ID экрана который сейчас отображается в LIVE режиме
    static int _currentScreenId = -1;
    
    // Флаг: true когда включен LIVE режим обновления таблицы
    static bool _liveMode = false;
    
    // Отмена фонового потока при выходе из LIVE режима
    static CancellationTokenSource? _liveCts;

    // Точка входа в программу
    static void Main()
    {
        // Устанавливаем заголовок окна консоли
        Console.Title = "Tag & Screen System";
        
        // Включаем поддержку UTF-8 для русского языка и эмодзи
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Создаём сервисы. TagService генерирует 2000 тегов. ScreenService создаёт 12 экранов.
        _tagService = new TagService();
        _screenService = new ScreenService(_tagService);

        // Выводим приветствие и информацию о системе
        Console.WriteLine("TAG & SCREEN SYSTEM v2.0");
        Console.WriteLine($"Тегов: {_tagService.GetAllTags().Count} | Queue Size: {_screenService.QueueSize}\n");

        // Основной цикл программы. Работает пока пользователь не нажмёт 0.
        while (true)
        {
            // Если не в LIVE режиме - показываем меню
            if (!_liveMode) ShowMenu();
            
            // Ждём ввод пользователя
            var input = Console.ReadLine()?.Trim().ToLower();

            // Обрабатываем ввод в зависимости от режима
            if (_liveMode)
            {
                HandleLiveInput(input);
            }
            else
            {
                HandleInput(input);
            }
        }
    }

    // Отображает главное меню с пунктами 0-7
    static void ShowMenu()
    {
        Console.WriteLine($"\nПользователь: {_currentUser}");
        Console.WriteLine("1. Показать все экраны");
        Console.WriteLine("2. Выбрать экран (LIVE таблица)");
        Console.WriteLine("3. Просмотр графиков (статика)");
        Console.WriteLine("4. Поиск на графике по дате");
        Console.WriteLine("5. Установить параметры");
        Console.WriteLine("6. Сменить пользователя");
        if (_currentUser == "Admin")
            Console.WriteLine("7. Редактировать тег");
        Console.WriteLine("0. Выход");
        Console.Write("\nВаш выбор: ");
    }

    // Обрабатывает команды в обычном режиме (цифры от 0 до 7)
    static void HandleInput(string? input)
    {
        switch (input)
        {
            case "1": ShowScreens(); break;
            case "2": SelectScreenAndViewTable(); break;
            case "3": ShowAllChartsStatic(); break;
            case "4": SearchChartByDate(); break;
            case "5": SetQueueSize(); break;
            case "6": ChangeUser(); break;
            case "7": if (_currentUser == "Admin") EditTag(); else Invalid(); break;
            case "0": Environment.Exit(0); break;
            default: Invalid(); break;
        }
    }

    // Обрабатывает команды в LIVE режиме (Q, +, -, *, /)
    static void HandleLiveInput(string? input)
    {
        if (input == "q")
        {
            StopLiveMode();
        }
        else if (input == "+")
        {
            var newSize = _screenService!.QueueSize + 1;
            if (newSize <= 20)
            {
                _screenService.QueueSize = newSize;
                Console.WriteLine($"Queue Size увеличен до: {_screenService.QueueSize}");
            }
        }
        else if (input == "-")
        {
            var newSize = _screenService!.QueueSize - 1;
            if (newSize >= 1)
            {
                _screenService.QueueSize = newSize;
                Console.WriteLine($"Queue Size уменьшен до: {_screenService.QueueSize}");
            }
        }
        else if (input == "*")
        {
            var newDisplays = _screenService!.NumberOfDisplays + 50;
            _screenService.NumberOfDisplays = Math.Min(500, newDisplays);
            Console.WriteLine($"Number of Displays увеличен до: {_screenService.NumberOfDisplays}");
        }
        else if (input == "/")
        {
            var newDisplays = _screenService!.NumberOfDisplays - 50;
            _screenService.NumberOfDisplays = Math.Max(10, newDisplays);
            Console.WriteLine($"Number of Displays уменьшен до: {_screenService.NumberOfDisplays}");
        }
    }

    // Пункт меню 1. Показывает таблицу всех 12 экранов с их параметрами
    static void ShowScreens()
    {
        var screens = _screenService!.GetScreens();
        Console.WriteLine($"\nВсего экранов: {screens.Count} (12 всего, из них 5 графиков)");
        Console.WriteLine($"Queue Size (порог обновлений): {_screenService.QueueSize}");
        Console.WriteLine($"Number of Displays (отображаемых тегов): {_screenService.NumberOfDisplays}\n");
        Console.WriteLine("ID | Название                         | Тегов | Тип      | Формат");
        Console.WriteLine("---|----------------------------------|-------|----------|-----------------");
        foreach (var s in screens)
        {
            string format = s.IsChart ? "[X;Y] столбиком" : "Таблица";
            Console.WriteLine($"{s.Id,2} | {s.Name,-32} | {s.TagIds.Count,5} | {(s.IsChart ? "График" : "Таблица"),8} | {format,-15}");
        }
        Console.WriteLine("\nИнформация:");
        Console.WriteLine("   • Экраны 0-4: графики в формате [X;Y] (каждая точка на новой строке)");
        Console.WriteLine("   • Экран 5-11: таблицы");
    }

    // Пункт меню 2. Пользователь выбирает табличный экран и включает LIVE режим
    static void SelectScreenAndViewTable()
    {
        var screens = _screenService!.GetScreens();
        var tableScreens = screens.Where(s => !s.IsChart).ToList();

        if (tableScreens.Count == 0)
        {
            Console.WriteLine("Нет доступных табличных экранов");
            return;
        }

        Console.WriteLine("\nДоступные табличные экраны (7 штук):");
        foreach (var s in tableScreens)
        {
            Console.WriteLine($"   ID: {s.Id} - {s.Name} (тегов: {s.TagIds.Count})");
        }

        Console.Write("\nВведите ID экрана: ");
        if (!int.TryParse(Console.ReadLine(), out int screenId)) return;

        var screen = tableScreens.FirstOrDefault(s => s.Id == screenId);
        if (screen == null)
        {
            Console.WriteLine("Экран не найден");
            return;
        }

        _currentScreenId = screenId;
        _liveMode = true;
        _liveCts = new CancellationTokenSource();

        Console.Clear();
        Console.WriteLine($"ТАБЛИЧНЫЙ РЕЖИМ (LIVE): {screen.Name}");
        Console.WriteLine($"Number of Displays: {_screenService.NumberOfDisplays} | Queue Size: {_screenService.QueueSize}");
        Console.WriteLine("Команды: Q - выход | +/- изменить Queue Size | */ - изменить NumberOfDisplays\n");

        Task.Run(() => LiveTableUpdateLoop(_liveCts.Token, screenId));
    }

    // Фоновый поток для обновления LIVE таблицы каждые 250 мс
    static void LiveTableUpdateLoop(CancellationToken token, int screenId)
    {
        while (!token.IsCancellationRequested)
        {
            if (_screenService != null)
            {
                var screen = _screenService.GetScreens().FirstOrDefault(s => s.Id == screenId);
                if (screen != null)
                {
                    lock (Console.Out)
                    {
                        Console.SetCursorPosition(0, 5);
                        DrawTable(screen, _screenService.NumberOfDisplays, _screenService.QueueSize);
                    }
                }
            }
            Thread.Sleep(250);
        }
    }

    // Рисует таблицу с тегами: ID, тип, значение, Y на графике, прогресс-бар
    static void DrawTable(Screen screen, int numberOfDisplays, int queueSize)
    {
        var displayTags = _screenService!.GetDisplayTags(screen);

        Console.WriteLine($"Обновлено: {DateTime.Now:HH:mm:ss.fff}");
        Console.WriteLine($"Queue Size (порог): {queueSize} | Number of Displays: {numberOfDisplays}");
        Console.WriteLine($"Быстрые теги (17мс): 30 шт | Обычные теги (50мс): 1970 шт\n");

        Console.WriteLine("--- | ------------------- | ------ | -------- | ---------- | -------- | -----------");
        Console.WriteLine(" №  | ID тега             | Тип     | Значение | Mapped Y   | Namespace | Прогресс");
        Console.WriteLine("--- | ------------------- | ------ | -------- | ---------- | -------- | -----------");

        int index = 1;
        int maxRows = Math.Min(25, displayTags.Count);

        for (int i = 0; i < maxRows; i++)
        {
            var item = displayTags[i];
            string typeIcon = item.Tag.Type == "float" ? "float" : "int";
            string speedIcon = item.IsFast ? "fast" : "slow";
            string progressBar = GetProgressBar(item.UpdateCount, item.MaxUpdates, queueSize);

            Console.WriteLine($"{index,3} | {speedIcon} {item.Tag.Id,-18} | {typeIcon,-5} | {item.Tag.Value,8:F2} | {item.MappedValue,10:F3} | {item.Tag.Namespace,8} | {progressBar}");
            index++;
        }

        if (displayTags.Count > maxRows)
        {
            Console.WriteLine("... | ...                 | ...    | ...     | ...        | ...      | ...");
        }

        Console.WriteLine("\nЛегенда: fast=17мс slow=50мс");
        Console.WriteLine($"Команды: Q - выход | + увеличить Queue | - уменьшить Queue | * увеличить Displays | / уменьшить Displays");
        Console.Write($"\nВведите номер тега: ");
    }

    // Создаёт прогресс-бар: показывает сколько обновлений сделано из нужного количества
    static string GetProgressBar(int current, int max, int queueSize)
    {
        int target = Math.Min(queueSize, max);
        int filled = Math.Min(current, target);
        int percent = target > 0 ? (int)((double)filled / target * 100) : 0;

        int barLength = 10;
        int filledBars = target > 0 ? (int)((double)filled / target * barLength) : 0;

        if (filled >= target && target > 0)
            return new string('█', barLength) + $" {percent,3}% ✓";
        else if (filled > 0)
            return new string('█', filledBars) + new string('░', barLength - filledBars) + $" {percent,3}%";
        else
            return new string('░', barLength) + $" {percent,3}%";
    }

    // Пункт меню 3. Показывает все 5 графиков в статическом режиме (без обновления)
    static void ShowAllChartsStatic()
    {
        var screens = _screenService!.GetScreens();
        var chartScreens = screens.Where(s => s.IsChart).ToList();

        Console.Clear();
        Console.WriteLine("ВСЕ ГРАФИКИ (5 шт)");
        Console.WriteLine("");

        for (int i = 0; i < chartScreens.Count; i++)
        {
            var screen = chartScreens[i];
            Console.WriteLine($"ГРАФИК {i + 1}: {screen.Name}");
            Console.WriteLine("");

            _screenService.RefreshScreen(screen.Id);

            if (screen.ChartData.Count == 0)
            {
                Console.WriteLine("Нет данных для отображения");
            }
            else
            {
                DrawChartAsPointsVertical(screen.ChartData, screen.TagIds.Count);
            }
            Console.WriteLine("");
        }

        Console.WriteLine("Нажмите любую клавишу для продолжения...");
        Console.ReadKey();
    }

    // Выводит точки графика в формате [X; Y] на каждой строке
    static void DrawChartAsPointsVertical(List<(DateTime Time, double Value)> data, int tagCount)
    {
        if (data.Count == 0)
        {
            Console.WriteLine("Нет данных для отображения");
            return;
        }

        var displayData = data.TakeLast(30).ToList();

        Console.WriteLine("");
        Console.WriteLine("ТОЧКИ ГРАФИКА [X;Y]:");
        Console.WriteLine("");

        for (int i = 0; i < displayData.Count; i++)
        {
            Console.WriteLine($"  {i + 1,2}.  [{displayData[i].Time:yyyy-MM-dd HH:mm:ss.fff}; {displayData[i].Value,8:F3}]");
        }

        Console.WriteLine("");

        var values = displayData.Select(d => d.Value).ToList();
        Console.WriteLine($"СТАТИСТИКА:");
        Console.WriteLine($"     Всего точек: {displayData.Count,3} | Тегов на экране: {tagCount,3}");
        Console.WriteLine($"     Минимум: {values.Min(),8:F3}");
        Console.WriteLine($"     Максимум: {values.Max(),8:F3}");
        Console.WriteLine($"     Среднее: {values.Average(),8:F3}");
    }

    // Пункт меню 4. Поиск значения на графике по дате
    static void SearchChartByDate()
    {
        var screens = _screenService!.GetScreens();
        var chartScreens = screens.Where(s => s.IsChart).ToList();

        if (chartScreens.Count == 0)
        {
            Console.WriteLine("Нет доступных графиков");
            return;
        }

        Console.Clear();
        Console.WriteLine("ПОИСК ЗНАЧЕНИЙ ПО ДАТЕ");
        Console.WriteLine("");

        Console.WriteLine("\nДоступные графики:");
        foreach (var s in chartScreens)
        {
            Console.WriteLine($"   ID: {s.Id} - {s.Name} (тегов: {s.TagIds.Count})");
        }

        Console.Write("\nВведите ID графика (0-4): ");
        if (!int.TryParse(Console.ReadLine(), out int id)) return;

        var screen = chartScreens.FirstOrDefault(s => s.Id == id);
        if (screen == null)
        {
            Console.WriteLine("График не найден");
            Console.ReadKey();
            return;
        }

        _screenService.RefreshScreen(screen.Id);

        Console.Write("Введите дату для поиска (ГГГГ-ММ-ДД ЧЧ:ММ:СС): ");
        if (!DateTime.TryParse(Console.ReadLine(), out DateTime searchDate))
        {
            Console.WriteLine("Неверный формат даты");
            Console.ReadKey();
            return;
        }

        var closest = FindClosestValue(screen.ChartData, searchDate);

        Console.WriteLine($"\nРЕЗУЛЬТАТ ПОИСКА");
        Console.WriteLine("");
        Console.WriteLine($"  НАЙДЕННАЯ ТОЧКА:");
        Console.WriteLine("");
        Console.WriteLine($"     ДАТА: {closest.Time:yyyy-MM-dd HH:mm:ss.fff}");
        Console.WriteLine($"     ЗНАЧЕНИЕ: {closest.Value:F3}");
        Console.WriteLine("");
        Console.WriteLine($"  Искомая дата: {searchDate:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  Отклонение: {Math.Abs((closest.Time - searchDate).TotalMilliseconds):F0} мс");
        Console.WriteLine("");

        ShowNeighborPointsVertical(screen.ChartData, closest.Time);

        Console.WriteLine("\nНажмите любую клавишу для продолжения...");
        Console.ReadKey();
    }

    // Показывает соседние точки графика (3 слева и 3 справа от найденной)
    static void ShowNeighborPointsVertical(List<(DateTime Time, double Value)> data, DateTime centerTime)
    {
        int centerIndex = data.FindIndex(d => d.Time == centerTime);
        if (centerIndex == -1)
        {
            Console.WriteLine("\n   Не удалось найти соседние точки");
            return;
        }

        int startIndex = Math.Max(0, centerIndex - 3);
        int endIndex = Math.Min(data.Count - 1, centerIndex + 3);
        var neighbors = data.Skip(startIndex).Take(endIndex - startIndex + 1).ToList();

        Console.WriteLine($"\nСОСЕДНИЕ ТОЧКИ ГРАФИКА");
        Console.WriteLine("");

        for (int i = 0; i < neighbors.Count; i++)
        {
            string marker = (i == neighbors.Count / 2) ? "->" : "  ";
            Console.WriteLine($"  {marker} {i + 1}. ДАТА: {neighbors[i].Time:yyyy-MM-dd HH:mm:ss.fff}");
            Console.WriteLine($"      ЗНАЧЕНИЕ: {neighbors[i].Value,8:F3}");
            Console.WriteLine("");
        }

        Console.WriteLine($"  -> - искомая точка");
    }

    // Находит точку графика с минимальным отклонением по времени от искомой даты
    static (DateTime Time, double Value) FindClosestValue(List<(DateTime Time, double Value)> data, DateTime targetDate)
    {
        if (data.Count == 0)
            return (DateTime.Now, 0);

        var closest = data.OrderBy(d => Math.Abs((d.Time - targetDate).TotalSeconds)).First();
        return closest;
    }

    // Останавливает LIVE режим: отменяет фоновый поток и очищает экран
    static void StopLiveMode()
    {
        _liveCts?.Cancel();
        _liveMode = false;
        _currentScreenId = -1;
        Console.Clear();
        Console.WriteLine("LIVE режим остановлен.\n");
    }

    // Пункт меню 5. Настройка Queue Size (1-20) и NumberOfDisplays (10-500)
    static void SetQueueSize()
    {
        Console.Clear();
        Console.WriteLine("НАСТРОЙКА ПАРАМЕТРОВ");
        Console.WriteLine("");
        Console.WriteLine($"Текущий Queue Size (порог): {_screenService!.QueueSize}");
        Console.WriteLine($"Текущий Number of Displays: {_screenService.NumberOfDisplays}");
        Console.WriteLine("\nПояснение:");
        Console.WriteLine("• Queue Size (1-20) - сколько обновлений нужно для ✓");
        Console.WriteLine("• Number of Displays (10-500) - сколько тегов на экране");
        Console.WriteLine("\nВведите новые значения через пробел (QueueSize NumberOfDisplays)");
        Console.Write("Пример: '5 100' или только '10': ");

        var input = Console.ReadLine()?.Split(' ');
        if (input != null && input.Length >= 1 && int.TryParse(input[0], out int queueSize))
        {
            if (queueSize >= 1 && queueSize <= 20)
                _screenService.QueueSize = queueSize;
            else
                Console.WriteLine("Queue Size должен быть от 1 до 20");
        }

        if (input != null && input.Length >= 2 && int.TryParse(input[1], out int displays))
        {
            if (displays >= 10 && displays <= 500)
                _screenService.NumberOfDisplays = displays;
            else
                Console.WriteLine("Number of Displays должен быть от 10 до 500");
        }

        Console.WriteLine($"\nНовые настройки:");
        Console.WriteLine($"   Queue Size: {_screenService.QueueSize}");
        Console.WriteLine($"   Number of Displays: {_screenService.NumberOfDisplays}");
        Console.WriteLine("\nНажмите любую клавишу...");
        Console.ReadKey();
    }

    // Пункт меню 6. Переключение между Admin и Operator
    static void ChangeUser()
    {
        Console.Write("Выберите пользователя (Admin/Operator): ");
        var input = Console.ReadLine()?.Trim();
        if (input == "Admin" || input == "Operator")
        {
            _currentUser = input;
            Console.WriteLine($"Пользователь сменён на: {_currentUser}");
        }
        else
        {
            Console.WriteLine("Неверное имя пользователя");
        }
    }

    // Пункт меню 7. Редактирование тега (только для Admin)
    static void EditTag()
    {
        if (_currentUser != "Admin")
        {
            Console.WriteLine("Доступ запрещён. Только для Admin");
            return;
        }

        var tags = _tagService!.GetAllTags();
        Console.WriteLine("\nСписок тегов (первые 20):");
        Console.WriteLine("-------------------|----------|----------|--------");
        Console.WriteLine("ID                  | Тип      | Значение | Namespace");
        Console.WriteLine("-------------------|----------|----------|--------");
        foreach (var t in tags.Take(20))
        {
            Console.WriteLine($"{t.Id,-19} | {t.Type,-8} | {t.Value,8:F2} | {t.Namespace,6}");
        }
        Console.WriteLine("-------------------|----------|----------|--------");

        Console.Write("\nВведите ID тега для редактирования: ");
        var id = Console.ReadLine();
        if (string.IsNullOrEmpty(id))
        {
            Console.WriteLine("Неверный ID");
            return;
        }

        Console.Write("Новое значение (число): ");
        if (double.TryParse(Console.ReadLine(), out double val))
        {
            _screenService!.UpdateTagValue(id, val);
            Console.WriteLine("Тег обновлён");
        }
        else
        {
            Console.WriteLine("Неверное значение");
        }
    }

    // Вызывается при неверном вводе
    static void Invalid()
    {
        Console.WriteLine("Неверный ввод");
    }
}

// Вспомогательный класс для повторения символа N раз (нужен для прогресс-баров)
public static class StringExtensions
{
    public static string Repeat(this string str, int count)
    {
        if (count <= 0) return "";
        return new string(str[0], count);
    }
}