
namespace BinanceWebsocketTester.Models
{
    public class TestMeasurement
    {
        public string IPAddress { get; set; }
        public DateTime Time { get; set; }
        public long ResponseTimeInTicks { get; set; }

        public int MeasurementGroupId { get; set; }
    }
}
