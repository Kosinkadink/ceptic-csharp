using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream.Exceptions
{
    class StreamTimeoutException : StreamException
    {
        public StreamTimeoutException()
        {

        }

        public StreamTimeoutException(string message)
            : base(message)
        {

        }

        public StreamTimeoutException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
