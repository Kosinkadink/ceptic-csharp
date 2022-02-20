using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Security.Exceptions
{
    class SecurityPEMException : SecurityException
    {
        public SecurityPEMException()
        {

        }

        public SecurityPEMException(string message)
            : base(message)
        {

        }

        public SecurityPEMException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
