using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream.Exceptions
{
    class StreamTotalDataSizeException : StreamException
    {
        public StreamTotalDataSizeException()
        {

        }

        public StreamTotalDataSizeException(string message)
            : base(message)
        {

        }

        public StreamTotalDataSizeException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
