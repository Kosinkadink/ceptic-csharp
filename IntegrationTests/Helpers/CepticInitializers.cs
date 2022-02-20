using Ceptic.Client;
using Ceptic.Security;
using Ceptic.Server;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace IntegrationTests.Helpers
{
    public class CepticInitializers
    {
        public static readonly string localhostIPv4 = "127.0.0.1";

        public static CepticServer CreateUnsecureServer(ServerSettings settings = null, bool? verbose = null)
        {
            settings ??= new ServerSettings(verbose: verbose == true);
            return new CepticServer(settings, new SecuritySettings(secure: false));
        }

        public static CepticClient CreateUnsecureClient(ClientSettings settings = null)
        {
            settings ??= new ClientSettings();
            return new CepticClient(settings, new SecuritySettings(secure: false));
        }

        public static CepticServer CreateSecureServer(ServerSettings settings = null, bool? verbose = null)
        {
            settings ??= new ServerSettings(verbose: verbose == true);
            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            var localKey = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_key.key");
            //var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PFX", "server.combined.pfx");
            var security = new SecuritySettings(localCert, localKey);
            return new CepticServer(settings, security);
        }

        public static CepticClient CreateSecureClient(ClientSettings settings = null)
        {
            settings ??= new ClientSettings();
            var remoteCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            //var remoteCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PFX", "server_cert.crt");
            var security = new SecuritySettings(remoteCert, verifyRemote: false);
            return new CepticClient(settings, security);
        }

        public static CepticServer CreateSecureServerVerifyClient(ServerSettings settings = null, bool? verbose = null)
        {
            settings ??= new ServerSettings(verbose: verbose == true);
            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            var localKey = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_key.key");
            var remoteCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "client_cert.cer");
            var security = new SecuritySettings(localCert, localKey, remoteCert);
            return new CepticServer(settings, security);
        }

        public static CepticClient CreateSecureClientVerifyClient(ClientSettings settings = null)
        {
            settings ??= new ClientSettings();
            var localCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "client_cert.cer");
            var localKey = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "client_key.key");
            var remoteCert = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestData", "PEM", "server_cert.cer");
            var security = new SecuritySettings(localCert, localKey, remoteCert, verifyRemote: false);
            return new CepticClient(settings, security);
        }
    }
}
