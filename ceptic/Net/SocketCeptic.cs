using Ceptic.Net.Exceptions;
using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Ceptic.Net
{
    public class SocketCeptic
    {
        private readonly System.IO.Stream s;
        private readonly TcpClient c;

        public SocketCeptic(NetworkStream stream, TcpClient client)
        {
            s = stream;
            c = client;
        }

        public SocketCeptic(SslStream stream, TcpClient client)
        {
            s = stream;
            c = client;
        }

        #region Send
        public void SendRaw(byte[] msg)
        {
            try
            {
                s.Write(msg, 0, msg.Length);
            }
            catch (IOException e)
            {
                Close();
                throw new SocketCepticException(e.Message, e);
            }
        }

        public void SendRaw(string msg)
        {
            SendRaw(Encoding.UTF8.GetBytes(msg));
        }

        public void Send(byte[] msg)
        {
            byte[] totalSize = Encoding.UTF8.GetBytes(string.Format("{0,16}", msg.Length));
            SendRaw(totalSize);
            SendRaw(msg);
        }

        public void Send(string msg)
        {
            Send(Encoding.UTF8.GetBytes(msg));
        }
        #endregion

        #region Receive
        public byte[] RecvRaw(int bytes)
        {
            try
            {
                var charCount = 0;
                var totalCount = 0;
                var byteBuffer = new byte[bytes];
                // repeat until all bytes received
                while (totalCount < bytes)
                {
                    charCount = s.Read(byteBuffer, totalCount, bytes-totalCount);
                    totalCount += charCount;
                    // if nothing received, done receiving regardless of expectation
                    if (charCount == 0)
                        break;
                }
                return byteBuffer;
            }
            catch (IOException e)
            {
                Close();
                throw new SocketCepticException(e.Message, e);
            }
        }

        public string RecvRawString(int bytes)
        {
            return Encoding.UTF8.GetString(RecvRaw(bytes));
        }

        public byte[] RecvBytes(int bytes)
        {
            // get length of bytes
            var sizeBuffer = RecvRaw(16);
            int sizeToReceive;
            try
            {
                sizeToReceive = int.Parse(Encoding.UTF8.GetString(sizeBuffer));
            }
            catch (FormatException e)
            {
                throw new SocketCepticException("size to receive was not the right format to convert to Int32", e);
            }
            // expect size to receive to be either sizeToReceive or byte amount, whichever is lower 
            var amount = bytes;
            if (sizeToReceive < amount)
                amount = sizeToReceive;
            return RecvRaw(amount);
        }

        public string RecvString(int bytes)
        {
            return Encoding.UTF8.GetString(RecvBytes(bytes));
        }
        #endregion

        public System.IO.Stream GetStream()
        {
            return s;
        }

        public TcpClient GetClient()
        {
            return c;
        }

        public void Close()
        {
            s.Close();
            c.Close();
        }

    }
}
