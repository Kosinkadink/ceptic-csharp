using Ceptic.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Endpoint
{
    public class EndpointValue
    {
        private readonly EndpointEntry entry;
        private readonly Dictionary<string, string> values;
        private readonly CommandSettings settings;

        public EndpointValue(EndpointEntry entry, Dictionary<string, string> values, CommandSettings settings)
        {
            this.entry = entry;
            this.values = values ?? new Dictionary<string, string>();
            this.settings = settings;
        }

        public EndpointEntry GetEntry()
        {
            return entry;
        }

        public Dictionary<string, string> GetValues()
        {
            return values;
        }

        public CommandSettings GetSettings()
        {
            return settings;
        }

        public CepticResponse ExecuteEndpointEntry(CepticRequest request)
        {
            return entry(request, values);
        }
    }
}
