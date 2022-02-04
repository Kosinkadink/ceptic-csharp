using Ceptic.Client;
using Ceptic.Common;
using Ceptic.Endpoint;
using Ceptic.Server;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace ceptic
{
    class Program
    {
        static readonly string localhostIPv4 = "127.0.0.1";

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            //DoServer();
            DoClient();
            //DoClientExchange();
        }

        static void DoClientExchange()
        {
            var clientSettings = new ClientSettings();
            var client = new CepticClient(clientSettings);

            var request = new CepticRequest(CommandType.GET, $"{localhostIPv4}/exchange");
            request.SetExchange(true);

            var response = client.Connect(request);
            Console.WriteLine($"Request successful!\n{response.GetStatusCode().GetValue()}\n{response.GetHeaders()}\n{Encoding.UTF8.GetString(response.GetBody())}");
            if (response.GetExchange())
            {
                var stream = response.GetStream();
                var hasReceivedResponse = false;
                for (int i = 0; i < 10000; i++) {
                    var stringData = $"echo{i}";
                    stream.SendData(Encoding.UTF8.GetBytes(stringData));
                    var data = stream.ReadData(100);
                    if (data.IsResponse())
                    {
                        hasReceivedResponse = true;
                        Console.WriteLine($"Received response; end of exchange!\n{data.GetResponse().GetStatusCode().GetValue()}\n{data.GetResponse().GetHeaders()}\n{Encoding.UTF8.GetString(data.GetResponse().GetBody())}");
                        break;
                    }
                    var receivedData = data.GetData();
                    if (receivedData == null)
                        Console.WriteLine($"Received null when expecting {stringData}");
                    Console.WriteLine($"Received echo: {Encoding.UTF8.GetString(receivedData)}");

                }
                if (!hasReceivedResponse)
                {
                    stream.SendData(Encoding.UTF8.GetBytes("exit"));
                    var data = stream.ReadData(100);
                    if (data.IsResponse())
                    {
                        hasReceivedResponse = true;
                        Console.WriteLine($"Received response after sending exit; end of exchange!\n{data.GetResponse().GetStatusCode().GetValue()}\n{data.GetResponse().GetHeaders()}\n{Encoding.UTF8.GetString(data.GetResponse().GetBody())}");
                    }
                }
                stream.SendClose();
            }
            client.Stop();
        }

        static void DoClient()
        {
            var clientSettings = new ClientSettings();
            var client = new CepticClient(clientSettings, secure:false);

            var connectStopwatch = new Stopwatch();
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < 10000; i++)
            {
                var request = new CepticRequest("get", $"{localhostIPv4}:9000");
                connectStopwatch.Restart();
                var response = client.Connect(request);
                connectStopwatch.Stop();
                Console.WriteLine($"\n#{i + 1} Request successful in {connectStopwatch.ElapsedMilliseconds} ms!\n{response.GetStatusCode().GetValue()}\n{response.GetHeaders()}\n{Encoding.UTF8.GetString(response.GetBody())}");
                //Thread.Sleep(1000);
            }
            sw.Stop();
            Console.WriteLine($"Total elapsed: {sw.ElapsedMilliseconds} ms");
            client.Stop();
        }
        static void DoServer()
        {
            var serverSettings = new ServerSettings(verbose: true);
            var server = new CepticServer(serverSettings);

            server.AddCommand(CommandType.GET);
            server.AddRoute(CommandType.GET, "/",
                new EndpointEntry((request, values) => new CepticResponse(CepticStatusCode.OK)));

            server.Start();
            //DoClient();

            Console.WriteLine("Press ENTER to close server...");
            Console.ReadLine();
            server.Stop();
        }
    }
}
