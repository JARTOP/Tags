using System;

namespace TagScreenSystem
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Инициализация всех систем
            TagSystem.Initialize(2000);
            ScreenSystem.Initialize();
            DataLogger.Start();

            // Запуск меню
            Menu.Run();
        }
    }
}