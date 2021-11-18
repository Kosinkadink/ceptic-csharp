using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common.Exceptions
{
    class CepticRequestVerifyException : CepticException
    {
        public CepticRequestVerifyException()
        {

        }

        public CepticRequestVerifyException(string message)
            : base(message)
        {

        }

        public CepticRequestVerifyException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
