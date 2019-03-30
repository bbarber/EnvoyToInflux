using EnvoyReader.Config;
using EnvoyReader.Envoy;
using EnvoyReader.Output;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace EnvoyReader
{
    class Program
    {
        private static IAppSettings ReadAppConfiguration()
        {
            var startupPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

            var builder = new ConfigurationBuilder()
               .SetBasePath(startupPath)
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            var configuration = builder.Build();
            var appSettings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(appSettings);

            return appSettings;
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine(DateTimeOffset.Now);

            var appSettings = ReadAppConfiguration();
            Console.WriteLine($"Use Envoy: {appSettings.EnvoyBaseUrl} as {appSettings.EnvoyUsername}");

            while (true)
            {
                try
                {
                    var envoyDataProvider = new EnvoyDataProvider(appSettings.EnvoyUsername, appSettings.EnvoyPassword, appSettings.EnvoyBaseUrl);

                    var data = await envoyDataProvider.GetSystemProduction();

                    var invertersPower = data.Production.FirstOrDefault(p => p.Type == "inverters");
                    var production = data.Production.FirstOrDefault(p => p.MeasurementType == "production");
                    var consumption = data.Consumption.FirstOrDefault(p => p.MeasurementType == "total-consumption");

                    var inverters = await ReadInverterProduction(envoyDataProvider);

                    var influxDb = new Output.InfluxDB(appSettings);
                    await influxDb.WriteAsync(invertersPower, production, consumption, inverters);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine(ex);
                }

                Thread.Sleep(60 * 1000);
            }
        }

        private static async Task<List<Inverter>> ReadInverterProduction(EnvoyDataProvider envoyDataProvider)
        {
            Console.WriteLine("Read inverter producton");

            var inverters = await envoyDataProvider.GetInverterProduction();

            if (inverters == null)
                throw new Exception("No inverter data found");

            Console.WriteLine("  S/N\t\tReportTime\t\t\tWatts");

            foreach (var inverter in inverters)
            {
                if (inverter.LastReportDate > 0)
                {
                    var reportTime = DateTimeOffset.FromUnixTimeSeconds(inverter.LastReportDate);

                    Console.WriteLine($"  {inverter.SerialNumber}\t{reportTime.ToLocalTime()}\t{inverter.LastReportWatts}");
                }
            }

            Console.WriteLine($"  Total watts: {inverters.Sum(i => i.LastReportWatts)}");

            return inverters;
        }
    }
}
