namespace TagScreenSystem.Models;

// Модель экрана - представляет один экран в системе (таблицу или график)
public class Screen
{
    // Уникальный идентификатор экрана (от 0 до 11)
    public int Id { get; set; }
    
    // Название экрана (например "Экран 1 (График)" или "Экран 6 (Таблица)")
    public string Name { get; set; } = "";
    
    // Тип экрана: true - график, false - таблица
    public bool IsChart { get; set; }
    
    // Список ID тегов, которые отображаются на этом экране
    // Каждый экран содержит от 300 до 500 тегов
    public List<string> TagIds { get; set; } = new();
    
    // Данные для отображения на графике
    // Каждый элемент кортежа: (DateTime Time - время, double Value - значение Y в диапазоне -2..0)
    // Хранит последние NumberOfDisplays точек для этого экрана
    public List<(DateTime Time, double Value)> ChartData { get; set; } = new();
}