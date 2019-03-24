using System;

namespace EnvoyToInflux
{
    class Weather
    {
        public double Temperature { get; set; }
        public double Pressure { get; set; }
        public double Humidity { get; set; }
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double Clouds { get; set; }
    }
}
