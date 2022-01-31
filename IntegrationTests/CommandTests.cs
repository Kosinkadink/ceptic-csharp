using Ceptic.Client;
using Ceptic.Common;
using Ceptic.Endpoint;
using Ceptic.Server;
using Ceptic.Stream.Exceptions;
using IntegrationTests.Helpers;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Text;

namespace IntegrationTests
{
    public class Tests
    {
        private EndpointEntry basicEndpointEntry = new EndpointEntry((request, values) => new CepticResponse(CepticStatusCode.OK));
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
        public void Command_Unsecure_Success()
        {
            // Arrange
            server = CepticInitializers.CreateUnsecureServer(verbose: true);
            client = CepticInitializers.CreateUnsecureClient();

            var command = CommandType.GET;
            var endpoint = "/";

            server.AddCommand(command);
            server.AddRoute(command, endpoint, basicEndpointEntry);

            var request = new CepticRequest(command, $"localhost{endpoint}");
            // Act
            server.Start();
            var response = client.Connect(request);
            // Assert
            Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.OK));
            Assert.That(response.GetBody().Length, Is.EqualTo(0));
            Assert.That(response.GetExchange(), Is.False);
        }

        [Test]
        public void Command_Unsecure_1000_Success()
        {
            // Arrange
            server = CepticInitializers.CreateUnsecureServer(verbose: true);
            client = CepticInitializers.CreateUnsecureClient();

            var command = CommandType.GET;
            var endpoint = "/";

            server.AddCommand(command);
            server.AddRoute(command, endpoint, new EndpointEntry((request, variables) => {
                Console.WriteLine($"Received body: {Encoding.UTF8.GetString(request.GetBody())}");
                return new CepticResponse(CepticStatusCode.OK, request.GetBody());
            }));

            
            // Act
            server.Start();
            var connectTimer = new Stopwatch();
            var timer = new Stopwatch();
            timer.Start();
            for (int i = 0; i < 1000; i++)
            {
                try
                {
                    var request = new CepticRequest(command, $"localhost{endpoint}", body: Encoding.UTF8.GetBytes($"{i}"));
                    connectTimer.Restart();
                    var response = client.Connect(request);
                    connectTimer.Stop();
                    Console.WriteLine($"Connection took {connectTimer.ElapsedMilliseconds} ms");
                    // Assert
                    Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.OK));
                    //Assert.That(response.GetBody().Length, Is.EqualTo(0));
                    Assert.That(response.GetExchange(), Is.False);
                }
                catch (StreamException e)
                {
                    Console.WriteLine($"Error thrown on iteration: {i},{e.GetType()},{e.Message}");
                    throw e;
                }
            }
            timer.Stop();
            Console.WriteLine($"Total ms elapsed: {timer.ElapsedMilliseconds}");
        }

        [Test]
        public void Command_Unsecure_EchoBody_Success()
        {
            // Arrange
            server = CepticInitializers.CreateUnsecureServer(verbose: true);
            client = CepticInitializers.CreateUnsecureClient();

            var command = CommandType.GET;
            var endpoint = "/";

            byte[] expectedBody = Encoding.UTF8.GetBytes("Hello world!");

            server.AddCommand(command);
            server.AddRoute(command, endpoint, new EndpointEntry((request, values) =>
            {
                return new CepticResponse(CepticStatusCode.OK, body: request.GetBody());
            }));
            
            var request = new CepticRequest(command, $"localhost{endpoint}", body: expectedBody);
            // Act
            server.Start();
            var response = client.Connect(request);
            // Assert
            Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.OK));
            Assert.That(response.GetBody(), Is.EqualTo(expectedBody));
            Assert.That(response.GetExchange(), Is.False);

            Assert.That(request.GetContentLength(), Is.EqualTo(expectedBody.Length));
            Assert.That(response.GetContentLength(), Is.EqualTo(expectedBody.Length));
        }

        [Test]
        public void Command_Unsecure_EchoVariables_Success()
        {
            // Arrange
            server = CepticInitializers.CreateUnsecureServer(verbose: true);
            client = CepticInitializers.CreateUnsecureClient();

            var command = CommandType.GET;
            var variableName1 = "var1";
            var variableName2 = "var2";
            var registerEndpoint = $"<{variableName1}>/<{variableName2}>";
            var expectedValue1 = Guid.NewGuid().ToString();
            var expectedValue2 = Guid.NewGuid().ToString();
            var endpoint = $"{expectedValue1}/{expectedValue2}";

            byte[] expectedBody = Encoding.UTF8.GetBytes($"{variableName1} was {expectedValue1}, {variableName2} was {expectedValue2}");

            server.AddCommand(command);
            server.AddRoute(command, registerEndpoint, new EndpointEntry((request, values) =>
            {
                var stringResult = $"{variableName1} was {values[variableName1]}, {variableName2} was {values[variableName2]}";
                if (request.GetStream().GetSettings().verbose)
                    Console.WriteLine($"Sending body: {stringResult}");
                return new CepticResponse(CepticStatusCode.OK, body: Encoding.UTF8.GetBytes(stringResult));
            }));

            var request = new CepticRequest(command, $"localhost/{endpoint}", body: expectedBody);
            // Act
            server.Start();
            var response = client.Connect(request);
            // Assert
            Assert.That(response.GetStatusCode(), Is.EqualTo(CepticStatusCode.OK));
            Assert.That(response.GetBody(), Is.EqualTo(expectedBody));
            Assert.That(response.GetExchange(), Is.False);

            Assert.That(request.GetContentLength(), Is.EqualTo(expectedBody.Length));
            Assert.That(response.GetContentLength(), Is.EqualTo(expectedBody.Length));
        }


        #region Private Methods
        #endregion
    }
}