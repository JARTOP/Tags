using System;

namespace TagScreenSystem.Models
{
    public class Tag
    {
        public string Type { get; set; }        // <Тип>
        public double Value { get; set; }        // <Значение>
        public int Namespace { get; set; }       // <namespace> число
        public string NodeId { get; set; }       // <node id> string
        public DateTime Timestamp { get; set; }  // время для графиков

        public override string ToString()
        {
            return $"{Timestamp:HH:mm:ss.fff} | Type:{Type} Val:{Value:F2} NS:{Namespace} Node:{NodeId}";
        }
        
        public string ToFileString()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Type}|{Value:F2}|{Namespace}|{NodeId}";
        }
    }
}