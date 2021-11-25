using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common.Exceptions
{
    class CepticIOException : CepticException
    {
        public CepticIOException()
        {

        }

        public CepticIOException(string message)
            : base(message)
        {

        }

        public CepticIOException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
