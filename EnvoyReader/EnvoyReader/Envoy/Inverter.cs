namespace EnvoyReader.Envoy
{
    public class Inverter
    {
        public string SerialNumber { get; set; }
        public int LastReportDate { get; set; }
        public int DevType { get; set; }
        public double LastReportWatts { get; set; }
        public double MaxReportWatts { get; set; }
    }
}
