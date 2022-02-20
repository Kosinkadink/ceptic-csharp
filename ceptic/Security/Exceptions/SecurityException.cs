using Ceptic.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Security.Exceptions
{
    public class SecurityException : CepticException
    {
        public SecurityException()
        {

        }

        public SecurityException(string message)
            : base(message)
        {

        }

        public SecurityException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
