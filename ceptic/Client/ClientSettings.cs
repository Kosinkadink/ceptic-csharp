using Ceptic.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Client
{
    class ClientSettings
    {
        public string version { get; }
        public int headersMinSize { get; }
        public int headersMaxSize { get; }
        public int frameMinSize { get; }
        public int frameMaxSize { get; }
        public int bodyMax { get; }
        public int streamMinTimeout { get; }
        public int streamTimeout { get; }
        public int sendBufferSize { get; }
        public int readBufferSize { get; }
        public int defaultPort { get; }

        public ClientSettings(
            string version="1.0.0",
            int headersMinSize = 1024000, int headersMaxSize = 1024000,
            int frameMinSize = 1024000, int frameMaxSize = 1024000,
            int bodyMax = 102400000,
            int streamMinTimeout = 1, int streamTimeout = 5,
            int readBufferSize = 102400000, int sendBufferSize = 102400000,
            int? defaultPort = null)
        {
            // TODO: add verification for settings
            this.version = version;
            this.headersMinSize = headersMinSize;
            this.headersMaxSize = headersMaxSize;
            this.frameMinSize = frameMinSize;
            this.frameMaxSize = frameMaxSize;
            this.bodyMax = bodyMax;
            this.streamMinTimeout = streamMinTimeout;
            this.streamTimeout = streamTimeout;
            this.readBufferSize = readBufferSize;
            this.sendBufferSize = sendBufferSize;
            if (defaultPort == null)
                this.defaultPort = Constants.DEFAULT_PORT;
            else
                this.defaultPort = (int) defaultPort;
        }
    }
}
