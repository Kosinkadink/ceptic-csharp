using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Common
{
    public class CepticStatusCode
    {
        public static readonly CepticStatusCode OK = new CepticStatusCode(200);
        public static readonly CepticStatusCode CREATED = new CepticStatusCode(201);
        public static readonly CepticStatusCode NO_CONTENT = new CepticStatusCode(204);
        public static readonly CepticStatusCode NOT_MODIFIED = new CepticStatusCode(304);
        public static readonly CepticStatusCode BAD_REQUEST = new CepticStatusCode(400);
        public static readonly CepticStatusCode UNAUTHORIZED = new CepticStatusCode(401);
        public static readonly CepticStatusCode FORBIDDEN = new CepticStatusCode(403);
        public static readonly CepticStatusCode NOT_FOUND = new CepticStatusCode(404);
        public static readonly CepticStatusCode CONFLICT = new CepticStatusCode(409);
        public static readonly CepticStatusCode INTERNAL_SERVER_ERROR = new CepticStatusCode(500);

        private readonly int valueInt;
        private readonly string valueString;

        CepticStatusCode(int valueInt)
        {
            this.valueInt = valueInt;
            valueString = string.Format("D3", valueInt);
        }

        public int GetValueInt()
        {
            return valueInt;
        }

        public string GetValueString()
        {
            return valueString;
        }

        /// <summary>
        /// Creates new CepticStatusCode object with chosen value - must be >= 100 and <= 999, otherwise returns null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static CepticStatusCode FromValue(int value)
        {
            if (value >= 100 && value <= 999)
                return new CepticStatusCode(value);
            return null;
        }

        /// <summary>
        /// Creates new CepticStatusCode object with chosen value - must be >= 100 and <= 999, otherwise returns null
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static CepticStatusCode FromValue(string value)
        {
            try
            {
                return FromValue(int.Parse(value));
            }
            catch (Exception e) when (e is ArgumentNullException || e is FormatException || e is OverflowException)
            {
                return null;
            }

        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }
            return valueInt == ((CepticStatusCode)obj).valueInt;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            return valueInt.GetHashCode();
        }

        public bool IsSuccess()
        {
            return 200 <= valueInt && valueInt <= 399;
        }

        public bool IsError()
        {
            return 400 <= valueInt && valueInt <= 599;
        }

        public bool IsClientError()
        {
            return 400 <= valueInt && valueInt <= 499;
        }

        public bool IsServerError()
        {
            return 500 <= valueInt && valueInt <= 599;
        }

    }
}
