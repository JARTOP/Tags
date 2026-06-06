using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TagScreenSystem.Models;

namespace TagScreenSystem.Systems
{
    public class Screen
    {
        public int Id { get; set; }
        public List<Tag> Tags { get; set; } = new List<Tag>();

        public void Refresh()
        {
            var allTags = TagSystem.GetAllTags();
            var rand = new Random();
            Tags = allTags.OrderBy(x => rand.Next()).Take(rand.Next(300, 501)).ToList();
        }

        public void Display()
        {
            Console.WriteLine($"\n╔══════════════════════════════════════╗");
            Console.WriteLine($"║           ЭКРАН {Id,2}                    ║");
            Console.WriteLine($"╠══════════════════════════════════════╣");
            Console.WriteLine($"║ Всего значений: {Tags.Count,4}                     ║");
            Console.WriteLine($"╠══════════════════════════════════════╣");
            
            foreach (var tag in Tags.Take(15))
                Console.WriteLine($"║ {tag,-42} ║");
            
            if (Tags.Count > 15)
                Console.WriteLine($"║ ... и еще {Tags.Count - 15,2} значений                  ║");
            
            Console.WriteLine($"╚══════════════════════════════════════╝");
        }
    }

    public static class ScreenSystem
    {
        private static List<Screen> _screens = new List<Screen>();
        private static Timer _refreshTimer;
        private static Random _rand = new Random();

        public static void Initialize()
        {
            int screenCount = _rand.Next(1, 13);
            _screens.Clear();
            
            for (int i = 0; i < screenCount; i++)
            {
                var screen = new Screen { Id = i + 1 };
                screen.Refresh();
                _screens.Add(screen);
            }

            _refreshTimer = new Timer(RefreshScreens, null, 0, 250);
            Console.WriteLine($"Screen System инициализирована: {screenCount} экранов, обновление каждые 250 мс");
        }

        private static void RefreshScreens(object state)
        {
            foreach (var screen in _screens)
                screen.Refresh();
        }

        public static List<Screen> GetAllScreens() => _screens;
        
        public static Screen GetScreen(int id) => _screens.FirstOrDefault(s => s.Id == id);

        public static void Shutdown()
        {
            _refreshTimer?.Dispose();
            Console.WriteLine("Screen System остановлена");
        }
    }
}