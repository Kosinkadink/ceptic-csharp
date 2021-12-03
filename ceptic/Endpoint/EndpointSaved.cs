using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Endpoint
{
    public class EndpointSaved
    {
        private readonly EndpointEntry entry;
        private readonly List<string> variables;
        private readonly CommandSettings settings;

        public EndpointSaved(EndpointEntry entry, List<string> variables, CommandSettings settings)
        {
            this.entry = entry;
            this.variables = variables ?? new List<string>();
            this.settings = settings;
        }

        public EndpointEntry GetEntry()
        {
            return entry;
        }

        public List<string> GetVariables()
        {
            return variables;
        }

        public CommandSettings GetSettings()
        {
            return settings;
        }
    }
}
