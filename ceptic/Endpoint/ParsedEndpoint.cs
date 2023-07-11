using System.Collections.Generic;

namespace Ceptic.Endpoint
{
    public class ParsedEndpoint
    {
        private readonly string endpoint;
        private readonly string querystring;
        private readonly Dictionary<string, string> queryparams;

        public ParsedEndpoint(string endpoint, string querystring, Dictionary<string, string> queryparams)
        {
            this.endpoint = endpoint;
            this.querystring = querystring;
            this.queryparams = queryparams;
        }

        public string Endpoint { get { return endpoint; } }
        public string Querystring { get { return querystring; } }
        public Dictionary<string, string> Queryparams { get { return queryparams; } }
    }
}
