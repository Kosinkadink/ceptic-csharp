using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream
{
    public class StreamFrameType
    {
        public static readonly StreamFrameType DATA = new StreamFrameType('0');
        public static readonly StreamFrameType HEADER = new StreamFrameType('1');
        public static readonly StreamFrameType RESPONSE = new StreamFrameType('2');
        public static readonly StreamFrameType KEEP_ALIVE = new StreamFrameType('3');
        public static readonly StreamFrameType CLOSE = new StreamFrameType('4');
        public static readonly StreamFrameType CLOSE_ALL = new StreamFrameType('5');

        public readonly char character;
        public readonly byte[] byteArray;

        StreamFrameType(char character)
        {
            this.character = character;
            byteArray = Encoding.UTF8.GetBytes(character.ToString());
        }

        public static StreamFrameType FromValue(string value)
        {
            try
            {
                switch (value[0])
                {
                    case '0':
                        return DATA;
                    case '1':
                        return HEADER;
                    case '2':
                        return RESPONSE;
                    case '3':
                        return KEEP_ALIVE;
                    case '4':
                        return CLOSE;
                    case '5':
                        return CLOSE_ALL;
                    default:
                        return null;
                }
            }
            catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return character.ToString();
        }
    }
}
