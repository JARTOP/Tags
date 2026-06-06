using System;
using System.Collections.Generic;
using System.Linq;
using TagScreenSystem.Models;

namespace TagScreenSystem.Systems
{
    public class Chart
    {
        public int Id { get; set; }
        public List<(DateTime Time, double Value)> Data { get; set; } = new List<(DateTime, double)>();
        private int _currentIndex = 0;

        public void UpdateFromTagSystem()
        {
            var history = TagSystem.GetLastNTags(200);
            Data = history.Select(t => (t.Timestamp, t.Value)).ToList();
        }

        public void Display()
        {
            Console.WriteLine($"\n╔══════════════════════════════════════╗");
            Console.WriteLine($"║           ГРАФИК {Id,2}                    ║");
            Console.WriteLine($"╠══════════════════════════════════════╣");
            Console.WriteLine($"║ Точек данных: {Data.Count,4}                      ║");
            Console.WriteLine($"╠══════════════════════════════════════╣");
            
            if (Data.Count == 0)
            {
                Console.WriteLine($"║ Нет данных для отображения              ║");
                Console.WriteLine($"╚══════════════════════════════════════╝");
                return;
            }

            // Показываем последние 8 точек
            foreach (var point in Data.TakeLast(8))
                Console.WriteLine($"║ {point.Time:HH:mm:ss} -> {point.Value,6:F2}              ║");
            
            Console.WriteLine($"╚══════════════════════════════════════╝");
        }

        public void ShowValueAtDate(DateTime targetDate)
        {
            var closest = Data.OrderBy(p => Math.Abs((p.Time - targetDate).TotalMilliseconds)).FirstOrDefault();
            if (closest != default)
            {
                Console.WriteLine($"\n📊 На {targetDate:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"   Ближайшее значение: {closest.Value:F2}");
                Console.WriteLine($"   Время точки: {closest.Time:HH:mm:ss.fff}");
            }
            else
                Console.WriteLine("❌ Нет данных для этой даты");
        }

        public void MoveSlider(int step)
        {
            if (Data.Count == 0)
            {
                Console.WriteLine("Нет данных для навигации");
                return;
            }
            
            _currentIndex = Math.Clamp(_currentIndex + step, 0, Data.Count - 1);
            var point = Data[_currentIndex];
            
            Console.WriteLine($"\n🔘 Слайдер [{_currentIndex + 1}/{Data.Count}]");
            Console.WriteLine($"   Время: {point.Time:HH:mm:ss.fff}");
            Console.WriteLine($"   Значение: {point.Value:F2}");
            
            // Простая визуализация
            int barLength = (int)(point.Value / 100 * 30);
            Console.WriteLine($"   График: [{'█' * barLength}{'░' * (30 - barLength)}]");
        }
    }

    public static class ChartManager
    {
        private static List<Chart> _charts = new List<Chart>();
        
        static ChartManager()
        {
            for (int i = 1; i <= 5; i++)
                _charts.Add(new Chart { Id = i });
        }
        
        public static Chart GetChart(int id) => _charts.FirstOrDefault(c => c.Id == id);
        
        public static void UpdateAllCharts()
        {
            foreach (var chart in _charts)
                chart.UpdateFromTagSystem();
        }
        
        public static void DisplayAllCharts()
        {
            foreach (var chart in _charts)
                chart.Display();
        }
    }
}