using Ceptic.Common;
using Ceptic.Common.Exceptions;
using Ceptic.Net;
using Ceptic.Stream;
using Ceptic.Stream.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ceptic.Client
{
    class CepticClient : IRemovableManagers
    {
        private readonly ClientSettings settings;
        private readonly string certFile;
        private readonly string keyFile;
        private readonly string caFile;
        private readonly bool checkHostname;
        private readonly bool secure;

        private readonly ConcurrentDictionary<Guid, StreamManager> managers = new ConcurrentDictionary<Guid, StreamManager>();
        private readonly ConcurrentDictionary<string, HashSet<Guid>> destinationMap = new ConcurrentDictionary<string, HashSet<Guid>>();

        public CepticClient(ClientSettings settings=null, string certFile=null, string keyFile = null, string caFile=null, bool checkHostname=true, bool secure=true)
        {
            if (settings == null)
                this.settings = new ClientSettings();
            else
                this.settings = settings;
            this.certFile = certFile;
            this.keyFile = keyFile;
            this.caFile = caFile;
            this.checkHostname = checkHostname;
            this.secure = secure;
        }

        #region Getters
        public ClientSettings GetSettings()
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

        public bool IsCheckHostname()
        {
            return checkHostname;
        }

        public bool IsSecure()
        {
            return secure;
        }
        #endregion

        #region Connection
        public CepticResponse Connect(CepticRequest request)
        {
            return Connect(request, SpreadType.Normal);
        }

        public CepticResponse ConnectStandalone(CepticRequest request)
        {
            return Connect(request, SpreadType.Standalone);
        }

        protected CepticResponse Connect(CepticRequest request, SpreadType spread)
        {
            // verify and prepare request
            request.VerifyAndPrepare();
            // create destination based off of host and port
            var destination = $"{request.GetHost()}:{request.GetPort()}";

            StreamManager manager;
            StreamHandler handler;
            if (spread == SpreadType.Normal)
            {
                manager = GetAvailableManagerForDestination(destination);
                if (manager != null)
                {
                    handler = manager.CreateHandler();
                    // connect to ser ver with thsi handler, returning CepticResponse
                    return ConnectWithHandler(handler, request);
                }
            }
            // if Standalone, make stored destination be random UUID to avoid reuse
            else
                destination += Guid.NewGuid().ToString();
            // create new manager
            manager = CreateNewManager(request, destination);
            handler = manager.CreateHandler();
            // connect to server with this handler, returning CepticResponse
            return ConnectWithHandler(handler, request);
        }

        protected CepticResponse ConnectWithHandler(StreamHandler stream, CepticRequest request)
        {
            try
            {
                // create frames from request and send
                stream.SendRequest(request);
                // wait for response
                var streamData = stream.ReadData(stream.GetSettings().frameMaxSize);
                if (!streamData.IsResponse())
                    throw new StreamException("No CepticResponse found in response");
                var response = streamData.GetResponse();
                // if not success status code, close stream and return response
                if (!response.GetStatusCode().IsSuccess())
                {
                    stream.SendClose();
                    return response;
                }
                // set stream encoding based on request header
                stream.SetEncode(request.GetEncoding());
                // send body if content length header present and greater than 0
                if (request.HasContentLength())
                    stream.SendData(request.GetBody());
                // get response
                streamData = stream.ReadData(stream.GetSettings().frameMaxSize);
                if (!streamData.IsResponse())
                    throw new StreamException("No CepticResponse found in post-body response");
                response = streamData.GetResponse();
                response.SetStream(stream);
                // if content length header is present, receive response body
                if (response.HasContentLength())
                {
                    if (response.GetContentLength() > settings.bodyMax)
                        throw new StreamException($"Response content length ({response.GetContentLength()} is greater than client allows ({settings.bodyMax}");
                    // receive body
                    response.SetBody(stream.ReadDataRaw((long)response.GetContentLength()));
                }
                // TODO: add check for Exchange header on request as well
                // close stream if no Exchange header on response
                if (!response.GetExchange())
                    stream.SendClose();
                return response;
            }
            catch (CepticException e)
            {
                stream.SendClose();
                throw e;
            }
        }
        #endregion

        #region Stop
        public void Stop()
        {
            RemoveAllManagers();
        }
        #endregion

        #region Managers
        protected StreamManager CreateNewManager(CepticRequest request, string destination)
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry(request.GetHost());
                IPAddress ipAddress = host.AddressList[1];
                IPEndPoint endpoint = new IPEndPoint(ipAddress, request.GetPort());

                // create socket
                Socket rawSocket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = 5000,
                    SendTimeout = 5000
                };
                // connect the socket to teh remove endpoint
                try
                {
                    rawSocket.Connect(endpoint);
                    // TODO: set up ssl stream
                    
                    // wrap as SocketCeptic
                    var socket = new SocketCeptic(rawSocket);
                    // send version
                    socket.SendRaw(string.Format("{0,16}", settings.version));
                    // send frameMinSize
                    socket.SendRaw(string.Format("{0,16}", settings.frameMinSize));
                    // send frameMaxSize
                    socket.SendRaw(string.Format("{0,16}", settings.frameMaxSize));
                    // send headersMinSize
                    socket.SendRaw(string.Format("{0,16}", settings.headersMinSize));
                    // send headersMaxSize
                    socket.SendRaw(string.Format("{0,16}", settings.headersMaxSize));
                    // send streamMinTimeout
                    socket.SendRaw(string.Format("{0,4}", settings.streamMinTimeout));
                    // send streamTimeout
                    socket.SendRaw(string.Format("{0,4}", settings.streamTimeout));
                    // get response
                    var response = socket.RecvRaw(1);
                    // if not positive, get additional info and raise exception
                    if (response[0] != 'y')
                    {
                        string errorString = socket.RecvString(1024);
                        throw new CepticIOException($"Client settings not compatible with server settings: {errorString}");
                    }
                    // otherwise received decided values
                    var serverFrameMaxSizeStr = socket.RecvRawString(16).Trim();
                    var serverHeaderMaxSizeStr = socket.RecvRawString(16).Trim();
                    string serverStreamTimeoutStr = socket.RecvRawString(4).Trim();
                    string serverHandlerMaxCountStr = socket.RecvRawString(4).Trim();

                    // attempt to convert to integers
                    int frameMaxSize;
                    int headersMaxSize;
                    int streamTimeout;
                    int handlerMaxCount;
                    try
                    {
                        frameMaxSize = int.Parse(serverFrameMaxSizeStr);
                        headersMaxSize = int.Parse(serverHeaderMaxSizeStr);
                        streamTimeout = int.Parse(serverStreamTimeoutStr);
                        handlerMaxCount = int.Parse(serverHandlerMaxCountStr);
                    }
                    catch (FormatException)
                    {
                        throw new CepticIOException($"Server's values were not all integers, could not proceed:" +
                            $"{serverFrameMaxSizeStr},{serverHeaderMaxSizeStr},{serverStreamTimeoutStr},{serverHandlerMaxCountStr}");
                    }

                    // verify server's chosen values are valid for client
                    // TODO: expand checks to check lower bounds
                    StreamSettings streamSettings = new StreamSettings(settings.sendBufferSize, settings.readBufferSize,
                        frameMaxSize, headersMaxSize, streamTimeout, handlerMaxCount);
                    if (streamSettings.frameMaxSize > settings.frameMaxSize)
                        throw new CepticIOException($"Server chose frameMaxSize ({streamSettings.frameMaxSize}) higher than client's ({settings.frameMaxSize})");
                    if (streamSettings.headersMaxSize > settings.headersMaxSize)
                        throw new CepticIOException($"Server chose headersMaxSize ({streamSettings.headersMaxSize}) higher than client's ({settings.headersMaxSize})");
                    if (streamSettings.streamTimeout > settings.streamTimeout)
                        throw new CepticIOException($"Server chose streamTimeout ({streamSettings.streamTimeout}) higher than client's ({settings.streamTimeout})");
                    // create manager
                    var manager = new StreamManager(socket, Guid.NewGuid(), destination, streamSettings, this, false);
                    // add and start manager
                    AddManager(manager);
                    manager.Start();
                    return manager;
                }
                catch (ArgumentNullException e)
                {
                    throw e;
                }
                catch (SocketException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw e;
                }

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        protected void AddManager(StreamManager manager)
        {
            destinationMap.TryGetValue(manager.GetDestination(), out var managerSet);
            // if manager set already exists for this destination, add manager to that set
            if (managerSet != null)
                managerSet.Add(manager.GetManagerId());
            // otherwise create new set and add to destination map
            else
            {
                managerSet = new HashSet<Guid>();
                managerSet.Add(manager.GetManagerId());
                destinationMap.TryAdd(manager.GetDestination(), managerSet);
            }
            // add manager to map
            managers.TryAdd(manager.GetManagerId(), manager);
        }

        protected StreamManager GetManager(Guid managerId)
        {
            managers.TryGetValue(managerId, out var manager);
            return manager;
        }

        protected StreamManager GetAvailableManagerForDestination(string destination)
        {
            destinationMap.TryGetValue(destination, out var managerSet);
            // if manager set exists, try to get first manager that isn't saturated with handlers
            if (managerSet != null)
            {
                foreach(var managerId in managerSet)
                {
                    var manager = GetManager(managerId);
                    if (manager != null && !manager.IsHandlerLimitReached())
                        return manager;
                }
            }
            // otherwise return null
            return null;
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

        public IStreamManager RemoveManager(Guid managerId)
        {
            // remove manager from managers map
            managers.TryRemove(managerId, out var manager);
            if (manager != null)
            {
                manager.Stop("removed by CepticClient");
                // remove manager id from destinationSet in destination map
                destinationMap.TryGetValue(manager.GetDestination(), out var managerSet);
                if (managerSet != null)
                {
                    managerSet.Remove(manager.GetManagerId());
                }
            }
            return manager;
        }
        #endregion

        public void HandleNewConnection(StreamHandler stream)
        {
            throw new NotImplementedException();
        }
    }
}
