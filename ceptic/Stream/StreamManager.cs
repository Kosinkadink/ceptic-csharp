using Ceptic.Common;
using Ceptic.Net;
using Ceptic.Net.Exceptions;
using Ceptic.Stream.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ceptic.Stream
{
    class StreamManager : IStreamManager
    {
        private readonly SocketCeptic socket;
        private readonly Guid managerId;
        private readonly string destination;
        private readonly StreamSettings settings;
        private readonly IRemovableManagers removable;
        private readonly bool isServer;

        private readonly Stopwatch existenceTimer = new Stopwatch();
        private Timer keepAliveTimer;

        private readonly CancellationTokenSource cancellationSource;
        private readonly CancellationToken cancellationToken;

        private readonly BlockingCollection<StreamFrame> managerSendBuffer;
        private const int bufferWaitTimeout = 100;

        private bool isTimedOut = false;
        private bool shouldStop = false;
        private bool fullyStopped = false;
        private string stopReason = "";
        private bool alreadyRemoved = false;

        private readonly ConcurrentDictionary<Guid, StreamHandler> streams = new ConcurrentDictionary<Guid, StreamHandler>();

        public StreamManager(SocketCeptic socket, Guid managerId, string destination, StreamSettings settings,
            IRemovableManagers removable, bool isServer)
        {
            this.socket = socket;
            this.managerId = managerId;
            this.destination = destination;
            this.settings = settings;
            this.removable = removable;
            this.isServer = isServer;

            managerSendBuffer = new BlockingCollection<StreamFrame>(new ConcurrentQueue<StreamFrame>());

            cancellationSource = new CancellationTokenSource();
            cancellationToken = cancellationSource.Token;
        }

        public Guid GetManagerId()
        {
            return managerId;
        }

        public string GetDestination()
        {
            return destination;
        }

        public void Start()
        {
            StartTimers();

            new Task(() => SendMonitor(), cancellationToken, TaskCreationOptions.LongRunning).Start();
            new Task(() => ReceiveMonitor(), cancellationToken, TaskCreationOptions.LongRunning).Start();
        }

        private void SendMonitor()
        {
            try
            {
                while(!shouldStop)
                {
                    StreamFrame frame = null;
                    // try to get frame from send queue
                    bool obtained = managerSendBuffer.TryTake(out frame, bufferWaitTimeout, cancellationToken);
                    
                    if (obtained)
                    {
                        // if close all frame, send and then immediately stop manager
                        if (frame.IsCloseAll())
                        {
                            // update keep alive; close_all frame about to be sent, so stream must be active
                            UpdateKeepAlive();
                            // try to send frame
                            try
                            {
                                frame.Send(socket);
                            }
                            catch (SocketCepticException e)
                            {
                                // trigger manager to stop if problem with socket
                                Stop($"exception while sending frame: {e}");
                                break;
                            }
                            Stop($"sending close_all from handler {frame.GetStreamId()}");
                            break;
                        }
                        // get requesting handler
                        var handler = streams.GetValueOrDefault(frame.GetStreamId());
                        if (handler != null)
                        {
                            // update keep alive; frame about to be sent from valid handler, so stream must be active
                            UpdateKeepAlive();
                            // decrement size of handler's send buffer
                            handler.DecrementSendBuffer(frame);
                            // try to send frame
                            try
                            {
                                frame.Send(socket);
                            }
                            catch (SocketCepticException e)
                            {
                                // trigger manager to stop if problem with socket
                                Stop($"exception while sending frame: {e}");
                                break;
                            }
                            // if sent close frame, close handler
                            if (frame.IsClose())
                                RemoveHandler(handler);
                        }
                    }
                }
            }
            catch (Exception e) when (e is OperationCanceledException || e is ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception e)
            {
                Stop($"[SendMonitor] Unexpected exception: {e}");
                throw e;
            }
        }

        private void ReceiveMonitor()
        {
            try
            {
                while (!shouldStop)
                {
                    // try to get frame from socket
                    StreamFrame frame;
                    try
                    {
                        frame = StreamFrame.FromSocket(socket, settings.frameMaxSize);
                    }
                    catch (Exception e) when (e is StreamFrameSizeException || e is SocketCepticException)
                    {
                        Stop($"exception while receiving frame: {e}");
                        break;
                    }
                    // update keep alive timer; just received frame, so connection must be alive
                    UpdateKeepAlive();
                    // if keep alive frame, update keep alive on handler and keep processing; just there to keep connection alive
                    if (frame.IsKeepAlive())
                    {
                        var handler = streams.GetValueOrDefault(frame.GetStreamId());
                        handler?.UpdateKeepAlive();
                    }
                    // if handler is to be closed, add frame and it will take care of itself
                    else if (frame.IsClose())
                    {
                        var handler = streams.GetValueOrDefault(frame.GetStreamId());
                        try
                        {
                            handler?.AddToRead(frame);
                        }
                        catch (StreamHandlerStoppedException)
                        {
                            continue;
                        }
                    }
                    // if close all, stop manager
                    else if (frame.IsCloseAll())
                    {
                        Stop("received close_all addressed to handler " + frame.GetStreamId());
                        break;
                    }
                    // if server and header frame, create new handler and pass frame
                    else if (isServer && frame.IsHeader())
                    {
                        var handler = CreateHandler(frame.GetStreamId());
                        // if handler couldn't be created, something is wrong and should stop manager
                        if (handler == null)
                        {
                            Stop($"couldn't create handler - possible duplicate for handler {frame.GetStreamId()}");
                            break;
                        }
                        if (IsHandlerLimitReached())
                        {
                            handler.SendClose("Handler limit reached");
                            RemoveHandler(handler);
                            continue;
                        }
                        try
                        {
                            handler.AddToRead(frame);
                        } catch (StreamHandlerStoppedException)
                        {
                            continue;
                        }
                        // create task to run removable.HandleNewConnection to continue comms with handler
                        new Task(() => removable.HandleNewConnection(handler), cancellationToken, TaskCreationOptions.LongRunning).Start();
                    }
                    // otherwise try to pass frame to appropriate handler
                    else
                    {
                        var handler = streams.GetValueOrDefault(frame.GetStreamId());
                        try
                        {
                            handler?.AddToRead(frame);
                        }
                        catch (StreamHandlerStoppedException)
                        {
                            
                        }
                    }
                }
            }
            catch (Exception e) when (e is OperationCanceledException || e is ObjectDisposedException)
            {
                // ignore
            }
            catch (Exception e)
            {
                Stop($"[ReadMonitor] Unexpected exception: {e}");
                throw e;
            }
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
        }

        public void OnTimedOutEvent(Object source)
        {
            isTimedOut = true;
            Stop("manager timed out");
        }
        #endregion

        #region Handler Management
        public bool IsHandlerLimitReached()
        {
            if (settings.handlerMaxCount > 0)
                return streams.Count >= settings.handlerMaxCount;
            return false;
        }

        public StreamHandler CreateHandler()
        {
            return CreateHandler(Guid.NewGuid());
        }

        public StreamHandler CreateHandler(Guid streamId)
        {
            var handler = new StreamHandler(streamId, settings, managerSendBuffer);
            bool added = streams.TryAdd(streamId, handler);
            return added ? handler : null;
        }

        private void RemoveHandler(Guid streamId)
        {
            StreamHandler handler;
            var removed = streams.TryRemove(streamId, out handler);
            if (removed)
            {
                handler?.Dispose();
            }
        }

        public void RemoveHandler(StreamHandler handler)
        {
            RemoveHandler(handler.GetStreamId());
            try
            {
                handler?.Dispose();
            }
            catch (ObjectDisposedException) { }
        }

        private void StopHandler(Guid streamId)
        {
            RemoveHandler(streamId);
        }

        private void StopAllHandlers()
        {
            foreach (var handler in streams.Values)
            {
                RemoveHandler(handler);
            }
        }
        #endregion

        #region Stopped
        public void Stop()
        {
            Stop("");
        }

        public void Stop(string reason)
        {
            if (!shouldStop)
            {
                shouldStop = true;
                if (reason.Length > 0)
                    stopReason = reason;
                if (!alreadyRemoved)
                    alreadyRemoved = true;
                    StopAllHandlers();
                try
                {
                    cancellationSource.Cancel();
                }
                catch (ObjectDisposedException) { }
                try
                {
                    socket.Close();
                }
                catch (ObjectDisposedException) { }
                removable.RemoveManager(managerId);
            }
        }

        public bool IsFullyStopped()
        {
            return fullyStopped;
        }

        public string GetStopReason()
        {
            return stopReason;
        }

        public void Dispose()
        {
            Stop();
            keepAliveTimer.Dispose();
            managerSendBuffer.Dispose();
            cancellationSource.Dispose();
        }
        #endregion

        ~StreamManager()
        {
            Dispose();
        }
    }
}
