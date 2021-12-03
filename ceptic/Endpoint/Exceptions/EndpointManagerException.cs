using Ceptic.Common.Exceptions;
using System;

namespace Ceptic.Endpoint.Exceptions
{
    public class EndpointManagerException : CepticException
    {
        public EndpointManagerException()
        {

        }

        public EndpointManagerException(string message)
            : base(message)
        {

        }

        public EndpointManagerException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
