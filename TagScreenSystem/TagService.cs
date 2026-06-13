using System.Collections.Concurrent;
using System.Text.Json;
using TagScreenSystem.Models;

namespace TagScreenSystem;

// Сервис для управления тегами - основное ядро системы
public class TagService
{
    // Потокобезопасное хранилище всех тегов (2000 штук)
    private readonly ConcurrentDictionary<string, Tag> _tags = new();
    
    // Путь к файлу базы данных (в папке Database)
    private readonly string _dbPath = "Database/tags.txt";
    
    // Генератор случайных чисел для симуляции значений тегов
    private readonly Random _rand = new();
    
    // Токен для отмены фоновых задач при остановке сервиса
    private CancellationTokenSource? _cts;
    
    // Размер очереди - сколько обновлений нужно для достижения порога (1-20)
    private int _queueSize = 5;

    // Конструктор - создаёт базу, загружает данные и запускает обновления
    public TagService()
    {
        Directory.CreateDirectory("Database");      // Создаём папку для БД если её нет
        LoadFromDatabase();                         // Загружаем сохранённые теги
        GenerateTags();                             // Генерируем недостающие теги (2000 штук)
        StartUpdates();                             // Запускаем фоновые потоки обновления
    }

    // Свойство для доступа к Queue Size (от 1 до 20)
    public int QueueSize
    {
        get => _queueSize;
        set => _queueSize = Math.Clamp(value, 1, 20);
    }

    // Генерация 2000 тегов (30 быстрых по 17мс и 1970 обычных по 50мс)
    private void GenerateTags()
    {
        // Если уже есть 2000 тегов - ничего не делаем
        if (_tags.Count >= 2000) return;

        // Очищаем существующие теги
        _tags.Clear();

        // Создаём 30 быстрых тегов с интервалом обновления 17 мс
        for (int i = 0; i < 30; i++)
        {
            var tag = new Tag
            {
                Id = $"fast_tag_{i}",                           // Уникальный ID
                Type = _rand.Next(2) == 0 ? "float" : "int",    // Случайный тип данных
                Value = _rand.NextDouble() * 100,               // Случайное значение от 0 до 100
                Namespace = _rand.Next(1, 100),                 // Случайное пространство имён
                NodeId = $"fast_node_{_rand.Next(1, 50)}",      // Случайный ID узла
                Timestamp = DateTime.Now,                       // Время создания
                UpdateIntervalMs = 17,                          // 17 миллисекунд
                IsFastUpdate = true,                            // Флаг быстрого обновления
                UpdateCounter = 0                               // Счётчик обновлений
            };
            _tags.TryAdd(tag.Id, tag);
        }

        // Создаём 1970 обычных тегов с интервалом обновления 50 мс
        for (int i = 0; i < 1970; i++)
        {
            var tag = new Tag
            {
                Id = $"tag_{i}",
                Type = _rand.Next(2) == 0 ? "float" : "int",
                Value = _rand.NextDouble() * 100,
                Namespace = _rand.Next(1, 100),
                NodeId = $"node_{_rand.Next(1, 50)}",
                Timestamp = DateTime.Now,
                UpdateIntervalMs = 50,          // 50 миллисекунд
                IsFastUpdate = false,           // Обычный тег
                UpdateCounter = 0
            };
            _tags.TryAdd(tag.Id, tag);
        }

        // Сохраняем все теги в базу данных
        SaveToDatabase();
        Console.WriteLine($"✅ Сгенерировано {_tags.Count} тегов (30 быстрых, 1970 обычных)");
    }

    // Загрузка тегов из файла базы данных
    private void LoadFromDatabase()
    {
        // Если файла нет - выходим (будут созданы новые теги)
        if (!File.Exists(_dbPath))
        {
            Console.WriteLine("База данных не найдена. Будет создана новая.");
            return;
        }

        // Читаем весь файл
        var json = File.ReadAllText(_dbPath);

        // Если файл пустой - выходим
        if (string.IsNullOrWhiteSpace(json))
        {
            Console.WriteLine("Файл базы данных пуст. Будет создана новая.");
            return;
        }

        try
        {
            // Десериализуем JSON в список тегов
            var tags = JsonSerializer.Deserialize<List<Tag>>(json);
            if (tags != null && tags.Count > 0)
            {
                // Добавляем каждый тег в словарь
                foreach (var t in tags)
                {
                    _tags.TryAdd(t.Id, t);
                }
                Console.WriteLine($"Загружено {tags.Count} тегов из базы данных");
            }
        }
        catch (JsonException ex)
        {
            // Если JSON повреждён - удаляем файл и создаём новый
            Console.WriteLine($"Ошибка чтения базы данных: {ex.Message}");
            Console.WriteLine("Будет создана новая база данных.");
            File.Delete(_dbPath);
        }
    }

    // Сохранение всех тегов в файл базы данных
    private void SaveToDatabase()
    {
        try
        {
            // Сериализуем все теги в JSON
            var json = JsonSerializer.Serialize(_tags.Values.ToList());
            // Записываем в файл
            File.WriteAllText(_dbPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения базы данных: {ex.Message}");
        }
    }

    // Запуск всех фоновых потоков обновления
    private void StartUpdates()
    {
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Поток 1: Быстрые теги (30 штук, обновление каждые 17 мс)
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var startTime = DateTime.Now;  // Запоминаем время начала

                // Обновляем все быстрые теги
                foreach (var tag in _tags.Values.Where(t => t.IsFastUpdate))
                {
                    tag.Value = _rand.NextDouble() * 100;      // Новое случайное значение
                    tag.Timestamp = DateTime.Now;              // Новое время
                    tag.UpdateCounter++;                       // Увеличиваем счётчик
                }

                // Вычисляем сколько времени заняло обновление
                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                // Вычитаем время выполнения из целевой задержки (17 мс)
                var delay = Math.Max(1, (int)(17 - elapsed));
                await Task.Delay(delay, token);
            }
        }, token);

        // Поток 2: Обычные теги (1970 штук, обновление каждые 50 мс)
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var startTime = DateTime.Now;

                // Обновляем все обычные теги
                foreach (var tag in _tags.Values.Where(t => !t.IsFastUpdate))
                {
                    tag.Value = _rand.NextDouble() * 100;
                    tag.Timestamp = DateTime.Now;
                    tag.UpdateCounter++;
                }

                var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
                var delay = Math.Max(1, (int)(50 - elapsed));
                await Task.Delay(delay, token);
            }
        }, token);

        // Поток 3: Сброс счётчиков обновлений каждые 250 мс
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(250, token);
                // Обнуляем счётчики у всех тегов
                foreach (var tag in _tags.Values)
                {
                    tag.UpdateCounter = 0;
                }
            }
        }, token);

        // Поток 4: Сохранение в базу данных каждые 5 секунд
        Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(5000, token);
                SaveToDatabase();  // Периодически сохраняем состояние
            }
        }, token);
    }

    // Возвращает список всех тегов
    public List<Tag> GetAllTags() => _tags.Values.ToList();

    // Возвращает тег по ID (или null если не найден)
    public Tag? GetTag(string id) => _tags.GetValueOrDefault(id);

    // Обновляет значение тега и сохраняет в БД
    public void UpdateTag(string id, double newValue)
    {
        if (_tags.TryGetValue(id, out var tag))
        {
            tag.Value = newValue;
            tag.Timestamp = DateTime.Now;
            SaveToDatabase();
        }
    }

    // Останавливает все фоновые потоки
    public void Stop() => _cts?.Cancel();

    // Генерирует историю значений тега за указанный период (для поиска по дате)
    public List<(DateTime Time, double Value)> GetHistory(string tagId, DateTime from, DateTime to)
    {
        var result = new List<(DateTime, double)>();
        var tag = GetTag(tagId);
        if (tag == null) return result;

        // Интервал между точками равен интервалу обновления тега
        int interval = tag.UpdateIntervalMs;
        // Сколько точек нужно сгенерировать (максимум 500)
        int points = Math.Min(500, (int)((to - from).TotalMilliseconds / interval));

        // Генерируем точки с равным интервалом
        for (int i = 0; i <= points; i++)
        {
            var time = from.AddMilliseconds(i * interval);  // X - время
            double y = _rand.NextDouble() * 100;            // Y - случайное значение
            result.Add((time, y));
        }
        return result;
    }

    // Возвращает текущее значение тега (простой геттер)
    public double GetCurrentValue(Tag tag)
    {
        return tag.Value;
    }

    // Возвращает информацию о прогрессе обновлений тега
    // Current - сколько обновлений произошло за последние 250 мс
    // Max - сколько должно произойти за 250 мс (250 / интервал обновления)
    public (int Current, int Max) GetUpdateInfo(Tag tag)
    {
        int maxUpdates = 250 / tag.UpdateIntervalMs;  // 250/17≈14.7 или 250/50=5
        return (tag.UpdateCounter, maxUpdates);
    }
}