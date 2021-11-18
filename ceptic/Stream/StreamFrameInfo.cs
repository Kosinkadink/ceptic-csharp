using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream
{
    public class StreamFrameInfo
    {
        public static readonly StreamFrameInfo CONTINUE = new StreamFrameInfo('0');
        public static readonly StreamFrameInfo END = new StreamFrameInfo('1');

        public readonly char character;
        public readonly byte[] byteArray;

        StreamFrameInfo(char character)
        {
            this.character = character;
            byteArray = Encoding.UTF8.GetBytes(character.ToString());
        }

        public static StreamFrameInfo FromValue(string value)
        {
            try
            {
                switch (value[0])
                {
                    case '0':
                        return CONTINUE;
                    case '1':
                        return END;
                    default:
                        return null;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }
    }
}
