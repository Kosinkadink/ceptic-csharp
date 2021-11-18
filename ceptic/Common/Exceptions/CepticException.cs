using System;

namespace Ceptic.Common.Exceptions
{
    public class CepticException : Exception
    {
        public CepticException()
        {

        }

        public CepticException(string message)
            : base(message)
        {

        }

        public CepticException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
