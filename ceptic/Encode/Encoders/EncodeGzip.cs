using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Ceptic.Encode.Encoders
{
    class EncodeGzip : IEncodeObject
    {
        public byte[] Encode(byte[] data)
        {
            using (var resultStream = new MemoryStream())
            using (var zipStream = new GZipStream(resultStream, CompressionMode.Compress))
            {
                zipStream.Write(data, 0, data.Length);
                zipStream.Close();
                return resultStream.ToArray();
            }
        }
        public byte[] Decode(byte[] data)
        {
            using (var initialStream = new MemoryStream(data))
            using (var zipStream = new GZipStream(initialStream, CompressionMode.Decompress))
            using (var resultStream = new MemoryStream())
            {
                zipStream.CopyTo(resultStream);
                return resultStream.ToArray();
            }
        }
    }
}
