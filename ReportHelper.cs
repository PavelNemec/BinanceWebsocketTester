using BinanceWebsocketTester.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BinanceWebsocketTester
{
    public static class ReportHelper
    {
        private const string _datetimeFormat = "dd.MM. HH:mm:ss:ff";

        public const string MeasurementsFileLocation = @"Measurements\";

        static ReportHelper()
        {
            Directory.CreateDirectory(MeasurementsFileLocation);
        }

        public static async Task<List<TestMeasurement>> ReadAllCsvFilesAsync()
        {
            List<TestMeasurement> allMeasurements = new List<TestMeasurement>();

            foreach (string filePath in Directory.GetFiles(MeasurementsFileLocation, "*.csv"))
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    var header = await reader.ReadLineAsync();
                    string currentLine;
                    while ((currentLine = await reader.ReadLineAsync()) != null)
                    {
                        var currentLineColumns = currentLine.Split(',').Select(e=>e.Trim()).ToArray();
                        allMeasurements.Add(
                            new TestMeasurement { 
                                IPAddress = currentLineColumns[0], 
                                Time = DateTime.ParseExact(currentLineColumns[1], _datetimeFormat, CultureInfo.InvariantCulture), 
                                ResponseTimeInTicks = Convert.ToInt64(currentLineColumns[2]),
                                MeasurementGroupId = Convert.ToInt32(currentLineColumns[3])
                            });
                    }
                }
            }

            return allMeasurements;
        }

        private static double TimeInTicksToMiliseconds(double responseTimeInTicks)
        {
            return (double)responseTimeInTicks / 10000;
        }

        public static void WriteReportSummary(IEnumerable<TestMeasurement> measurements)
        {
            Console.WriteLine("Overall summary:");
            var ipAddress = measurements.Select(e => e.IPAddress).Distinct();
            foreach(var ip in ipAddress)
            {
                var pings = measurements.Count(e => e.IPAddress == ip);
                var avgResponseInMiliseconds = TimeInTicksToMiliseconds(measurements.Where(e => e.IPAddress == ip).Average(e=> e.ResponseTimeInTicks));
                Console.WriteLine($"IP Address: {ip}, Average request-response time {avgResponseInMiliseconds}, number of request-response {pings}");
            }
            Console.WriteLine();

            var grouped = measurements.GroupBy(tm => tm.MeasurementGroupId)
                                    .Select(g => new
                                    {
                                        MeasurementGroupId = g.Key,
                                        IPAddressGroups = g.GroupBy(tm => tm.IPAddress),
                                        MinTime = g.Min(e=> e.Time),
                                        MaxTime = g.Max(e => e.Time),
                                    });

            foreach (var group in grouped.OrderBy(e=>e.MeasurementGroupId))
            {
                Console.WriteLine($"MeasurementGroupId {group.MeasurementGroupId} : {group.MinTime.ToString("dd.MM. HH:mm")} - {group.MaxTime.ToString("dd.MM. HH:mm")}");
                foreach (var ipAddressGroup in group.IPAddressGroups)
                {
                    double avgResponseTime = ipAddressGroup.Average(x => x.ResponseTimeInTicks);
                    int dataPointCount = ipAddressGroup.Count();
                    Console.WriteLine($"IP Address: {ipAddressGroup.Key}, Average Request-Response Time (in ms): {TimeInTicksToMiliseconds(avgResponseTime)}, Number of Data Points: {dataPointCount}");
                }
            }
        }

        public static async Task ExportToCsvAsync(IEnumerable<TestMeasurement> measurements, string name)
        {
            StringBuilder csvBuilder = new StringBuilder();

            csvBuilder.AppendLine("IP Address, Time, TickToResponse, MeasurementGroupId");

            foreach (TestMeasurement measurement in measurements)
            {
                csvBuilder.AppendLine($"{measurement.IPAddress},{measurement.Time.ToString(_datetimeFormat)},{measurement.ResponseTimeInTicks},{measurement.MeasurementGroupId}");
            }

            if (!Directory.Exists(MeasurementsFileLocation))
            {
                Directory.CreateDirectory(MeasurementsFileLocation);
            }

            await File.WriteAllTextAsync(MeasurementsFileLocation + name, csvBuilder.ToString());
        }
    }
}
