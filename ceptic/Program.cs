using Ceptic.Client;
using Ceptic.Common;
using System;
using System.Text;
using System.Threading;

namespace ceptic
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var clientSettings = new ClientSettings();
            var client = new CepticClient(clientSettings);

            for (int i = 0; i < 1000; i++)
            {
                var request = new CepticRequest("get", "localhost:9000");
                var response = client.Connect(request);
                Console.WriteLine($"\n#{i+1} Request successful!\n{response.GetStatusCode().GetValueInt()}\n{response.GetHeaders()}\n{Encoding.UTF8.GetString(response.GetBody())}");
                //Thread.Sleep(1000);
            }

            client.Stop();
        }
    }
}
