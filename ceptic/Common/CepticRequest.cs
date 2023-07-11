using Ceptic.Common.Exceptions;
using Ceptic.Stream;
using Ceptic.Stream.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common
{
    public class CepticRequest : CepticHeaders
    {
        private readonly string command;
        private string endpoint;
        private byte[] body;
        private string url = "";

        private string querystring;
        private Dictionary<string, string> values;
        private Dictionary<string, string> queryparams;

        private StreamHandler stream;

        private string host;
        private int port = Constants.DEFAULT_PORT;

        #region Constructors
        public CepticRequest(string command, string url, byte[] body=null, JObject headers = null)
        {
            this.command = command;
            this.url = url;
            if (headers != null)
                this.headers = headers;
            SetBody(body);
        }

        protected static CepticRequest CreateWithEndpoint(string command, string endpoint, JObject headers=null, byte[] body=null)
        {
            var request = new CepticRequest(command, null, body, headers)
            {
                endpoint = endpoint
            };
            return request;
        }
        #endregion

        #region Getters and Setters
        public string GetCommand()
        {
            return command;
        }

        public string GetEndpoint()
        {
            return endpoint;
        }

        public byte[] GetBody()
        {
            if (body == null)
                return new byte[0];
            return body;
        }

        public void SetBody(byte[] body)
        {
            if (body != null)
            {
                this.body = body;
                SetContentLength(body.Length);
            }
        }

        public bool HasStream()
        {
            return stream != null;
        }

        public StreamHandler GetStream()
        {
            return stream;
        }

        public void SetStream(StreamHandler stream)
        {
            this.stream = stream;
        }

        public string GetHost()
        {
            return host;
        }

        public int GetPort()
        {
            return port;
        }
        
        public Dictionary<string, string> Values
        {
            get
            {
                return values;
            }
            set
            {
                values = value;
            }
        }
        public Dictionary<string, string> Queryparams
        {
            get
            {
                return queryparams;
            }
            set
            {
                queryparams = value;
            }
        }
        public string Querystring
        {
            get
            {
                return querystring;
            }
            set
            {
                querystring = value;
            }
        }
        #endregion

        #region Verify
        /// <summary>
        /// Verify that CepticRequest is in valid state, and prepare internal fields
        /// </summary>
        /// <exception cref="CepticRequestVerifyException"></exception>
        public void VerifyAndPrepare()
        {
            // check that command isn't null or empty
            if (string.IsNullOrEmpty(command))
                throw new CepticRequestVerifyException("command cannot be null or empty");
            // check that url isn't null or empty
            if (string.IsNullOrEmpty(url))
                throw new CepticRequestVerifyException("url cannot be null or empty");
            // don't redo verification if already satisfied
            if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(endpoint))
                return;
            // extract request components from url
            var components = url.Split("/", 2);
            // set endpoint
            if (components.Length < 2 || string.IsNullOrEmpty(components[1]))
                endpoint = "/";
            else
                endpoint = components[1];
            // extract host and port from first component
            var elements = components[0].Split(":", 2);
            host = elements[0];
            if (elements.Length > 1)
            {
                try
                {
                    port = int.Parse(elements[1]);
                }
                catch (FormatException)
                {
                    throw new CepticRequestVerifyException($"port must be an integer, not {elements[1]}");
                }
                catch (OverflowException)
                {
                    throw new CepticRequestVerifyException($"port caused integer overflow with value {elements[1]}");
                }
                catch (ArgumentNullException)
                {
                    throw new CepticRequestVerifyException($"port was null");
                }
            }
        }
        #endregion

        #region Data
        public byte[] GetData()
        {
            return Encoding.UTF8.GetBytes($"{command}\r\n{endpoint}\r\n{JsonConvert.SerializeObject(headers)}");
        }

        public static CepticRequest FromData(string data)
        {
            string[] values = data.Split("\r\n");
            string command = values[0];
            string endpoint = "";
            JObject headers = null;
            if (values.Length > 1)
                endpoint = values[1];
            if (values.Length > 2)
                headers = JsonConvert.DeserializeObject<JObject>(values[2]);
            return CreateWithEndpoint(command, endpoint, headers);
        }

        public static CepticRequest FromData(byte[] data)
        {
            return FromData(Encoding.UTF8.GetString(data));
        }
        #endregion

        public StreamHandler BeginExchange()
        {
            var response = new CepticResponse(CepticStatusCode.EXCHANGE_START);
            response.SetExchange(true);
            if (stream != null && !stream.IsStopped())
            {
                try
                {
                    if (!GetExchange())
                    {
                        stream.SendResponse(new CepticResponse(CepticStatusCode.MISSING_EXCHANGE));
                        if (stream.GetSettings().verbose)
                            Console.WriteLine("Request did not have required Exchange header");
                        return null;
                    }
                    stream.SendResponse(response);
                }
                catch (StreamException e)
                {
                    if (stream.GetSettings().verbose)
                        Console.WriteLine($"StreamException type {e.GetType()} while trying to BeginExchange: {e.Message}");
                    return null;
                }
                return stream;
            }
            return null;
        }

    }
}
