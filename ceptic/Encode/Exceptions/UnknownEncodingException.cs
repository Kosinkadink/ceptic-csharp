using Ceptic.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Encode.Exceptions
{
    class UnknownEncodingException : CepticException
    {
        public UnknownEncodingException()
        {

        }

        public UnknownEncodingException(string message)
            : base(message)
        {

        }

        public UnknownEncodingException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
