﻿using Ceptic.Common;
using Ceptic.Encode;
using Ceptic.Encode.Exceptions;
using Ceptic.Endpoint;
using Ceptic.Endpoint.Exceptions;
using Ceptic.Net;
using Ceptic.Net.Exceptions;
using Ceptic.Stream;
using Ceptic.Stream.Exceptions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ceptic.Server
{
    public class CepticServer : IRemovableManagers
    {
        private readonly ServerSettings settings;
        private readonly string certFile;
        private readonly string keyFile;
        private readonly string caFile;
        private readonly bool secure;

        private readonly CancellationTokenSource cancellationSource;
        private readonly CancellationToken cancellationToken;

        private Thread runThread;

        private Socket serverSocket;

        private bool shouldStop = false;
        private bool stopped = false;

        protected readonly EndpointManager endpointManager;
        protected readonly ConcurrentDictionary<Guid, IStreamManager> managers = new ConcurrentDictionary<Guid, IStreamManager>();

        public CepticServer(ServerSettings settings = null, string certFile = null, string keyFile = null, string caFile = null, bool secure = true)
        {
            this.settings = settings ?? new ServerSettings();
            this.certFile = certFile;
            this.keyFile = keyFile;
            this.caFile = caFile;
            this.secure = secure;

            runThread = new Thread(new ThreadStart(Run));
            runThread.IsBackground = settings.daemon;

            cancellationSource = new CancellationTokenSource();
            cancellationToken = cancellationSource.Token;

            endpointManager = new EndpointManager(this.settings);
        }

        #region Getters
        public ServerSettings GetSettings()
        {
            return settings;
        }

        public string GetCertFile()
        {
            return certFile;
        }

        public string GetKeyFile()
        {
            return keyFile;
        }

        public string GetCaFile()
        {
            return caFile;
        }

        public bool IsSecure()
        {
            return secure;
        }
        #endregion

        #region Add Commands and Route
        public void AddCommand(string command, CommandSettings settings=null)
        {
            endpointManager.AddCommand(command, settings);
        }

        public void AddRoute(string command, string endpoint, EndpointEntry entry, CommandSettings settings=null)
        {
            endpointManager.AddEndpoint(command, endpoint, entry, settings);
        }
        #endregion

        #region Start
        public void Start()
        {
            runThread.Start();
        }

        private void Run()
        {
            if (settings.verbose)
                Console.WriteLine($"ceptic server started - version {settings.version} on port {settings.port} (secure: {secure})");
            // create server socket
            var host = Dns.GetHostEntry("localhost");
            var ipAddress = host.AddressList[1];
            var endpoint = new IPEndPoint(ipAddress, settings.port);

            try
            {
                serverSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = 5000,
                    SendTimeout = 5000
                };
                serverSocket.Bind(endpoint);
                serverSocket.Listen(settings.requestQueueSize);
            }
            catch (SocketException e)
            {
                if (settings.verbose)
                    Console.WriteLine($"Issue creating ServerSocket: {e}");
                Stop();
                return;
            }

            while (!shouldStop)
            {
                Socket socket;
                try
                {
                    socket = serverSocket.Accept();
                }
                catch (SocketException)
                {
                    continue;
                }
                catch (Exception e)
                {
                    if (settings.verbose)
                        Console.WriteLine($"Issue accepting Socket: {e}");
                    Stop();
                    continue;
                }
                // set socket timeout
                try
                {
                    socket.ReceiveTimeout = 5000;
                    socket.SendTimeout = 5000;
                }
                catch (SocketException e)
                {
                    if (settings.verbose)
                        Console.WriteLine($"Issue setting timeout of accepted socket: {e}");
                    Stop();
                    continue;
                }
                // handle accepted socket
                new Task(() => {
                    try
                    {
                        CreateNewManager(socket);
                    }
                    catch (SocketCepticException e)
                    {
                        if (settings.verbose)
                            Console.WriteLine($"Issue with CreateNewManager: {e}");
                    }
                },
                cancellationToken, TaskCreationOptions.LongRunning).Start();
            }
            // server is closing
            // close socket
            try
            {
                serverSocket.Close();
            }
            catch (Exception e)
            {
                if (settings.verbose)
                    Console.WriteLine($"Issue closing ServerSocket: {e}");
            }
            // shut down all managers
            RemoveAllManagers();
            // done running
            stopped = true;
        }
        #endregion

        #region Stop
        public void Stop()
        {
            shouldStop = true;
            try
            {
                cancellationSource.Cancel();
            }
            catch (ObjectDisposedException) { }
            try
            {
                serverSocket?.Close();
            }
            catch (Exception) { }
        }

        public bool IsStopped()
        {
            return stopped;
        }

        public bool Join(int? timeout=null)
        {
            if (runThread == null)
                return true;
            if (timeout != null)
                return runThread.Join((int)timeout);
            runThread.Join();
            return true;
        }

        public bool Join(TimeSpan timeSpan)
        {
            if (runThread == null)
                return true;
            if (timeSpan != null)
                return runThread.Join(timeSpan);
            runThread.Join();
            return true;
        }
        #endregion

        #region Connection
        public void HandleNewConnection(StreamHandler stream)
        {
            // store errors in request
            var errors = new JArray();
            // get request from request data
            var request = CepticRequest.FromData(stream.ReadHeaderDataRaw());
            // begin checking validity of request
            // check that command and endpoint are of valid length
            if (request.GetCommand().Length > Constants.COMMAND_LENGTH)
                errors.Add($"command too long; should be no more than {Constants.COMMAND_LENGTH} but was {request.GetCommand().Length}");
            if (request.GetEndpoint().Length > Constants.ENDPOINT_LENGTH)
                errors.Add($"command too long; should be no more than {Constants.ENDPOINT_LENGTH} but was {request.GetEndpoint().Length}");
            // if no errors yet, get endpoint from endpoint manager
            EndpointValue endpointValue = null;
            if (errors.Count == 0)
            {
                try
                {
                    // get endpoint value from endpoint manager
                    endpointValue = endpointManager.GetEndpoint(request.GetCommand(), request.GetEndpoint());
                    // check that headers are valid
                    errors.Merge(CheckNewConnectionHeaders(request));
                }
                catch (EndpointManagerException e)
                {
                    errors.Add(e.ToString());
                }
            }
            // if errors or no endpointValue found, send CepticResponse with BadRequest
            if (errors.Count > 1 || endpointValue == null)
            {
                stream.SendResponse(new CepticResponse(CepticStatusCode.BAD_REQUEST, errors: errors));
                stream.SendClose();
                return;
            }
            // send positive response and continue with endpoint function
            stream.SendResponse(new CepticResponse(CepticStatusCode.OK));
            // set stream encoding, based on request header
            try
            {
                stream.SetEncode(request.GetEncoding());
            }
            catch (UnknownEncodingException e)
            {
                stream.SendClose(e.ToString());
                return;
            }
            // get body if content length header is present
            if (request.HasContentLength())
            {
                try
                {
                    request.SetBody(stream.ReadDataRaw((long)request.GetContentLength()));
                }
                catch (StreamTotalDataSizeException)
                {
                    stream.SendClose("body received is greater than reported Content-Length");
                    return;
                }
            }
            // set request stream
            request.SetStream(stream);
            // perform endpoint function and get back response
            var response = endpointValue.ExecuteEndpointEntry(request);
            // send response
            stream.SendResponse(response);
            // send body if content length header present
            if (response.HasContentLength())
            {
                try
                {
                    stream.SendData(response.GetBody());
                }
                catch (StreamException e)
                {
                    stream.SendClose("Server stream exception occurred");
                    if (settings.verbose)
                        Console.WriteLine($"StreamException type {e.GetType().Name} raised while sending response body: {e}");
                    return;
                }
            }
            // close connection
            stream.SendClose("Server command complete");
        }
        #endregion

        #region Managers
        private void CreateNewManager(Socket rawSocket)
        {
            if (settings.verbose)
                Console.WriteLine($"Got a connection from {rawSocket.RemoteEndPoint}");
            // TODO: wrap with SSL
            // wrap as SocketCeptic
            var socket = new SocketCeptic(rawSocket);

            // get client version
            var clientVersion = socket.RecvRawString(16).Trim();
            // get client frameMinSize
            var clientFrameMinSizeString = socket.RecvRawString(16).Trim();
            // get client frameMaxSize
            var clientFrameMaxSizeString = socket.RecvRawString(16).Trim();
            // get client headersMinSize
            var clientHeadersMinSizeString = socket.RecvRawString(16).Trim();
            // get client headersMaxSize
            var clientHeadersMaxSizeString = socket.RecvRawString(16).Trim();
            // get client streamMinTimeout
            var clientStreamMinTimeoutString = socket.RecvRawString(4).Trim();
            // get client streamTimeout
            var clientStreamTimeoutString = socket.RecvRawString(4).Trim();

            // see if values are acceptable
            var errors = new StringBuilder();
            // TODO: add version checking
            // convert to int
            StreamSettings streamSettings = null;
            int clientFrameMinSize;
            int clientFrameMaxSize;
            int clientHeadersMinSize;
            int clientHeadersMaxSize;
            int clientStreamMinTimeout;
            int clientStreamTimeout;
            try
            {
                clientFrameMinSize = int.Parse(clientFrameMinSizeString);
                clientFrameMaxSize = int.Parse(clientFrameMaxSizeString);
                clientHeadersMinSize = int.Parse(clientHeadersMinSizeString);
                clientHeadersMaxSize = int.Parse(clientHeadersMaxSizeString);
                clientStreamMinTimeout = int.Parse(clientStreamMinTimeoutString);
                clientStreamTimeout = int.Parse(clientStreamTimeoutString);
                // check value bounds
                var frameMaxSize = CheckIfSettingBounded(clientFrameMinSize, clientFrameMaxSize,
                    settings.frameMinSize, settings.frameMaxSize, "frame size");
                var headersMaxSize = CheckIfSettingBounded(clientHeadersMinSize, clientHeadersMaxSize,
                    settings.headersMinSize, settings.headersMaxSize, "header size");
                var streamTimeout = CheckIfSettingBounded(clientStreamMinTimeout, clientStreamTimeout,
                    settings.streamMinTimeout, settings.streamTimeout, "stream timeout");
                // add errors, if applicable
                if (frameMaxSize.HasError())
                    errors.Append(frameMaxSize.GetError());
                if (headersMaxSize.HasError())
                    errors.Append(headersMaxSize.GetError());
                if (streamTimeout.HasError())
                    errors.Append(streamTimeout.GetError());
                // create stream settings
                streamSettings = new StreamSettings(settings.sendBufferSize, settings.readBufferSize,
                    frameMaxSize.GetValue(), headersMaxSize.GetValue(), streamTimeout.GetValue(),
                    settings.handlerMaxCount);
                streamSettings.verbose = settings.verbose;
            }
            catch (FormatException)
            {
                errors.Append($"Client's thresholds were not all integers:" +
                    $"{clientFrameMinSizeString},{clientFrameMaxSizeString}," +
                    $"{clientHeadersMinSizeString},{clientHeadersMaxSizeString}," +
                    $"{clientStreamMinTimeoutString},{clientStreamTimeoutString}");
            }
            // send response
            // if errors present, send negative response with explanation
            if (errors.Length > 0 || streamSettings == null)
            {
                socket.SendRaw("n");
                var errorString = errors.Length > 1024 ? errors.ToString().Substring(0, 1024) : errors.ToString();
                socket.Send(errorString);
                if (settings.verbose)
                    Console.WriteLine("Client not compaible with server settings, connection terminated.");
                socket.Close();
                return;
            }
            // otherwise send positive response along with decided values
            socket.SendRaw("y");
            socket.SendRaw(string.Format("{0,16}", streamSettings.frameMaxSize));
            socket.SendRaw(string.Format("{0,16}", streamSettings.headersMaxSize));
            socket.SendRaw(string.Format("{0,4}", streamSettings.streamTimeout));
            socket.SendRaw(string.Format("{0,4}", streamSettings.handlerMaxCount));
            // create manager
            var manager = new StreamManager(socket, Guid.NewGuid(), "manager", streamSettings,
                this, true);
            // add and start manager
            AddManager(manager);
            manager.Start();
        }

        protected void AddManager(IStreamManager manager)
        {
            // add maanger to map
            managers.TryAdd(manager.GetManagerId(), manager);
        }

        public IStreamManager RemoveManager(Guid managerId)
        {
            // remove manager from managers map
            managers.TryRemove(managerId, out var manager);
            if (manager != null)
            {
                manager.Stop("removed by CepticServer");
            }
            return manager;
        }

        protected List<IStreamManager> RemoveAllManagers()
        {
            var removedManagers = new List<IStreamManager>();
            foreach (var manager in managers.Values)
            {
                removedManagers.Add(RemoveManager(manager.GetManagerId()));
            }
            return removedManagers;
        }
        #endregion

        #region Helper Methods
        private SettingsBoundedResult CheckIfSettingBounded(int clientMin, int clientMax, int serverMin, int serverMax, string settingName)
        {
            var error = "";
            var value = -1;
            if (clientMax <= serverMax)
            {
                if (clientMax < serverMin)
                    error = $"client max {settingName} ({clientMax}) is greater than server's max ({serverMax})";
                else
                    value = clientMax;
            }
            // since client is greater than server max, check if server max is appropriate
            if (clientMin > serverMax)
                // client min greater than server max, so not compatible
                error = $"client min {settingName} ({clientMin}) is greater than server's max ({serverMax})";
            // otherwise use server max
            else
                value = serverMax;
            return new SettingsBoundedResult(error, value);
        }

        private JArray CheckNewConnectionHeaders(CepticRequest request)
        {
            var errors = new JArray();
            // check that content length is of allowed length
            // if content length is longer than set max body length, invalid
            if (request.HasContentLength() && request.GetContentLength() > settings.bodyMax)
            {
                errors.Add($"Content-Length ({request.GetContentLength()}) exceeds server's allowed max body length of {settings.bodyMax}");
            }
            // check that encoding is recognized and valid
            if (request.HasEncoding())
            {
                try
                {
                    EncodeGetter.Get(request.GetEncoding());
                }
                catch (UnknownEncodingException e)
                {
                    errors.Add(e.ToString());
                }
            }
            return errors;
        }
        #endregion
    }
}
