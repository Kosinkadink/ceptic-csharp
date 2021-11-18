using Ceptic.Common.Exceptions;
using System;

namespace Ceptic.Net.Exceptions
{
    public class SocketCepticException : CepticException
    {
        public SocketCepticException()
        {

        }

        public SocketCepticException(string message)
            : base(message)
        {

        }

        public SocketCepticException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
