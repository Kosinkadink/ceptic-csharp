using Ceptic.Stream;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common
{
    class CepticResponse : CepticHeaders
    {
        private readonly CepticStatusCode statusCode;
        private byte[] body;

        private StreamHandler stream;

        #region Constructors
        public CepticResponse(CepticStatusCode statusCode, byte[] body, JObject headers, JArray errors, StreamHandler stream)
        {
            this.statusCode = statusCode;
            SetBody(body);
            if (headers != null)
                this.headers = headers;
            if (errors != null)
                SetErrors(errors);
            this.stream = stream;
        }

        public CepticResponse(CepticStatusCode statusCode, JObject headers) : this(statusCode, null, headers, null, null)
        {
            
        }

        public CepticResponse(CepticStatusCode statusCode, JArray errors) : this(statusCode, null, null, errors, null)
        {

        }
        #endregion

        #region Getters and Setters
        public StreamHandler GetStream()
        {
            return stream;
        }

        public void SetStream(StreamHandler stream)
        {
            this.stream = stream;
        }

        public CepticStatusCode GetStatusCode()
        {
            return statusCode;
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
        #endregion

        #region Data
        public byte[] GetData()
        {
            return Encoding.UTF8.GetBytes($"{statusCode.GetValueString()}\r\n{JsonConvert.SerializeObject(headers)}");
        }

        public static CepticResponse FromData(string data)
        {
            string[] values = data.Split("\r\n");
            CepticStatusCode statusCode = CepticStatusCode.FromValue(values[0]);
            if (values.Length >= 2)
                return new CepticResponse(statusCode, JsonConvert.DeserializeObject<JObject>(values[1]));
            else
                return new CepticResponse(statusCode, (JObject)null);
        }

        public static CepticResponse FromData(byte[] data)
        {
            return FromData(Encoding.UTF8.GetString(data));
        }
        #endregion

    }
}
