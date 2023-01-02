using Ceptic.Common;
using Ceptic.Common.Exceptions;
using Ceptic.Net;
using Ceptic.Security;
using Ceptic.Security.Exceptions;
using Ceptic.Stream;
using Ceptic.Stream.Exceptions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Ceptic.Client
{
    public class CepticClient : IRemovableManagers
    {
        private readonly ClientSettings settings;
        private readonly SecuritySettings security;

        private readonly ConcurrentDictionary<Guid, StreamManager> managers = new ConcurrentDictionary<Guid, StreamManager>();
        private readonly ConcurrentDictionary<string, HashSet<Guid>> destinationMap = new ConcurrentDictionary<string, HashSet<Guid>>();

        protected X509Certificate2 localCert = null;
        protected X509Certificate2Collection remoteCerts = null;

        public CepticClient(SecuritySettings security) : this(null, security)
        {

        }

        public CepticClient(ClientSettings settings=null, SecuritySettings security=null)
        {
            this.settings = settings ?? new ClientSettings();
            this.security = security ?? SecuritySettings.Client();
            SetupSecurity();
        }

        #region Security
        protected void SetupSecurity()
        {
            if (security.Secure)
            {
                // if LocalCert present, attempt to load client cert and key
                if (security.LocalCert != null)
                {
                    // if no LocalKey, then assume LocalCert combines both certificate and key
                    if (security.LocalKey == null)
                    {
                        // try to load server certificate + key from combined file
                        try
                        {
                            localCert = CertificateHelper.GenerateFromCombined(security.LocalCert, security.GetKeyPassword());
                        }
                        catch (SecurityException e)
                        {
                            throw e;
                        }
                    }
                    // otherwise, assume LocalCert contains certificate and LocalKey contains key
                    else
                    {
                        // try to load server certificate + key from separate files
                        try
                        {
                            localCert = CertificateHelper.GenerateFromSeparate(security.LocalCert, security.LocalKey, security.GetKeyPassword());
                        }
                        catch (SecurityException e)
                        {
                            throw e;
                        }
                    }
                }

                // if RemoteCert present, attempt to load server cert
                if (security.RemoteCert != null)
                {
                    try
                    {
                        remoteCerts = new X509Certificate2Collection(new X509Certificate2(security.RemoteCert));
                    }
                    catch (CryptographicException e)
                    {
                        throw new SecurityException($"RemoteCert could not be loaded at '{security.RemoteCert}': {e.Message}", e);
                    }
                }
            }
        }

        private static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            // do not allow client to communicate with unauthenticated servers
            return false;
        }

        private static bool ValidateServerCertificateNoHostnameValidation(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            // allow name mismatch and chain errors
            if (sslPolicyErrors == SslPolicyErrors.None ||
                sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch || sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors ||
                sslPolicyErrors == (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors))
                return true;
            // do not allow client to communicate with unauthenticated servers
            return false;
        }
        #endregion

        #region Getters
        public ClientSettings GetSettings()
        {
            return settings;
        }

        public bool IsVerifyRemote()
        {
            return security.VerifyRemote;
        }

        public bool IsSecure()
        {
            return security.Secure;
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

        private CepticResponse Connect(CepticRequest request, SpreadType spread)
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
                // close stream if no Exchange header on response
                if (!response.GetExchange() || !request.GetExchange())
                    stream.SendClose();
                return response;
            }
            catch (CepticException e)
            {
                stream.SendClose();
                throw e;
            }
        }

        public void HandleNewConnection(StreamHandler stream)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Stop
        public void Stop()
        {
            RemoveAllManagers();
        }
        #endregion

        #region Managers
        private StreamManager CreateNewManager(CepticRequest request, string destination)
        {
            try
            {
                // create tcp client and connect
                var tcpClient = new TcpClient(request.GetHost(), request.GetPort());
                tcpClient.NoDelay = true;
                tcpClient.SendTimeout = 5000; // TODO: set all timeouts to match settings
                tcpClient.ReceiveTimeout = 5000;

                // connect the socket to the remove endpoint
                try
                {
                    // set up ssl stream
                    SocketCeptic socket;
                    if (security.Secure)
                    {
                        // only check server hostname if VerifyRemote is true
                        var validation = security.VerifyRemote ? new RemoteCertificateValidationCallback(ValidateServerCertificate) :
                            new RemoteCertificateValidationCallback(ValidateServerCertificateNoHostnameValidation);
                        var sslStream = new SslStream(tcpClient.GetStream(), false, validation, null);
                        sslStream.ReadTimeout = 5000;
                        sslStream.WriteTimeout = 5000;
                        try
                        {
                            // NOTE: due to .NET Core requiring RemoteCertificateValidationCallback to be a static function,
                            // the C# implementation cannot currently support non-system certs or cert chains. Thus,
                            // remoteCerts is completely unused at this time.
                            sslStream.AuthenticateAsClient(request.GetHost(), null,
                                    System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13,
                                    false);
                        }
                        catch (Exception e)
                        {
                            sslStream.Close();
                            tcpClient.Close();
                            throw new CepticIOException($"Could not authenticate as client: {e.Message}", e);
                        }
                        // wrap as SocketCeptic
                        socket = new SocketCeptic(sslStream, tcpClient);
                    }
                    else
                    {
                        // wrap as SocketCeptic
                        socket = new SocketCeptic(tcpClient.GetStream(), tcpClient);
                    }
                    
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
                    var serverStreamTimeoutStr = socket.RecvRawString(4).Trim();
                    var serverHandlerMaxCountStr = socket.RecvRawString(4).Trim();

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
                catch (Exception)
                {
                    throw;
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private void AddManager(StreamManager manager)
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

        private StreamManager GetManager(Guid managerId)
        {
            managers.TryGetValue(managerId, out var manager);
            return manager;
        }

        private StreamManager GetAvailableManagerForDestination(string destination)
        {
            destinationMap.TryGetValue(destination, out var managerSet);
            // if manager set exists, try to get first manager that isn't saturated with handlers
            if (managerSet != null)
            {
                foreach(var managerId in managerSet)
                {
                    var manager = GetManager(managerId);
                    if (manager != null && !manager.IsStopped() && !manager.IsHandlerLimitReached())
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
    }
}
