using Ceptic.Stream;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common
{
    public class CepticResponse : CepticHeaders
    {
        private readonly CepticStatusCode statusCode;
        private byte[] body;

        private StreamHandler stream;

        #region Constructors
        public CepticResponse(CepticStatusCode statusCode, byte[] body=null, JObject headers=null, JArray errors=null, StreamHandler stream=null)
        {
            this.statusCode = statusCode;
            if (headers != null)
                this.headers = headers;
            if (errors != null)
                SetErrors(errors);
            SetBody(body);
            this.stream = stream;
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
            return Encoding.UTF8.GetBytes($"{string.Format("{0,3}", statusCode.GetValue())}\r\n{JsonConvert.SerializeObject(headers)}");
        }

        public static CepticResponse FromData(string data)
        {
            string[] values = data.Split("\r\n");
            CepticStatusCode statusCode = CepticStatusCode.FromValue(values[0]);
            if (values.Length >= 2)
                return new CepticResponse(statusCode, headers: JsonConvert.DeserializeObject<JObject>(values[1]));
            else
                return new CepticResponse(statusCode);
        }

        public static CepticResponse FromData(byte[] data)
        {
            return FromData(Encoding.UTF8.GetString(data));
        }
        #endregion

    }
}
