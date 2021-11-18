using Ceptic.Common.Exceptions;
using System;

namespace Ceptic.Stream.Exceptions
{
    class StreamException : CepticException
    {
        public StreamException()
        {

        }

        public StreamException(string message)
            : base(message)
        {

        }

        public StreamException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
