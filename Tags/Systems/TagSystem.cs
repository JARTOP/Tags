using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TagScreenSystem.Models;

namespace TagScreenSystem.Systems
{
    public static class TagSystem
    {
        private static List<Tag> _tags = new List<Tag>();
        private static readonly object _lock = new object();
        private static Timer _updateTimer;
        private static Random _rand = new Random();
        private static readonly string[] _types = { "Temp", "Pressure", "Flow", "Level", "Speed", "Voltage" };

        // Очередь для хранения последних N значений
        private static ConcurrentQueue<Tag> _historyQueue = new ConcurrentQueue<Tag>();
        private static int _queueSize = 100;

        public static int QueueSize
        {
            get => _queueSize;
            set
            {
                if (value > 0 && value < 500)
                {
                    _queueSize = value;
                    Console.WriteLine($"Queue Size установлен на {value}");
                }
                else
                    Console.WriteLine("Queue Size должно быть от 1 до 499");
            }
        }

        public static void Initialize(int tagCount = 2000)
        {
            _tags.Clear();
            for (int i = 0; i < tagCount; i++)
            {
                _tags.Add(new Tag
                {
                    Type = _types[_rand.Next(_types.Length)],
                    Value = _rand.NextDouble() * 100,
                    Namespace = _rand.Next(1, 100),
                    NodeId = $"Node_{_rand.Next(1, 500)}",
                    Timestamp = DateTime.Now
                });
            }

            _updateTimer = new Timer(UpdateTags, null, 0, 50);
            Console.WriteLine($"Tag System инициализирована: {tagCount} тегов, обновление каждые 50 мс");
        }

        private static void UpdateTags(object state)
        {
            lock (_lock)
            {
                foreach (var tag in _tags)
                {
                    tag.Value += (_rand.NextDouble() - 0.5) * 5;
                    if (tag.Value < 0) tag.Value = 0;
                    if (tag.Value > 100) tag.Value = 100;
                    tag.Timestamp = DateTime.Now;
                }

                var snapshot = new List<Tag>(_tags);
                foreach (var tag in snapshot)
                {
                    _historyQueue.Enqueue(tag);
                    while (_historyQueue.Count > _queueSize)
                        _historyQueue.TryDequeue(out _);
                }
            }
        }

        public static List<Tag> GetLastNTags(int n)
        {
            return _historyQueue.Reverse().Take(n).ToList();
        }

        public static List<Tag> GetAllTags()
        {
            lock (_lock)
            {
                return new List<Tag>(_tags);
            }
        }

        public static bool EditTag(int index, double newValue, string newType = null)
        {
            lock (_lock)
            {
                if (index < 0 || index >= _tags.Count) return false;
                _tags[index].Value = newValue;
                if (!string.IsNullOrEmpty(newType))
                    _tags[index].Type = newType;
                return true;
            }
        }

        public static void Shutdown()
        {
            _updateTimer?.Dispose();
            Console.WriteLine("Tag System остановлена");
        }
    }
}