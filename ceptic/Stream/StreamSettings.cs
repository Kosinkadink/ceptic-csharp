using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream
{
    public class StreamSettings
    {
        public readonly int sendBufferSize;
        public readonly int readBufferSize;
        public readonly int frameMaxSize;
        public readonly int headersMaxSize;
        public readonly int streamTimeout;
        public readonly int handlerMaxCount;
        public bool verbose = false;

        public StreamSettings(int sendBufferSize, int readBufferSize,int frameMaxSize,
            int headersMaxSize, int streamTimeout, int handlerMaxCount)
        {
            this.sendBufferSize = sendBufferSize;
            this.readBufferSize = readBufferSize;
            this.frameMaxSize = frameMaxSize;
            this.headersMaxSize = headersMaxSize;
            this.streamTimeout = streamTimeout;
            this.handlerMaxCount = handlerMaxCount;
        }
    }
}
