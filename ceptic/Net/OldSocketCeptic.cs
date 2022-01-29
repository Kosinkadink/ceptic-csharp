using Ceptic.Net.Exceptions;
using System;
using System.Net.Sockets;
using System.Text;

namespace Ceptic.Net
{
    public class OldSocketCeptic
    {
        private readonly Socket s;

        public OldSocketCeptic(Socket socket)
        {
            s = socket;
        }

        #region Send
        public void SendRaw(byte[] msg)
        {
            try
            {
                var totalCount = 0;
                // repeat until all bytes sent
                while (totalCount < msg.Length)
                {
                    var sent = s.Send(msg, totalCount, msg.Length-totalCount, SocketFlags.None);
                    totalCount += sent;
                }
            }
            catch (SocketException e)
            {
                throw new SocketCepticException(e.Message);
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
                    charCount = s.Receive(byteBuffer, totalCount, bytes-totalCount, SocketFlags.None);
                    totalCount += charCount;
                    // if nothing received, done receiving regardless of expectation
                    if (charCount == 0)
                        break;
                }
                return byteBuffer;
            }
            catch (SocketException e)
            {
                throw new SocketCepticException(e.Message);
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
            catch (FormatException)
            {
                throw new SocketCepticException("size to receive was not the right format to convert to Int32");
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

        public Socket GetSocket()
        {
            return s;
        }

        public void Close()
        {
            // TODO: maybe add s.Shutdown?
            s.Close();
        }

    }
}
