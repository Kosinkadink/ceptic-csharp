using Ceptic.Common;
using System;
using System.Collections.Generic;

namespace Ceptic.Endpoint
{
    public delegate CepticResponse EndpointEntry(CepticRequest request, Dictionary<string, string> values);
}
