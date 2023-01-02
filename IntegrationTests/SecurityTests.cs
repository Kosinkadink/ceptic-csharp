using Ceptic.Security;
using Ceptic.Security.Exceptions;
using Ceptic.Server;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntegrationTests
{
    class SecurityTests
    {
        [Test]
        public void Security_KeyPassword_WipedAfterUse()
        {
            // Arrange
            var security = SecuritySettings.Server(null);
            var expectedPassword = "ceptic";
            security.KeyPassword = expectedPassword;
            // Act, Assert
            Assert.That(security.GetKeyPassword(), Is.EqualTo(expectedPassword));
            Assert.That(security.GetKeyPassword(), Is.Null);
        }

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
        public void Server_Secure_PEM_Encrypted()
        {
            // Arrange
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            var localKey = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_key_enc.key");
            var security = SecuritySettings.Server(localCert, localKey);
            security.SetKeyPassword("ceptic");
            // Act, Assert
            Assert.That(() => new CepticServer(settings, security), Throws.Nothing);
        }

        [Test]
        public void Server_Secure_PEM_Encrypted_WrongPassword()
        {
            // Arrange
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            var localKey = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_key_enc.key");
            var security = SecuritySettings.Server(localCert, localKey);
            security.SetKeyPassword("wrongpassword");
            // Act, Assert
            Assert.That(() => new CepticServer(settings, security), Throws.InstanceOf<SecurityException>());
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
        public void Server_Secure_PEM_Combined_Encrypted()
        {
            // Arrange
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server.combined_enc.cer");
            var security = SecuritySettings.Server(localCert);
            security.SetKeyPassword("ceptic");
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

        [Test]
        public void Server_Secure_PFX_Combined_Encrypted()
        {
            // Arrange
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PFX", "server.combined_enc.pfx");
            var security = SecuritySettings.Server(localCert);
            security.SetKeyPassword("ceptic");
            // Act, Assert
            Assert.That(() => new CepticServer(settings, security), Throws.Nothing);
        }
    }
}
