
using BinanceWebsocketTester.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace BinanceWebsocketTester
{
    public class BinanceApiTester : IDisposable
    {
        public int Counter = 0;

        public List<TestMeasurement> Measurements = new List<TestMeasurement>();

        public IPAddress BinanceIPAddress { get; private set; }

        public ushort? LocalPort { get; private set; }

        private string _binanceApiUrl = "wss://ws-api.binance.com:443/ws-api/v3";

        private ClientWebSocket _clientWebSocket;

        private int _mesurementGroupId;

        public void SetBinanceIPAddress(IPAddress iPaddress)
        {
            BinanceIPAddress = iPaddress;
        }
        public void SetLocalPort(ushort port)
        {
            LocalPort = port;
        }

        public BinanceApiTester(int mesurementGroupId)
        {
            _clientWebSocket = new ClientWebSocket();
            _mesurementGroupId = mesurementGroupId;
        }

        public async Task ConnectToApiAsync()
        {
            await _clientWebSocket.ConnectAsync(new Uri(_binanceApiUrl), CancellationToken.None);
        }

        public async Task PerformTestAsync()
        {
            var testRequest = new
            {
                method = "time",
                id = "1d7d3c72-942d-484c-1271-4e21413badb1"
            };

            var json = JsonConvert.SerializeObject(testRequest);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var buffer = new ArraySegment<byte>(bytes);

            var receiveBuffer = new byte[1024];
            var receiveSegment = new ArraySegment<byte>(receiveBuffer);

            var stopwatch = Stopwatch.StartNew();
            await _clientWebSocket.SendAsync(buffer, WebSocketMessageType.Text, true, CancellationToken.None);
            var result = await _clientWebSocket.ReceiveAsync(receiveSegment, CancellationToken.None);
            stopwatch.Stop();

            var newMeasurment = new TestMeasurement()
            {
                IPAddress = BinanceIPAddress.ToString(),
                ResponseTimeInTicks = stopwatch.ElapsedTicks,
                Time = DateTime.UtcNow,
                MeasurementGroupId = _mesurementGroupId
            };

            if (CheckResponseStatus200(Encoding.UTF8.GetString(receiveBuffer, 0, result.Count)))
            {
                Console.WriteLine("Ping numb. " + Counter++.ToString() + " " + newMeasurment.ResponseTimeInTicks);
                Measurements.Add(newMeasurment);
            }
            else
            {
                throw new Exception("Server not returend correct status"); 
            }
        }

        public async Task CreateReportAsync()
        {
            if (Measurements.Any())
            {
                var reportName = Measurements.First().IPAddress.Replace(".", "-") + " " + DateTime.UtcNow.ToString("dd-MM HH-mm-ss") + ".csv";
                await ReportHelper.ExportToCsvAsync(Measurements, reportName);
            }
        }

        public void Dispose()
        {
            Task.Run(async () => await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing the WebSocket", CancellationToken.None)).Wait();
            _clientWebSocket.Dispose();
        }

        private bool CheckResponseStatus200(string response)
        {
            return response.Contains("\"status\":200");
        }
    }
}
