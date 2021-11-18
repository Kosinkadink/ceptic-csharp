using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Encode
{
    public interface IEncodeObject
    {
        public byte[] Encode(byte[] data);
        public byte[] Decode(byte[] data);
    }
}
