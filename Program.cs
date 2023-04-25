using BinanceWebsocketTester;

class Program
{
    static async Task Main(string[] args)
    {
        // Show results
        var allTestMeasurements = await ReportHelper.ReadAllCsvFilesAsync();
        ReportHelper.WriteReportSummary(allTestMeasurements);
        Console.WriteLine("Press any key to start new measurements...");
        Console.ReadKey();
        Console.WriteLine("Starting new measurements...");

        // Start new mesurements
        for (int numbOfMeasurements = 0; numbOfMeasurements < 48; numbOfMeasurements++)
        {
            allTestMeasurements = await ReportHelper.ReadAllCsvFilesAsync();

            var testerFactory = new BinanceApiTesterFactory();
            List<string> ipAdresses = new List<string>();

            var measurementGroupId = 1;
            if (allTestMeasurements.Any())
            {
                measurementGroupId = allTestMeasurements.Max(e => e.MeasurementGroupId) + 1;
            }

            Console.WriteLine();
            Console.WriteLine("Processing group measurement number " + measurementGroupId);
            while (true)
            {
                using (var binanceApiTester = await testerFactory.CreateApiTesterWithDifferentIPAsync(ipAdresses, measurementGroupId))
                { 
                    if (binanceApiTester == null)
                    {
                        break;
                    }

                    ipAdresses.Add(binanceApiTester.BinanceIPAddress.ToString());
                    for (int i = 0; i < 10000; i++)
                    {
                        await binanceApiTester.PerformTestAsync();
                    }
                    await binanceApiTester.CreateReportAsync();
                }
            }

            Console.WriteLine("Group measurement number " + measurementGroupId + " compleated.");
            Thread.Sleep(TimeSpan.FromMinutes(10));
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}



