using Ceptic.Client;
using Ceptic.Common;
using Ceptic.Endpoint;
using Ceptic.Server;
using IntegrationTests.Helpers;
using NUnit.Framework;
using System;
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
            server = CepticInitializers.CreateNonSecureServer(verbose: true);
            client = CepticInitializers.CreateNonSecureClient();

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
        public void Command_EchoBody_Unsecure_Success()
        {
            // Arrange
            server = CepticInitializers.CreateNonSecureServer(verbose: true);
            client = CepticInitializers.CreateNonSecureClient();

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
        public void Command_EchoVariables_Unsecure_Success()
        {
            // Arrange
            server = CepticInitializers.CreateNonSecureServer(verbose: true);
            client = CepticInitializers.CreateNonSecureClient();

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