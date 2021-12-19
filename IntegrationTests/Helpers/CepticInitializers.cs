using Ceptic.Client;
using Ceptic.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationTests.Helpers
{
    public class CepticInitializers
    {
        public static CepticServer CreateNonSecureServer(ServerSettings settings = null, bool? verbose = null)
        {
            settings ??= new ServerSettings(verbose: verbose == true);
            return new CepticServer(settings, secure: false);
        }

        public static CepticClient CreateNonSecureClient(ClientSettings settings = null)
        {
            settings ??= new ClientSettings();
            return new CepticClient(settings, secure: false);
        }
    }
}
