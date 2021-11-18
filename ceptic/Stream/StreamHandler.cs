using Ceptic.Common;
using Ceptic.Encode;
using Ceptic.Encode.Exceptions;
using Ceptic.Stream.Exceptions;
using Ceptic.Stream.Iteration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Ceptic.Stream
{
    class StreamHandler : IDisposable
    {
        private readonly BlockingCollection<StreamFrame> readBuffer;
        private readonly BlockingCollection<StreamFrame> managerSendBuffer;
        private const int bufferWaitTimeout = 100;

        private int sendBufferCounter = 0;
        private int readBufferCounter = 0;

        private readonly ManualResetEventSlim eventSendBufferDecreased = new ManualResetEventSlim();
        private readonly ManualResetEventSlim eventReadBufferDecreased = new ManualResetEventSlim();

        private readonly Guid streamId;

        private readonly StreamSettings settings;

        private readonly Stopwatch existenceTimer = new Stopwatch();
        private Timer keepAliveTimer;

        private readonly CancellationTokenSource cancellationSource;

        private bool isTimedOut = false;
        private bool shouldStop = false;

        private EncodeHandler encodeHandler = new EncodeHandler(new List<IEncodeObject>()
        {
            EncodeType.None.GetEncoder()
        });

        public StreamHandler(Guid streamId, StreamSettings settings, BlockingCollection<StreamFrame> managerSendBuffer)
        {
            this.streamId = streamId;
            this.settings = settings;
            this.managerSendBuffer = managerSendBuffer;

            readBuffer = new BlockingCollection<StreamFrame>(new ConcurrentQueue<StreamFrame>());

            cancellationSource = new CancellationTokenSource();

            StartTimers();
        }

        public Guid GetStreamId()
        {
            return streamId;
        }

        public StreamSettings GetSettings()
        {
            return settings;
        }

        /// <summary>
        /// Sets encode behavior for all frames sent and received through this handler
        /// </summary>
        /// <exception cref="UnknownEncodingException">
        /// Thrown if string contains unknown encodings
        /// </exception>
        /// <param name="encodingString"></param>
        public void SetEncode(string encodingString)
        {
            encodeHandler = EncodeGetter.Get(encodingString);
        }

        #region Timers
        private void StartTimers()
        {
            if (!existenceTimer.IsRunning)
            {
                existenceTimer.Start();
                keepAliveTimer = new Timer(OnTimedOutEvent, null, TimeSpan.FromSeconds(settings.streamTimeout), Timeout.InfiniteTimeSpan);
            }
        }

        public void UpdateKeepAlive()
        {
            keepAliveTimer.Change(TimeSpan.FromSeconds(settings.streamTimeout), Timeout.InfiniteTimeSpan);
        }

        public bool IsTimedOut()
        {
            return isTimedOut;
            //return keepAliveTimer.Elapsed.TotalSeconds > settings.streamTimeout;
        }

        public void OnTimedOutEvent(Object source)
        {
            isTimedOut = true;
            Stop();
        }
        #endregion

        #region Buffer Checks
        public bool IsSendBufferFull()
        {
            return sendBufferCounter >= settings.sendBufferSize;
        }

        public bool IsReadBufferFull()
        {
            return readBufferCounter >= settings.readBufferSize;
        }

        public void IncrementSendBuffer(StreamFrame frame)
        {
            Interlocked.Add(ref sendBufferCounter, frame.GetSize());
        }

        public void DecrementSendBuffer(StreamFrame frame)
        {
            Interlocked.Add(ref sendBufferCounter, frame.GetSize() * -1);
            try
            {
                eventSendBufferDecreased.Set();
            }
            finally
            {
                eventSendBufferDecreased.Reset();
            }
        }

        protected void IncrementReadBuffer(StreamFrame frame)
        {
            Interlocked.Add(ref readBufferCounter, frame.GetSize());
        }

        protected void DecrementReadBuffer(StreamFrame frame)
        {
            Interlocked.Add(ref readBufferCounter, frame.GetSize() * -1);
            try
            {
                eventReadBufferDecreased.Set();
            }
            finally
            {
                eventReadBufferDecreased.Reset();
            }
        }
        #endregion

        #region Send
        /// <summary>
        /// Add frame to send buffer
        /// </summary>
        /// <exception cref="StreamException"></exception>
        /// <exception cref="StreamHandlerStoppedException"></exception>
        /// <param name="frame"></param>
        private void Send(StreamFrame frame)
        {
            if (IsStopped())
                throw new StreamHandlerStoppedException("handler is stopped; cannot send frames through a stopped handler");
            // update keep alive
            UpdateKeepAlive();
            // encode data on frame
            frame.EncodeData(encodeHandler);
            // increment send buffer counter
            IncrementSendBuffer(frame);
            // while not stopped, attempt to insert frame into buffer
            while (!IsStopped())
            {
                if (IsSendBufferFull())
                {
                    // wait until buffer space is available
                    eventSendBufferDecreased.Wait(bufferWaitTimeout);
                    continue;
                }
                if (managerSendBuffer.TryAdd(frame, bufferWaitTimeout))
                    break;
            }
        }

        /// <summary>
        /// Add all frames in enumerable to send buffer
        /// </summary>
        /// <exception cref="StreamException"></exception>
        /// <exception cref="StreamHandlerStoppedException"></exception>
        /// <param name="frames"></param>
        private void SendAll(IEnumerable<StreamFrame> frames)
        {
            foreach(var frame in frames)
            {
                Send(frame);
            }
        }

        /// <summary>
        /// Send data by converting to StreamFrame instances and adding them to send buffer
        /// </summary>
        /// <exception cref="StreamException"></exception>
        /// <param name="data"></param>
        /// <param name="isFirstHeader"></param>
        /// <param name="isResponse"></param>
        private void SendData(byte[] data, bool isFirstHeader, bool isResponse)
        {
            SendAll(new DataStreamFrameGenerator(streamId, data, settings.frameMaxSize, isFirstHeader, isResponse).Frames());
        }

        /// <summary>
        /// Send data by converting to StreamFrame instances and adding them to send buffer
        /// </summary>
        /// <exception cref="StreamException"></exception>
        /// <param name="data"></param>
        public void SendData(byte[] data)
        {
            SendData(data, false, false);
        }

        /// <summary>
        /// Send request as data converted into StreamFrames added to send buffer
        /// </summary>
        /// <exception cref="StreamException"></exception>
        /// <param name="request"></param>
        public void SendRequest(CepticRequest request)
        {
            SendData(request.GetData(), true, false);
        }

        /// <summary>
        /// Send response as data converted into StreamFrames added to send buffer
        /// </summary>
        /// <param name="response"></param>
        public void SendResponse(CepticResponse response)
        {
            SendData(response.GetData(), false, true);
        }

        /// <summary>
        /// Sends close frame and stops stream handler
        /// </summary>
        /// <param name="data"></param>
        public void SendClose(byte[] data)
        {
            if (data == null)
                data = new byte[0];
            try
            {
                Send(StreamFrameHelper.CreateClose(streamId, data));
            }
            catch (StreamException) { }
            Stop();
        }

        /// <summary>
        /// Sends close frame and stops stream handler
        /// </summary>
        /// <param name="data"></param>
        public void SendClose(string data)
        {
            SendClose(Encoding.UTF8.GetBytes(data));
        }

        /// <summary>
        /// Send close frame and stops stream handler
        /// </summary>
        public void SendClose()
        {
            SendClose((byte[]) null);
        }
        #endregion

        #region Read
        /// <summary>
        /// Add StreamFrame to read buffer (called by StreamManager to populate read buffer for use with read methods)
        /// </summary>
        /// <exception cref="StreamHandlerStoppedException"></exception>
        /// <param name="frame"></param>
        public void AddToRead(StreamFrame frame)
        {
            if (IsStopped())
                throw new StreamHandlerStoppedException("handler is stopped; cannot send frames through a stopped handler");
            // update keep alive
            UpdateKeepAlive();
            // while not stopped, attempt to insert frame into buffer
            IncrementReadBuffer(frame);
            while (!IsStopped())
            {
                if (IsReadBufferFull())
                {
                    eventReadBufferDecreased.Wait(bufferWaitTimeout);
                    continue;
                }
                if (readBuffer.TryAdd(frame, bufferWaitTimeout))
                    break;
            }
        }

        private StreamFrame ReadNextFrame()
        {
            return ReadNextFrame(settings.streamTimeout);
        }

        private StreamFrame ReadNextFrame(int timeout)
        {
            StreamFrame frame;
            // if timeout is less than 0, then block and wait to get next frame (up to stream timeout)
            if (timeout < 0)
                readBuffer.TryTake(out frame, TimeSpan.FromSeconds(settings.streamTimeout));
            // if timeout is 0, do not block and immediately return
            else if (timeout == 0)
                readBuffer.TryTake(out frame, 0);
            // otherwise wait up to specified time (bounded by stream timeout)
            else
                readBuffer.TryTake(out frame, TimeSpan.FromSeconds(Math.Min(timeout, settings.streamTimeout)));
            // if frame not null
            if (frame != null)
            {
                // decrement read buffer counter
                DecrementReadBuffer(frame);
                // decode frame data
                frame.DecodeData(encodeHandler);
                // if close frame, throw exception
                if (frame.IsClose())
                {
                    throw new StreamClosedException(Encoding.UTF8.GetString(frame.GetData()));
                }
                return frame;
            }
            // if handler is stopped and no additional frames to rad, throw exception
            if (IsStopped() && readBuffer.Count == 0)
                throw new StreamHandlerStoppedException("handler is stopped and no frames in read buffer; cannot read frames through a stopped handler");
            // return null since frame will be null here
            return null;
        }

        private StreamData ReadData(int timeout, long maxLength, bool convertResponse)
        {
            var frames = new List<StreamFrame>();
            var totalLength = 0;
            bool isResponse = false;

            while (true)
            {
                StreamFrame frame = ReadNextFrame(timeout);
                // if frame was null, stop listening for more frames
                if (frame == null)
                    break;
                // add data
                frames.Add(frame);
                totalLength += frame.GetData().Length;
                // check if max length provided and if past limit
                if (maxLength > 0 && totalLength > maxLength)
                {
                    Stop();
                    throw new StreamTotalDataSizeException($"total data received has surpassed maxLength of {maxLength}");
                }
                if (frame.IsResponse())
                    isResponse = true;
                // if frame is last, stop listening for more frames
                if (frame.IsLast())
                    break;
            }
            // combine frame data
            var finalArray = new byte[totalLength];
            int index = 0;
            foreach(var frame in frames)
            {
                Array.Copy(frame.GetData(), 0, finalArray, index, frame.GetData().Length);
                index += frame.GetData().Length;
            }
            // convert to response, if necessary
            if (isResponse && convertResponse)
                return new StreamData(CepticResponse.FromData(finalArray));
            return new StreamData(finalArray);
        }

        public StreamData ReadData(int timeout, long maxLength)
        {
            return ReadData(timeout, maxLength, true);
        }

        public StreamData ReadData(long maxLength)
        {
            return ReadData(settings.streamTimeout, maxLength);
        }

        public byte[] ReadDataRaw(int timeout, long maxLength)
        {
            return ReadData(timeout, maxLength, false).GetData();
        }

        public byte[] ReadDataRaw(long maxLength)
        {
            return ReadDataRaw(settings.streamTimeout, maxLength);
        }

        public byte[] ReadHeaderDataRaw()
        {
            // length should be no more than: headersMaxSize + command + endpoint + 2*\r\n (4 bytes)
            return ReadDataRaw(settings.streamTimeout, settings.headersMaxSize + Constants.COMMAND_LENGTH + Constants.ENDPOINT_LENGTH + 4);
        }
        #endregion

        #region Stopped
        public void Stop()
        {
            shouldStop = true;
            cancellationSource.Cancel();
        }

        public bool IsStopped()
        {
            return shouldStop;
        }

        public void Dispose()
        {
            Stop();
            keepAliveTimer.Dispose();
            readBuffer.Dispose();
            cancellationSource.Dispose();
        }
        ~StreamHandler()
        {
            Dispose();
        }
        #endregion

    }
}
