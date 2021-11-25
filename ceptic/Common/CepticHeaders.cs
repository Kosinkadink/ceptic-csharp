using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common
{
    abstract class CepticHeaders
    {
        protected JObject headers = new JObject();

        #region Errors
        public JArray GetErrors()
        {
            return headers[HeaderType.Errors]?.ToObject<JArray>();
        }

        public void SetErrors(JArray errors)
        {
            headers[HeaderType.Errors] = errors;
        }
        #endregion

        #region ContentLength
        public long? GetContentLength()
        {
            return headers[HeaderType.ContentLength]?.ToObject<long?>();
        }

        public void SetContentLength(long contentLength)
        {
            headers[HeaderType.ContentLength] = contentLength;
        }

        public bool HasContentLength()
        {
            var value = GetContentLength();
            return value != null && value > 0;
        }
        #endregion

        #region ContentType
        public string GetContentType()
        {
            return headers[HeaderType.ContentType]?.ToObject<string>();
        }

        public void SetContentType(string contentType)
        {
            headers[HeaderType.ContentType] = contentType;
        }

        public bool HasContentType()
        {
            var value = GetContentType();
            return value != null;
        }
        #endregion

        #region Encoding
        public string GetEncoding()
        {
            return headers[HeaderType.Encoding]?.ToObject<string>();
        }

        public void SetEncoding(string encoding)
        {
            headers[HeaderType.Encoding] = encoding;
        }

        public bool HasEncoding()
        {
            var value = GetEncoding();
            return value != null;
        }
        #endregion

        #region Authorization
        public string GetAuthorization()
        {
            return headers[HeaderType.Authorization]?.ToObject<string>();
        }

        public void SetAuthorization(string authorization)
        {
            headers[HeaderType.Authorization] = authorization;
        }

        public bool HasAuthorization()
        {
            var value = GetAuthorization();
            return value != null;
        }
        #endregion

        #region Exchange
        public bool GetExchange()
        {
            var value = headers[HeaderType.Exchange]?.ToObject<bool?>();
            return value != null && value == true;
        }

        public void SetExchange(bool exchange)
        {
            headers[HeaderType.Exchange] = exchange;
        }
        #endregion

        #region Files
        public JArray GetFiles()
        {
            return headers[HeaderType.Files]?.ToObject<JArray>();
        }

        public void SetFiles(JArray files)
        {
            headers[HeaderType.Files] = files;
        }
        #endregion

        public JObject GetHeaders()
        {
            return headers;
        }

    }
}
