using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Ceptic.Endpoint
{
    public class EndpointPattern
    {
        private readonly Regex pattern;
        private readonly List<string> variables;

        public EndpointPattern(Regex pattern, List<string> variables)
        {
            this.pattern = pattern;
            this.variables = variables ?? new List<string>();
        }

        public Regex GetPattern()
        {
            return pattern;
        }

        public List<string> GetVariables()
        {
            return variables;
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return pattern.ToString().Equals(((EndpointPattern)obj).pattern.ToString());
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return pattern.ToString().GetHashCode();
        }
    }
}
