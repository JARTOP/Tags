using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TagScreenSystem.Models;
using TagScreenSystem.Systems;

namespace TagScreenSystem.Core
{
    public static class DataLogger
    {
        private static readonly string _filePath = "data_log.txt";
        private static readonly ConcurrentQueue<string> _buffer = new ConcurrentQueue<string>();
        private static Timer _flushTimer;
        private static bool _isRunning = true;
        private static int _linesWritten = 0;

        public static void Start()
        {
            // Создаем файл с заголовком
            if (!File.Exists(_filePath))
                File.WriteAllText(_filePath, "Timestamp|Type|Value|Namespace|NodeId\n");
            
            _flushTimer = new Timer(FlushToFile, null, 0, 2000);
            Task.Run(() => ContinuousCapture());
            
            Console.WriteLine($"DataLogger запущен: файл {_filePath}, запись каждые 2 секунды");
        }

        private static void ContinuousCapture()
        {
            while (_isRunning)
            {
                var tags = TagSystem.GetLastNTags(50);
                foreach (var tag in tags)
                {
                    _buffer.Enqueue(tag.ToFileString());
                }
                Thread.Sleep(100);
            }
        }

        private static void FlushToFile(object state)
        {
            if (_buffer.IsEmpty) return;
            
            try
            {
                using (var writer = new StreamWriter(_filePath, append: true))
                {
                    int written = 0;
                    while (_buffer.TryDequeue(out string line) && written < 1000)
                    {
                        writer.WriteLine(line);
                        written++;
                        _linesWritten++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка записи в файл: {ex.Message}");
            }
        }

        public static void DisplayLastLines(int lines = 20)
        {
            if (!File.Exists(_filePath))
            {
                Console.WriteLine("Файл данных пуст.");
                return;
            }
            
            var allLines = File.ReadAllLines(_filePath);
            if (allLines.Length <= 1)
            {
                Console.WriteLine("Нет данных в файле");
                return;
            }
            
            Console.WriteLine($"\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║                     БАЗА ДАННЫХ (.txt)                        ║");
            Console.WriteLine($"╠══════════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║ Всего записей: {allLines.Length - 1,6}                                   ║");
            Console.WriteLine($"╠══════════════════════════════════════════════════════════════╣");
            
            int startLine = Math.Max(1, allLines.Length - lines);
            for (int i = startLine; i < allLines.Length; i++)
            {
                string[] parts = allLines[i].Split('|');
                if (parts.Length >= 3)
                    Console.WriteLine($"║ {parts[0]} | {parts[1]} | {parts[2],6} | {parts[3],3} | {parts[4],-8} ║");
            }
            Console.WriteLine($"╚══════════════════════════════════════════════════════════════╝");
        }
        
        public static void ShowStats()
        {
            Console.WriteLine($"\n📊 Статистика DataLogger:");
            Console.WriteLine($"   Файл: {_filePath}");
            Console.WriteLine($"   Записей в буфере: {_buffer.Count}");
            Console.WriteLine($"   Всего записано: {_linesWritten}");
        }

        public static void Shutdown()
        {
            _isRunning = false;
            _flushTimer?.Dispose();
            FlushToFile(null);
            Console.WriteLine($"DataLogger остановлен. Всего записано {_linesWritten} записей");
        }
    }
}