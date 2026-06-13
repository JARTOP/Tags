using TagScreenSystem.Models;

namespace TagScreenSystem;

// Сервис для управления экранами и графиками
public class ScreenService
{
    // Ссылка на сервис тегов, чтобы получать значения тегов
    private readonly TagService _tagService;
    
    // Список всех экранов (всего 12 штук)
    private List<Screen> _screens = new();
    
    // Генератор случайных чисел для создания экранов и выбора тегов
    private readonly Random _rand = new();
    
    // Количество отображаемых тегов на одном экране (по умолчанию 100)
    private int _numberOfDisplays = 100;

    // Конструктор - вызывается при создании сервиса
    public ScreenService(TagService tagService)
    {
        _tagService = tagService;           // Сохраняем ссылку на сервис тегов
        GenerateScreens();                  // Создаём 12 экранов
        StartAutoRefresh();                 // Запускаем автообновление графиков каждые 250 мс
    }

    // Свойство для доступа к количеству отображаемых тегов (от 10 до 500)
    public int NumberOfDisplays
    {
        get => _numberOfDisplays;
        set => _numberOfDisplays = Math.Clamp(value, 10, 500);  // Ограничиваем диапазон
    }

    // Свойство для доступа к Queue Size (прокси через TagService)
    public int QueueSize
    {
        get => _tagService.QueueSize;
        set => _tagService.QueueSize = value;
    }

    // Генерация всех экранов (12 штук, из них 5 графиков)
    private void GenerateScreens()
    {
        // Всегда 12 экранов как по заданию
        int screenCount = 12;
        // Всегда 5 графиков (первые 5 экранов будут графиками)
        int chartCount = 5;

        // Получаем все теги из TagService
        var allTags = _tagService.GetAllTags();

        // Создаём каждый экран по очереди
        for (int i = 0; i < screenCount; i++)
        {
            // Первые chartCount экранов - графики, остальные - таблицы
            bool isChart = i < chartCount;
            
            // Каждый экран содержит от 300 до 500 тегов (случайно)
            int tagCount = _rand.Next(300, 501);
            
            // Выбираем случайные теги для этого экрана
            var selectedTags = allTags.OrderBy(x => _rand.Next()).Take(tagCount).ToList();

            // Добавляем новый экран в список
            _screens.Add(new Screen
            {
                Id = i,     // ID экрана от 0 до 11
                Name = $"Экран {i + 1}" + (isChart ? " (График)" : " (Таблица)"),
                IsChart = isChart,  // true если график, false если таблица
                TagIds = selectedTags.Select(t => t.Id).ToList()  // Сохраняем только ID тегов
            });
        }

        Console.WriteLine($"✅ Создано {screenCount} экранов (из них {chartCount} графиков)");
    }

    // Запускает фоновый поток для автоматического обновления графиков
    private void StartAutoRefresh()
    {
        Task.Run(async () =>
        {
            while (true)  // Бесконечный цикл
            {
                await Task.Delay(250);  // Ждём 250 миллисекунд
                UpdateAllScreens();     // Обновляем все экраны-графики
            }
        });
    }

    // Обновляет данные всех экранов, которые являются графиками
    private void UpdateAllScreens()
    {
        foreach (var screen in _screens)
        {
            if (screen.IsChart)  // Только для графиков
            {
                UpdateChartData(screen);  // Обновляем данные графика
            }
        }
    }

    // Обновляет данные конкретного графика
    private void UpdateChartData(Screen screen)
    {
        // Получаем все теги, которые принадлежат этому экрану
        var tags = screen.TagIds.Select(id => _tagService.GetTag(id)).Where(t => t != null).ToList();

        // Очищаем старые данные графика
        screen.ChartData.Clear();

        // Для каждого тега создаём точку графика
        foreach (var tag in tags)
        {
            if (tag != null)
            {
                // Берём текущее значение тега
                double currentValue = _tagService.GetCurrentValue(tag);
                
                // Преобразуем значение в диапазон [-2; 0] согласно формуле из ТЗ
                // Значение тега от 0 до 100 превращается в Y от -2 до 0
                double mappedValue = -2 + (currentValue % 100) / 100 * 2;
                
                // Добавляем точку: X = время, Y = преобразованное значение
                screen.ChartData.Add((tag.Timestamp, mappedValue));
            }
        }

        // Оставляем только последние NumberOfDisplays точек
        screen.ChartData = screen.ChartData.TakeLast(_numberOfDisplays).ToList();
    }

    // Возвращает список всех экранов
    public List<Screen> GetScreens() => _screens;

    // Возвращает данные графика за указанный период времени (для поиска по дате)
    public List<(DateTime Time, double Value)> GetChartDataByDateRange(int screenId, DateTime from, DateTime to)
    {
        // Находим нужный экран
        var screen = _screens.FirstOrDefault(s => s.Id == screenId);
        if (screen == null || !screen.IsChart) return new List<(DateTime, double)>();

        // Собираем историю для всех тегов на экране
        var result = new List<(DateTime, double)>();
        foreach (var tagId in screen.TagIds)
        {
            var history = _tagService.GetHistory(tagId, from, to);
            result.AddRange(history);
        }
        
        // Убираем дубликаты и сортируем по времени (используем Item1 как время)
        return result.Distinct().OrderBy(x => x.Item1).ToList();
    }

    // Обновляет значение конкретного тега (вызывается из UI при редактировании)
    public void UpdateTagValue(string tagId, double newValue)
    {
        _tagService.UpdateTag(tagId, newValue);
    }

    // Принудительно обновляет данные графика по ID экрана
    public void RefreshScreen(int screenId)
    {
        var screen = _screens.FirstOrDefault(s => s.Id == screenId);
        if (screen != null && screen.IsChart)
            UpdateChartData(screen);
    }

    // Возвращает список тегов для отображения в таблице с дополнительной информацией
    public List<(Tag Tag, double MappedValue, int UpdateCount, int MaxUpdates, bool IsFast)> GetDisplayTags(Screen screen)
    {
        var result = new List<(Tag, double, int, int, bool)>();

        // Берём последние NumberOfDisplays тегов (или меньше если их не хватает)
        var recentTagIds = screen.TagIds.TakeLast(_numberOfDisplays).ToList();

        // Для каждого тега собираем информацию для отображения
        foreach (var tagId in recentTagIds)
        {
            var tag = _tagService.GetTag(tagId);
            if (tag != null)
            {
                // Текущее значение тега
                double currentValue = _tagService.GetCurrentValue(tag);
                
                // Преобразованное значение для графика (диапазон -2..0)
                double mappedValue = -2 + (currentValue % 100) / 100 * 2;
                
                // Информация о прогрессе обновлений
                var updateInfo = _tagService.GetUpdateInfo(tag);
                
                // Добавляем кортеж с данными: сам тег, Y-значение, текущее количество обновлений,
                // максимальное количество обновлений за 250 мс, флаг быстрого обновления
                result.Add((tag, mappedValue, updateInfo.Current, updateInfo.Max, tag.IsFastUpdate));
            }
        }

        return result;
    }
}