using Ceptic.Client;
using Ceptic.Common;
using Ceptic.Endpoint;
using Ceptic.Server;
using Ceptic.Stream.Exceptions;
using IntegrationTests.Helpers;
using NUnit.Framework;
using System;
using System.Text;
using System.Threading;

namespace IntegrationTests
{
    public class ExchangeTests
    {
        private CepticServer server;
        private CepticClient client;

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            // make sure servers and clients get stopped after each test
            server?.Stop();
            server = null;
            client?.Stop();
            client = null;
        }

        [Test]
        public void Exchange_Unsecure_Success()
        {
            // Arrange
            server = CepticInitializers.CreateNonSecureServer(verbose: true);
            client = CepticInitializers.CreateNonSecureClient();

            var command = CommandType.GET;
            var endpoint = "/";

            server.AddCommand(command);
            server.AddRoute(command, endpoint, new EndpointEntry((request, values) => {
                var stream = request.BeginExchange();
                if (stream == null)
                {
                    return new CepticResponse(CepticStatusCode.UNEXPECTED_END);
                }
                try
                {
                    return new CepticResponse(CepticStatusCode.EXCHANGE_END);
                }
                catch (StreamException e)
                {
                    if (stream.GetSettings().verbose)
                        Console.WriteLine($"StreamException in Endpoint: {e.GetType()},{e.Message}");
                    return new CepticResponse(CepticStatusCode.UNEXPECTED_END);
                }
            }));

            var request = new CepticRequest(command, $"localhost/");
            request.SetExchange(true);
            // Act, Assert
            server.Start();
            var response = client.Connect(request);
            Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.EXCHANGE_START));
            Assert.That(response.GetExchange(), Is.True);
            Assert.That(response.GetStream(), Is.Not.Null);

            var stream = response.GetStream();
            var data = stream.ReadData(200);
            Assert.That(data.IsResponse(), Is.True);
            response = data.GetResponse();
            Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.EXCHANGE_END));
            // sleep a little bit to make sure close frame is received by client before checking if stream is stopped
            Thread.Sleep(2);
            Assert.That(stream.IsStopped(), Is.True);
            Assert.That(() => stream.ReadData(200), Throws.InstanceOf<StreamClosedException>());
        }

        [Test]
        public void Exchange_Echo100_Unsecure_Success()
        {
            // Arrange
            server = CepticInitializers.CreateNonSecureServer(verbose: true);
            client = CepticInitializers.CreateNonSecureClient();

            var command = CommandType.GET;
            var endpoint = "/";

            server.AddCommand(command);
            server.AddRoute(command, endpoint, new EndpointEntry((request, values) => {
                var stream = request.BeginExchange();
                if (stream == null)
                {
                    return new CepticResponse(CepticStatusCode.UNEXPECTED_END);
                }
                try
                {
                    while(true)
                    {
                        var data = stream.ReadData(1000);
                        if (!data.IsData())
                            break;
                        if (stream.GetSettings().verbose)
                            Console.WriteLine($"Received data: {Encoding.UTF8.GetString(data.GetData())}");
                        stream.SendData(data.GetData());
                    }
                    return new CepticResponse(CepticStatusCode.EXCHANGE_END);
                }
                catch (StreamException e)
                {
                    if (stream.GetSettings().verbose)
                        Console.WriteLine($"StreamException in Endpoint: {e.GetType()},{e.Message}");
                    return new CepticResponse(CepticStatusCode.UNEXPECTED_END);
                }
            }));

            var request = new CepticRequest(command, $"localhost{endpoint}");
            request.SetExchange(true);
            // Act, Assert
            server.Start();
            var response = client.Connect(request);
            Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.EXCHANGE_START));
            Assert.That(response.GetExchange(), Is.True);
            Assert.That(response.GetStream(), Is.Not.Null);
            Assert.That(response.GetStream().IsStopped(), Is.False);

            var stream = response.GetStream();
            
            for (int i=0; i < 100; i++)
            {
                var expectedData = Encoding.UTF8.GetBytes($"echo{i}");
                stream.SendData(expectedData);
                var data = stream.ReadData(1000);
                Assert.That(data.IsData(), Is.True);
                Assert.That(data.GetData(), Is.EqualTo(expectedData));
            }
            stream.SendResponse(new CepticResponse(CepticStatusCode.OK));
            var lastData = stream.ReadData(1000);
            Assert.That(lastData.IsResponse, Is.True);
            Assert.That(lastData.GetResponse().GetStatusCode(), Is.EqualTo(CepticStatusCode.EXCHANGE_END));
            // sleep a little bit to make sure close frame is received by client before checking if stream is stopped
            Thread.Sleep(2);
            Assert.That(stream.IsStopped(), Is.True);
            Assert.That(() => stream.ReadData(200), Throws.InstanceOf<StreamClosedException>());
        }

        [Test]
        public void Exchange_NoExchangeHeader_Unsecure_MissingExchange()
        {
            // Arrange
            server = CepticInitializers.CreateNonSecureServer(verbose: true);
            client = CepticInitializers.CreateNonSecureClient();

            var command = CommandType.GET;
            var endpoint = "/";

            server.AddCommand(command);
            server.AddRoute(command, endpoint, new EndpointEntry((request, values) => {
                var stream = request.BeginExchange();
                if (stream == null)
                {
                    return new CepticResponse(CepticStatusCode.UNEXPECTED_END);
                }
                try
                {
                    return new CepticResponse(CepticStatusCode.EXCHANGE_END);
                }
                catch (StreamException e)
                {
                    if (stream.GetSettings().verbose)
                        Console.WriteLine($"StreamException in Endpoint: {e.GetType()},{e.Message}");
                    return new CepticResponse(CepticStatusCode.UNEXPECTED_END);
                }
            }));

            var request = new CepticRequest(command, $"localhost/");
            // Act, Assert
            server.Start();
            var response = client.Connect(request);
            Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.MISSING_EXCHANGE));
            Assert.That(response.GetExchange(), Is.False);
            Assert.That(response.GetStream(), Is.Not.Null);
            Assert.That(response.GetStream().IsStopped(), Is.True);
        }

    }
}
