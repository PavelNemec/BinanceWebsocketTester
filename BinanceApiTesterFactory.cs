using PacketDotNet;
using SharpPcap;
using System.Text;

namespace BinanceWebsocketTester
{
    public class BinanceApiTesterFactory
    {
        private BinanceApiTester _binanceApiTesterToInit;

        private int _mesurementGroupId;

        public BinanceApiTesterFactory()
        {
            var devices = CaptureDeviceList.Instance;
            var device = devices[0];
            device.Open();

            device.OnPacketArrival += (sender, e) =>
            {
                var packet = Packet.ParsePacket(e.GetPacket().LinkLayerType, e.GetPacket().Data);
                var ipPacket = packet.Extract<IPPacket>();

                if (ipPacket != null)
                {
                    var tcpPacket = ipPacket.Extract<TcpPacket>();
                    if (tcpPacket != null)
                    {
                        if (tcpPacket.PayloadData.Length > 0 && tcpPacket.DestinationPort == 443)
                        {
                            string payload = Encoding.ASCII.GetString(tcpPacket.PayloadData);

                            if (payload.Contains("ws-api.binance"))
                            {
                                _binanceApiTesterToInit.SetBinanceIPAddress(ipPacket.DestinationAddress);
                                _binanceApiTesterToInit.SetLocalPort(tcpPacket.SourcePort);

                                Console.WriteLine("Connected to Binance IP address: {0}", ipPacket.DestinationAddress + " using local port: " + tcpPacket.SourcePort);
                            }
                        }
                    }
                }
            };
            device.Filter = "tcp port 443";
            device.StartCapture();
        }

        public async Task<BinanceApiTester> CreateApiTesterWithDifferentIPAsync(IEnumerable<string> ipAddresses, int mesurementGroupId)
        {
            _mesurementGroupId = mesurementGroupId;

            if (!ipAddresses.Any())
            {
                return await CreateApiTesterAsync();
            }

            BinanceApiTester tester = null;

            for (int i = 0; i < 200; i++)
            {
                tester = await CreateApiTesterAsync();
                if (!ipAddresses.Any(e=> e.Contains(tester.BinanceIPAddress.ToString())))
                {
                    break;
                }
                tester.Dispose();
                Console.WriteLine("Canceling connection because this IP address was already tested.");
                tester = null;
            }

            return tester;
        }

        public async Task<BinanceApiTester> CreateApiTesterAsync()
        {
            _binanceApiTesterToInit = new BinanceApiTester(_mesurementGroupId);
            await _binanceApiTesterToInit.ConnectToApiAsync();

            while(_binanceApiTesterToInit.BinanceIPAddress == null)
            {
                Thread.Sleep(10);
            }

            return _binanceApiTesterToInit;
        }

    }
}
