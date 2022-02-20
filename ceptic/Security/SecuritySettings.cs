using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Security
{
    public class SecuritySettings
    {
        public string LocalCert { get; } = null;
        public string LocalKey { get; } = null;
        public string RemoteCert { get; } = null;
        public bool VerifyRemote { get; } = true;
        public bool Secure { get; } = true;
        
        public SecuritySettings()
        {

        }

        public SecuritySettings(bool verifyRemote = true, bool secure = true)
        {
            VerifyRemote = verifyRemote;
            Secure = secure;
        }

        public SecuritySettings(string remoteCert, bool verifyRemote = true)
        {
            RemoteCert = remoteCert;
            VerifyRemote = verifyRemote;
        }

        public SecuritySettings(string localCert, string localKey, bool verifyRemote = true)
        {
            LocalCert = localCert;
            LocalKey = localKey;
            VerifyRemote = verifyRemote;
        }

        public SecuritySettings(string localCert, string localKey, string remoteCert, bool verifyRemote = true)
        {
            LocalCert = localCert;
            LocalKey = localKey;
            RemoteCert = remoteCert;
            VerifyRemote = verifyRemote;
        }

    }
}
