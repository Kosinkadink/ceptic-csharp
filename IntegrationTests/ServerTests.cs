using Ceptic.Security;
using Ceptic.Server;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntegrationTests
{
    class ServerTests
    {
        [Test]
        public void Server_Secure_PEM()
        {
            // Arrange
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            var localKey = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_key.key");
            var security = SecuritySettings.Server(localCert, localKey);
            // Act, Assert
            Assert.That(() => new CepticServer(settings, security), Throws.Nothing);
        }

        [Test]
        public void Server_Secure_PEM_Combined()
        {
            // Arrange
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server.combined.cer");
            var security = SecuritySettings.Server(localCert);
            // Act, Assert
            Assert.That(() => new CepticServer(settings, security), Throws.Nothing);
        }

        [Test]
        public void Server_Secure_PFX_Combined()
        {
            // Arrange
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PFX", "server.combined.pfx");
            var security = SecuritySettings.Server(localCert);
            // Act, Assert
            Assert.That(() => new CepticServer(settings, security), Throws.Nothing);
        }
    }
}
