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
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            var localKey = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_key.key");
            var security = new SecuritySettings(localCert, localKey);
            var server = new CepticServer(settings, security);
        }

        [Test]
        public void Server_Secure_PEM_Combined()
        {
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server.combined.cer");
            var security = new SecuritySettings(localCert, null);
            var server = new CepticServer(settings, security);
        }

        [Test]
        public void Server_Secure_PFX_Combined()
        {
            var settings = new ServerSettings(verbose: true);

            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PFX", "server.combined.pfx");
            var security = new SecuritySettings(localCert, null);
            var server = new CepticServer(settings, security);
        }
    }
}
