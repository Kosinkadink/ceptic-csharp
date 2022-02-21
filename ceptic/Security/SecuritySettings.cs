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
        
        protected SecuritySettings()
        {

        }

        protected SecuritySettings(bool verifyRemote = true, bool secure = true)
        {
            VerifyRemote = verifyRemote;
            Secure = secure;
        }

        protected SecuritySettings(string remoteCert, bool verifyRemote = true)
        {
            RemoteCert = remoteCert;
            VerifyRemote = verifyRemote;
        }

        protected SecuritySettings(string localCert, string localKey, bool verifyRemote = true)
        {
            LocalCert = localCert;
            LocalKey = localKey;
            VerifyRemote = verifyRemote;
        }

        protected SecuritySettings(string localCert, string localKey, string remoteCert, bool verifyRemote = true)
        {
            LocalCert = localCert;
            LocalKey = localKey;
            RemoteCert = remoteCert;
            VerifyRemote = verifyRemote;
        }

        public static SecuritySettings Client(bool verifyRemote = true)
        {
            return new SecuritySettings(verifyRemote, true);
        }

        public static SecuritySettings Client(string remoteCert, bool verifyRemote = true)
        {
            return new SecuritySettings(remoteCert, verifyRemote);
        }

        public static SecuritySettings Client(string localCert, string remoteCert, bool verifyRemote = true)
        {
            return new SecuritySettings(localCert, null, remoteCert, verifyRemote);
        }

        public static SecuritySettings Client(string localCert, string localKey, string remoteCert, bool verifyRemote = true)
        {
            return new SecuritySettings(localCert, localKey, remoteCert, verifyRemote);
        }

        public static SecuritySettings ClientUnsecure()
        {
            return new SecuritySettings(secure: false);
        }

        public static SecuritySettings Server(string localCert, string localKey = null, string remoteCert = null)
        {
            return new SecuritySettings(localCert, localKey, remoteCert);
        }

        public static SecuritySettings ServerUnsecure()
        {
            return new SecuritySettings(secure: false);
        }

    }
}
