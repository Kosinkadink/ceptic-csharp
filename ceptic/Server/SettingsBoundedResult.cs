using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Server
{
    class SettingsBoundedResult
    {
        private readonly string error;
        private readonly int value;

        public SettingsBoundedResult(string error, int value)
        {
            this.error = error;
            this.value = value;
        }

        public bool HasError()
        {
            return error.Length > 0;
        }

        public string GetError()
        {
            return error;
        }

        public int GetValue()
        {
            return value;
        }
    }
}
