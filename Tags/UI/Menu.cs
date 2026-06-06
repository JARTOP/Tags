using System;
using TagScreenSystem.Systems;
using TagScreenSystem.Core;
using System.Linq;

namespace TagScreenSystem.UI
{
    public enum UserRole { Admin, Operator }

    public static class Menu
    {
        private static UserRole _currentUser = UserRole.Operator;

        public static void Run()
        {
            Console.Clear();
            Console.WriteLine("╔══════════════════════════════════════╗");
            Console.WriteLine("║     TAG & SCREEN SYSTEM v1.0        ║");
            Console.WriteLine("╚══════════════════════════════════════╝");
            
            Console.Write("\nВыберите роль:\n");
            Console.WriteLine("  1 - Администратор (полный доступ)");
            Console.WriteLine("  2 - Оператор (только просмотр)");
            Console.Write("Ваш выбор: ");
            
            string role = Console.ReadLine();
            _currentUser = role == "1" ? UserRole.Admin : UserRole.Operator;
            Console.WriteLine($"\n✅ Вход выполнен как {_currentUser}\n");
            Console.WriteLine("Нажмите любую клавишу для продолжения...");
            Console.ReadKey();

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine($"╔══════════════════════════════════════╗");
                Console.WriteLine($"║     ГЛАВНОЕ МЕНЮ - {_currentUser, -7}               ║");
                Console.WriteLine($"╠══════════════════════════════════════╣");
                Console.WriteLine($"║ 1. Показать все экраны               ║");
                Console.WriteLine($"║ 2. Выбрать экран                     ║");
                Console.WriteLine($"║ 3. Показать графики                  ║");
                Console.WriteLine($"║ 4. Работа с графиком (ползунок)      ║");
                Console.WriteLine($"║ 5. Управление очередью (Queue Size)  ║");
                Console.WriteLine($"║ 6. Просмотр очереди тегов            ║");
                Console.WriteLine($"║ 7. Просмотр БД (.txt)                ║");
                if (_currentUser == UserRole.Admin)
                    Console.WriteLine($"║ 8. ✏️  Редактировать тег (Админ)     ║");
                Console.WriteLine($"║ 0. Выход                             ║");
                Console.WriteLine($"╚══════════════════════════════════════╝");
                Console.Write($"\nВаш выбор: ");

                string choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": ShowAllScreens(); break;
                    case "2": SelectScreenAndView(); break;
                    case "3": ShowAllCharts(); break;
                    case "4": SelectChartAndSlider(); break;
                    case "5": ChangeQueueSize(); break;
                    case "6": ShowLastNTagsFromQueue(); break;
                    case "7": DataLogger.DisplayLastLines(20); break;
                    case "8": 
                        if (_currentUser == UserRole.Admin) 
                            EditTag(); 
                        else 
                            Console.WriteLine("⛔ Доступ запрещен"); 
                        break;
                    case "0": exit = true; break;
                    default: Console.WriteLine("❌ Неверный ввод"); break;
                }
                
                if (!exit)
                {
                    Console.WriteLine("\nНажмите любую клавишу для продолжения...");
                    Console.ReadKey();
                }
            }

            TagSystem.Shutdown();
            ScreenSystem.Shutdown();
            DataLogger.Shutdown();
            Console.WriteLine("\nСистема остановлена. Нажмите любую клавишу для выхода.");
            Console.ReadKey();
        }

        static void ShowAllScreens()
        {
            var screens = ScreenSystem.GetAllScreens();
            Console.WriteLine($"\n📺 Всего экранов: {screens.Count}\n");
            foreach (var screen in screens)
                Console.WriteLine($"   Экран {screen.Id} | Значений: {screen.Tags.Count}");
        }

        static void SelectScreenAndView()
        {
            var screens = ScreenSystem.GetAllScreens();
            Console.Write($"\nВведите номер экрана (1-{screens.Count}): ");
            
            if (int.TryParse(Console.ReadLine(), out int id) && id >= 1 && id <= screens.Count)
            {
                var screen = ScreenSystem.GetScreen(id);
                screen?.Display();
            }
            else
                Console.WriteLine("❌ Неверный номер экрана");
        }

        static void ShowAllCharts()
        {
            ChartManager.UpdateAllCharts();
            ChartManager.DisplayAllCharts();
        }

        static void SelectChartAndSlider()
        {
            Console.Write("\nВыберите график (1-5): ");
            if (int.TryParse(Console.ReadLine(), out int chartId) && chartId >= 1 && chartId <= 5)
            {
                var chart = ChartManager.GetChart(chartId);
                chart.UpdateFromTagSystem();
                
                Console.Clear();
                Console.WriteLine($"\n🎮 УПРАВЛЕНИЕ ГРАФИКОМ {chartId}");
                Console.WriteLine("═══════════════════════════════════");
                Console.WriteLine("  ← →  - перемещение по точкам");
                Console.WriteLine("  D    - поиск по дате");
                Console.WriteLine("  Q    - выход");
                Console.WriteLine("═══════════════════════════════════\n");
                
                bool inChart = true;
                while (inChart)
                {
                    var key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.LeftArrow: chart.MoveSlider(-1); break;
                        case ConsoleKey.RightArrow: chart.MoveSlider(1); break;
                        case ConsoleKey.D:
                            Console.Write("📅 Введите дату и время (ГГГГ-ММ-ДД ЧЧ:ММ:СС): ");
                            if (DateTime.TryParse(Console.ReadLine(), out DateTime date))
                                chart.ShowValueAtDate(date);
                            else
                                Console.WriteLine("❌ Неверный формат даты");
                            break;
                        case ConsoleKey.Q: 
                            inChart = false; 
                            break;
                    }
                }
            }
            else
                Console.WriteLine("❌ Неверный номер графика");
        }

        static void ChangeQueueSize()
        {
            Console.Write($"\n📊 Текущий Queue Size: {TagSystem.QueueSize}\n");
            Console.Write("Введите новый размер (1-499): ");
            
            if (int.TryParse(Console.ReadLine(), out int size))
                TagSystem.QueueSize = size;
            else
                Console.WriteLine("❌ Неверное число");
        }

        static void ShowLastNTagsFromQueue()
        {
            int n = Math.Min(TagSystem.QueueSize, 30);
            var tags = TagSystem.GetLastNTags(n);
            
            Console.WriteLine($"\n📋 ПОСЛЕДНИЕ ТЕГИ ИЗ ОЧЕРЕДИ");
            Console.WriteLine($"═══════════════════════════════════════════════════════");
            Console.WriteLine($"Queue Size: {TagSystem.QueueSize} | Показано: {tags.Count}\n");
            
            int i = 1;
            foreach (var tag in tags)
            {
                Console.WriteLine($"{i,2}. {tag}");
                i++;
            }
        }

        static void EditTag()
        {
            var tags = TagSystem.GetAllTags();
            Console.WriteLine($"\n✏️  РЕДАКТИРОВАНИЕ ТЕГА");
            Console.WriteLine($"═══════════════════════════════════════");
            Console.WriteLine($"Всего тегов: {tags.Count}\n");
            Console.WriteLine("Первые 10 тегов:");
            
            for (int i = 0; i < Math.Min(10, tags.Count); i++)
                Console.WriteLine($"  [{i}] {tags[i]}");

            Console.Write("\nВведите индекс тега для редактирования: ");
            if (int.TryParse(Console.ReadLine(), out int idx) && idx >= 0 && idx < tags.Count)
            {
                Console.Write($"Текущее значение: {tags[idx].Value:F2}\n");
                Console.Write("Новое значение (число): ");
                
                if (double.TryParse(Console.ReadLine(), out double newVal))
                {
                    TagSystem.EditTag(idx, newVal);
                    Console.WriteLine("✅ Тег успешно обновлен!");
                    
                    // Показываем обновленный тег
                    var updatedTags = TagSystem.GetAllTags();
                    Console.WriteLine($"Обновленный тег: {updatedTags[idx]}");
                }
                else 
                    Console.WriteLine("❌ Неверное значение");
            }
            else 
                Console.WriteLine("❌ Неверный индекс");
        }
    }
}