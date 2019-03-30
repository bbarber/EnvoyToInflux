using System.Collections.Generic;

namespace EnvoyReader.Envoy
{
    public class Production
    {
        // "total-consumption"
        // "net-consumption"
        // "production"
        public string MeasurementType { get; set; }

        public int ActiveCount { get; set; }
        public int ReadingTime { get; set; }
        public string Type { get; set; }
        
        public double WNow { get; set; }
        public double WhToday { get; set; }
        public double WhLifeTime { get; set; }
        public double WhLastSevenDays { get; set; }
    }

    public class SystemProduction
    {
        public List<Production> Production { get; set; }
        public List<Production> Consumption { get; set; }
    }
}
