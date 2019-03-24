using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Timers;
using InfluxDB.Collector;
using InfluxDB.Collector.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.FileExtensions;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json.Linq;

namespace EnvoyToInflux
{
    class Program
    {
        // Settings defined in secrets.json
        static string OpenWeatherMapApiKey;
        static string Zipcode;
        static string InfluxDBUrl;
        static string InfluxDBUsername;
        static string InfluxDBPassword;

        static string WEATHER_API_URL = "https://api.openweathermap.org/data/2.5/weather?zip={0},us&appid={1}&units=imperial";

        static Timer sleepLoop;

        static void Main(string[] args)
        {
            LoadConfiguration();

            sleepLoop = new Timer(60 * 1000);
            sleepLoop.Elapsed += LogWeatherData;
            sleepLoop.AutoReset = true;
            sleepLoop.Enabled = true;

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now);
            Console.ReadLine();
            sleepLoop.Stop();
            sleepLoop.Dispose();
        }

        private static void LogWeatherData(Object source, ElapsedEventArgs e)
        {
            var weather = FetchWeatherData();

            CollectorLog.RegisterErrorHandler((message, exception) =>
            {
                Console.WriteLine($"{message}: {exception}");
            });

            Metrics.Collector = new CollectorConfiguration()
                .Tag.With("location", Zipcode)
                .WriteTo.InfluxDB(InfluxDBUrl, "weather", InfluxDBUsername, InfluxDBPassword)
                .CreateCollector();

            Console.WriteLine("Writing data to InfluxDb...");
            Metrics.Write("weather",
                new Dictionary<string, object>
                {
                    { "temperature", weather.Temperature },
                    { "pressure", weather.Pressure },
                    { "humidity", weather.Humidity },
                    { "wind-speed", weather.WindSpeed },
                    { "wind-direction", weather.WindDirection },
                    { "clouds", weather.Clouds }
                });

            Metrics.Collector.Dispose();
        }

        static Weather FetchWeatherData()
        {
            Console.WriteLine("Fetching weather data...");
            var weatherUrl = string.Format(WEATHER_API_URL, Zipcode, OpenWeatherMapApiKey);
            var weatherJson = new WebClient().DownloadString(weatherUrl);

            var weather = JObject.Parse(weatherJson);

            var temperature = weather["main"]["temp"];
            var pressure = weather["main"]["pressure"];
            var humidity = weather["main"]["humidity"];
            var windSpeed = weather["wind"]["speed"];
            var windDirection = weather["wind"]["deg"];
            var clouds = weather["clouds"]["all"];

            return new Weather
            {
                Temperature = temperature.Value<double>(),
                Pressure = pressure.Value<double>(),
                Humidity = humidity.Value<double>(),
                WindSpeed = windSpeed.Value<double>(),
                WindDirection = windDirection.Value<double>(),
                Clouds = clouds.Value<double>()
            };
        }

        static void LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

            IConfigurationRoot configuration = builder.Build();

            OpenWeatherMapApiKey = configuration["OpenWeatherMapApiKey"];
            Zipcode = configuration["Zipcode"];

            InfluxDBUrl = configuration["InfluxDBUrl"];
            InfluxDBUsername = configuration["InfluxDBUsername"];
            InfluxDBPassword = configuration["InfluxDBPassword"];
        }
    }
}
