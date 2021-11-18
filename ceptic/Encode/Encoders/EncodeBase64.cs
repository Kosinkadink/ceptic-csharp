using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Encode.Encoders
{
    class EncodeBase64 : IEncodeObject
    {
        public byte[] Encode(byte[] data)
        {
            return Encoding.ASCII.GetBytes(Convert.ToBase64String(data));
        }

        public byte[] Decode(byte[] data)
        {
            return Convert.FromBase64String(Encoding.ASCII.GetString(data));
        }
    }
}
