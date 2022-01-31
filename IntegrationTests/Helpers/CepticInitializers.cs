using Ceptic.Client;
using Ceptic.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace IntegrationTests.Helpers
{
    public class CepticInitializers
    {
        public static CepticServer CreateUnsecureServer(ServerSettings settings = null, bool? verbose = null)
        {
            settings ??= new ServerSettings(verbose: verbose == true);
            return new CepticServer(settings, secure: false);
        }

        public static CepticClient CreateUnsecureClient(ClientSettings settings = null)
        {
            settings ??= new ClientSettings();
            return new CepticClient(settings, secure: false);
        }
    }
}
