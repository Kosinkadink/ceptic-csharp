using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Encode.Encoders
{
    public class EncodeNone : IEncodeObject
    {
        public byte[] Encode(byte[] data)
        {
            return data;
        }

        public byte[] Decode(byte[] data)
        {
            return data;
        }
    }
}
