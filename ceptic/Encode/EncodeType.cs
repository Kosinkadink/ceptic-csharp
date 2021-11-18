using Ceptic.Encode.Encoders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Encode
{
    class EncodeType
    {
        public static readonly EncodeType None = new EncodeType("none", new EncodeNone());
        public static readonly EncodeType Base64 = new EncodeType("base64", new EncodeBase64());
        public static readonly EncodeType Gzip = new EncodeType("gzip", new EncodeGzip());

        private static readonly Dictionary<string, EncodeType> BY_STRING = new Dictionary<string, EncodeType>()
        {
            { None.GetValue(), None },
            { Base64.GetValue(), Base64 },
            { Gzip.GetValue(), Gzip }
        };

        private readonly string value;
        private readonly IEncodeObject encoder;

        EncodeType(string value, IEncodeObject encoder)
        {
            this.value = value;
            this.encoder = encoder;
        }

        public string GetValue()
        {
            return value;
        }

        public IEncodeObject GetEncoder()
        {
            return encoder;
        }

        public static EncodeType FromValue(string value)
        {
            return BY_STRING.GetValueOrDefault(value);
        }

    }
}
