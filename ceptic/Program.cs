using Ceptic.Client;
using Ceptic.Common;
using System;
using System.Text;

namespace ceptic
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var clientSettings = new ClientSettings();
            var client = new CepticClient(clientSettings);

            var request = new CepticRequest("get", "localhost:9000");
            var response = client.Connect(request);

            Console.WriteLine($"Request successful!\n{response.GetStatusCode().GetValueInt()}\n{response.GetHeaders()}\n{Encoding.UTF8.GetString(response.GetBody())}");
        }
    }
}
