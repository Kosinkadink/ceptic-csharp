using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream.Exceptions
{
    class StreamHandlerStoppedException : StreamException
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
