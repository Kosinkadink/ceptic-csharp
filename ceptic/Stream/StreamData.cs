using Ceptic.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream
{
    public class StreamData
    {
        private readonly CepticResponse response;
        private readonly byte[] data;

        public StreamData(CepticResponse response)
        {
            this.response = response;
        }

        public StreamData(byte[] data)
        {
            this.data = data;
        }

        public CepticResponse GetResponse()
        {
            return response;
        }

        public byte[] GetData()
        {
            return data;
        }

        public bool IsResponse()
        {
            return response != null;
        }

        public bool IsData()
        {
            return data != null;
        }

        public bool IsEmpty()
        {
            return !IsData() && !IsResponse();
        }
    }
}
