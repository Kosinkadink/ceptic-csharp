using Ceptic.Encode.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Encode
{
    public class EncodeGetter
    {
        public static EncodeHandler Get(string encodingString)
        {
            if (encodingString == null || encodingString.Length == 0)
                return new EncodeHandler(new List<IEncodeObject>()
                {
                    EncodeType.None.GetEncoder()
                });
            string[] encodings = encodingString.Split(",");
            return Get(encodings);
        }

        public static EncodeHandler Get(string[] encodings)
        {
            var encoders = new List<IEncodeObject>();
            var uniqueTypes = new HashSet<EncodeType>();
            foreach(var encoding in encodings)
            {
                var encodeType = EncodeType.FromValue(encoding);
                if (encodeType == null)
                    throw new UnknownEncodingException($"EncodeType '{encoding}' not recognized");
                // if encoder is none, just use this encoding type and break
                if (encodeType.Equals(EncodeType.None))
                {
                    encoders = new List<IEncodeObject>();
                    encoders.Add(encodeType.GetEncoder());
                    break;
                }
                // if encoder is unique, add to encoder list (and to unique types)
                if (!uniqueTypes.Contains(encodeType))
                {
                    encoders.Add(encodeType.GetEncoder());
                    uniqueTypes.Add(encodeType);
                }
            }
            return new EncodeHandler(encoders);
        }

    }
}
