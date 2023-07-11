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
        private readonly Dictionary<string, string> queryparams;
        private readonly string querystring;
        private readonly CommandSettings settings;

        public EndpointValue(EndpointEntry entry, Dictionary<string, string> values, Dictionary<string, string> queryparams, string querystring, CommandSettings settings)
        {
            this.entry = entry;
            this.values = values ?? new Dictionary<string, string>();
            this.queryparams = queryparams ?? new Dictionary<string, string>();
            this.querystring = querystring;
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

        public Dictionary<string, string> GetQueryparams()
        {
            return queryparams;
        }

        public string GetQuerystring()
        {
            return querystring;
        }

        public CommandSettings GetSettings()
        {
            return settings;
        }

        public CepticResponse ExecuteEndpointEntry(CepticRequest request)
        {
            request.Values = values;
            request.Queryparams = queryparams;
            request.Querystring = querystring;
            return entry(request);
        }
    }
}
