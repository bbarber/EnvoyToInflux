using EnvoyReader.Config;
using EnvoyReader.Envoy;
using InfluxDB.LineProtocol.Client;
using InfluxDB.LineProtocol.Payload;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EnvoyReader.Output
{
    class InfluxDB
    {
        private readonly Uri url;
        private readonly string database;
        private readonly string username;
        private readonly string password;

        public InfluxDB(IAppSettings appSettings)
        {
            url = new Uri(appSettings.InfluxUrl);
            database = appSettings.InfluxDb;
            username = appSettings.InfluxDbUsername;
            password = appSettings.InfluxDbPassword;

            Console.WriteLine($"Use InfluxDB: {database} @ {url}");
        }

        public async Task WriteAsync(Production systemProduction, Production production, Production consumption, List<Inverter> inverters)
        {
            var payload = new LineProtocolPayload();

            AddProductionToPayload("inverters", systemProduction, payload);
            AddProductionToPayload("production", production, payload);
            AddProductionToPayload("consumption", consumption, payload);

            AddInvertersToPayload(inverters, payload);

            var client = new LineProtocolClient(url, database, username, password);

            var writeResult = await client.WriteAsync(payload);

            if (!writeResult.Success)
                throw new Exception(writeResult.ErrorMessage);
        }

        private bool AddProductionToPayload(string measurement, Production systemProduction, LineProtocolPayload payload)
        {
            if (systemProduction.ReadingTime <= 0)
                return false;

            var readingTime = DateTimeOffset.FromUnixTimeSeconds(systemProduction.ReadingTime);

            var systemPoint = new LineProtocolPoint(
                measurement, //Measurement
                new Dictionary<string, object> //Fields
                {
                    { $"activecount", systemProduction.ActiveCount },
                    { $"WNow", systemProduction.WNow },
                    { $"WhToday", systemProduction.WhToday },
                    { $"WhLifetime", systemProduction.WhLifeTime },
                    { $"WhLastSevenDays", systemProduction.WhLastSevenDays }
                },
                new Dictionary<string, string> //Tags
                {
                },
                readingTime.UtcDateTime); //Timestamp

            payload.Add(systemPoint);

            return true;
        }

        private bool AddInvertersToPayload(List<Inverter> inverters, LineProtocolPayload payload)
        {
            var added = false;

            foreach (var inverter in inverters.Where(i => i.LastReportDate > 0))
            {
                var reportTime = DateTimeOffset.FromUnixTimeSeconds(inverter.LastReportDate);

                var inverterPoint = new LineProtocolPoint(
                    "inverter", //Measurement
                    new Dictionary<string, object> //Fields
                    {
                        { $"lastreportwatts", inverter.LastReportWatts },
                        { $"maxreportwatts", inverter.MaxReportWatts },
                    },
                    new Dictionary<string, string> //Tags
                    {
                        { $"serialnumber", inverter.SerialNumber },
                    },
                    reportTime.UtcDateTime); //Timestamp

                payload.Add(inverterPoint);
                added = true;
            }

            return added;
        }
    }
}
