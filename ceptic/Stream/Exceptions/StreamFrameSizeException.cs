using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream.Exceptions
{
    class StreamFrameSizeException : StreamException
    {
        public StreamFrameSizeException()
        {

        }

        public StreamFrameSizeException(string message)
            : base(message)
        {

        }

        public StreamFrameSizeException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
