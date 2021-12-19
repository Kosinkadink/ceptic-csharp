using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream.Exceptions
{
    public class StreamHandlerStoppedException : StreamClosedException
    {
        public StreamHandlerStoppedException()
        {

        }

        public StreamHandlerStoppedException(string message)
            : base(message)
        {

        }

        public StreamHandlerStoppedException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
