using Ceptic.Encode;
using Ceptic.Net;
using Ceptic.Net.Exceptions;
using Ceptic.Stream.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Stream
{
    public class StreamFrame
    {
        public static readonly Guid NullId = Guid.Empty;

        private readonly Guid streamId;
        private readonly StreamFrameType type;
        private readonly StreamFrameInfo info;
        private byte[] data;

        // 16 zeroes
        private static readonly string zeroDataLength = "0000000000000000"; 

        public StreamFrame(Guid streamId, StreamFrameType type, StreamFrameInfo info, byte[] data)
        {
            this.streamId = streamId;
            this.type = type;
            this.info = info;
            this.data = data;
        }

        public Guid GetStreamId()
        {
            return streamId;
        }

        public StreamFrameType GetFrameType()
        {
            return type;
        }

        public StreamFrameInfo GetFrameInfo()
        {
            return info;
        }

        public byte[] GetData()
        {
            return data;
        }

        public int GetSize()
        {
            return 38 + data.Length;
        }

        public void EncodeData(EncodeHandler encodeHandler)
        {
            data = encodeHandler.Encode(data);
        }
        
        public void DecodeData(EncodeHandler encodeHandler)
        {
            data = encodeHandler.Decode(data);
        }

        public void Send(SocketCeptic s)
        {
            // send stream id
            s.SendRaw(Encoding.UTF8.GetBytes(streamId.ToString())); // TODO: replace with raw bytes
            // send type
            s.SendRaw(type.byteArray);
            // send info
            s.SendRaw(info.byteArray);
            // send data if data is present
            if (data.Length > 0)
                s.Send(data);
            else
                s.SendRaw(zeroDataLength);
        }

        /// <summary>
        /// Creates and returns instance of StreamFrame from data received via socket
        /// </summary>
        /// <exception cref="StreamFrameSizeException"></exception>
        /// <exception cref="SocketCepticException"></exception>
        /// <param name="s"></param>
        /// <param name="maxDataLength"></param>
        /// <returns></returns>
        public static StreamFrame FromSocket(SocketCeptic s, long maxDataLength)
        {
            // get stream id
            var rawStringId = s.RecvRawString(36);
            Guid streamId;
            try
            {
                streamId = Guid.Parse(rawStringId);
            }
            catch (FormatException e)
            {
                throw new StreamFrameSizeException($"Received stream id could not be parsed to Guid: {rawStringId}", e);
            }
            // get type
            var rawType = s.RecvRawString(1);
            var type = StreamFrameType.FromValue(rawType);
            // get info
            var rawInfo = s.RecvRawString(1);
            var info = StreamFrameInfo.FromValue(rawInfo);
            // verify type and info are valid
            if (type == null)
                throw new StreamFrameSizeException($"StreamFrameType '{rawType}' not recognized");
            if (info == null)
                throw new StreamFrameSizeException($"StreamFrameInfo '{rawInfo}' not recognized");
            // get data length
            var rawDataLength = s.RecvRawString(16);
            int dataLength;
            try
            {
                dataLength = int.Parse(rawDataLength);
            }
            catch (FormatException e)
            {
                throw new StreamFrameSizeException(
                    $"Received dataLength could not be parsed to int: {streamId},{type},{info},{rawDataLength}", e);
            }
            // if data length greater than max length, raise exception
            if (dataLength > maxDataLength)
                throw new StreamFrameSizeException($"DataLength ({dataLength}) greater than allowed max length {maxDataLength}");
            // if data length not zero, get data
            byte[] data = new byte[0];
            if (dataLength > 0)
                data = s.RecvRaw(dataLength);
            return new StreamFrame(streamId, type, info, data);
        }

        public bool IsHeader()
        {
            return type.Equals(StreamFrameType.HEADER);
        }

        public bool IsResponse()
        {
            return type.Equals(StreamFrameType.RESPONSE);
        }

        public bool IsData()
        {
            return type.Equals(StreamFrameType.DATA);
        }

        public bool IsKeepAlive()
        {
            return type.Equals(StreamFrameType.KEEP_ALIVE);
        }

        public bool IsClose()
        {
            return type.Equals(StreamFrameType.CLOSE);
        }

        public bool IsCloseAll()
        {
            return type.Equals(StreamFrameType.CLOSE_ALL);
        }

        public bool IsLast()
        {
            return info.Equals(StreamFrameInfo.END);
        }

        public bool IsContinued()
        {
            return info.Equals(StreamFrameInfo.CONTINUE);
        }

        public bool IsDataLast()
        {
            return IsData() && IsLast();
        }

        public bool IsDataContinued()
        {
            return IsData() && IsContinued();
        }
    }
}
