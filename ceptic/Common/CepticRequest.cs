using Ceptic.Common.Exceptions;
using Ceptic.Stream;
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

        private StreamHandler stream;

        private string host;
        private int port = Constants.DEFAULT_PORT;

        #region Constructors
        public CepticRequest(string command, string url)
        {
            this.command = command;
            this.url = url;
        }

        public CepticRequest(string command, string url, byte[] body)
            : this(command, url)
        {
            SetBody(body);
        }

        protected CepticRequest(string command, string endpoint, JObject headers)
        {
            this.command = command;
            this.endpoint = endpoint;
            if (headers != null)
                this.headers = headers;
        }

        protected CepticRequest(string command, string endpoint, JObject headers, byte[] body, string url)
        {
            this.command = command;
            this.endpoint = endpoint;
            if (headers != null)
                this.headers = headers;
            SetBody(body);
            this.url = url;
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

        public string GetUrl()
        {
            return url;
        }

        public void SetUrl(string url)
        {
            this.url = url;
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

        public string GetHost()
        {
            return host;
        }

        public int GetPort()
        {
            return port;
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

        public static CepticRequest FromData(String data)
        {
            string[] values = data.Split("\r\n");
            string command = values[0];
            string endpoint = "";
            JObject headers = null;
            if (values.Length > 1)
                endpoint = values[1];
            if (values.Length > 2)
                headers = JsonConvert.DeserializeObject<JObject>(values[2]);
            return new CepticRequest(command, endpoint, headers);
        }

        public static CepticRequest FromData(byte[] data)
        {
            return FromData(Encoding.UTF8.GetString(data));
        }
        #endregion

        public StreamHandler BeginExchange()
        {
            // TODO: fill out
            return null;
        }

    }
}
