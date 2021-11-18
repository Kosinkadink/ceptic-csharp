using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream
{
    public class StreamFrameHelper
    {
        #region Header Frames
        public static StreamFrame CreateHeader(Guid streamId, byte[] data, StreamFrameInfo info)
        {
            return new StreamFrame(streamId, StreamFrameType.HEADER, info, data);
        }

        public static StreamFrame CreateHeaderLast(Guid streamId, byte[] data)
        {
            return CreateHeader(streamId, data, StreamFrameInfo.END);
        }

        public static StreamFrame CreateHeaderContinued(Guid streamId, byte[] data)
        {
            return CreateHeader(streamId, data, StreamFrameInfo.CONTINUE);
        }
        #endregion

        #region Response Frames
        public static StreamFrame CreateResponse(Guid streamId, byte[] data, StreamFrameInfo info)
        {
            return new StreamFrame(streamId, StreamFrameType.RESPONSE, info, data);
        }

        public static StreamFrame CreateResponseLast(Guid streamId, byte[] data)
        {
            return CreateResponse(streamId, data, StreamFrameInfo.END);
        }

        public static StreamFrame CreateResponseContinued(Guid streamId, byte[] data)
        {
            return CreateResponse(streamId, data, StreamFrameInfo.CONTINUE);
        }
        #endregion

        #region Data Frames
        public static StreamFrame CreateData(Guid streamId, byte[] data, StreamFrameInfo info)
        {
            return new StreamFrame(streamId, StreamFrameType.DATA, info, data);
        }

        public static StreamFrame CreateDataLast(Guid streamId, byte[] data)
        {
            return CreateData(streamId, data, StreamFrameInfo.END);
        }

        public static StreamFrame CreateDataContinued(Guid streamId, byte[] data)
        {
            return CreateData(streamId, data, StreamFrameInfo.CONTINUE);
        }
        #endregion

        #region Keep Alive Frames
        public static StreamFrame CreateKeepAlive(Guid streamId)
        {
            return new StreamFrame(streamId, StreamFrameType.KEEP_ALIVE, StreamFrameInfo.END, new byte[0]);
        }
        #endregion

        #region Close Frames
        public static StreamFrame CreateClose(Guid streamId, byte[] data)
        {
            return new StreamFrame(streamId, StreamFrameType.CLOSE, StreamFrameInfo.END, data);
        }

        public static StreamFrame CreateClose(Guid streamId)
        {
            return CreateClose(streamId, new byte[0]);
        }

        public static StreamFrame CreateCloseAll()
        {
            return new StreamFrame(StreamFrame.NullId, StreamFrameType.CLOSE_ALL, StreamFrameInfo.END, new byte[0]);
        }
        #endregion
    }
}
