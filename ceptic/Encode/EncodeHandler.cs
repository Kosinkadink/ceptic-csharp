using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Encode
{
    public class EncodeHandler
    {
        private readonly List<IEncodeObject> encoderList;

        public EncodeHandler(List<IEncodeObject> encoderList)
        {
            this.encoderList = encoderList;
        }

        public byte[] Encode(byte[] data)
        {
            foreach (var encoder in encoderList)
            {
                data = encoder.Encode(data);
            }
            return data;
        }

        public byte[] Decode(byte[] data)
        {
            // decode in reverse order
            for (int i = encoderList.Count; i >= 0; i--)
            {
                data = encoderList[i].Decode(data);
            }
            return data;
        }
    }
}
