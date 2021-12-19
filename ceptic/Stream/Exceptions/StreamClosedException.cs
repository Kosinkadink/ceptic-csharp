using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream.Exceptions
{
    public class StreamClosedException : StreamException
    {
        public StreamClosedException()
        {

        }

        public StreamClosedException(string message)
            : base(message)
        {

        }

        public StreamClosedException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
