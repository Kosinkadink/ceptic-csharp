using System;
using System.Collections.Generic;
using System.Text;

namespace Ceptic.Security
{
    public class SecuritySettings
    {
        private string _KeyPassword = null;

        /// <summary>
        /// Local Key password that gets turned to null after a single use
        /// </summary>
        public string KeyPassword {
            set
            {
                SetKeyPassword(value);
            }
        }

        public string LocalCert { get; } = null;
        public string LocalKey { get; } = null;
        public string RemoteCert { get; } = null;
        public bool VerifyRemote { get; } = true;
        public bool Secure { get; } = true;

        #region Key Password
        /// <summary>
        /// Set local key password - will be turned to null upon reading with GetKeyPassword
        /// </summary>
        /// <param name="password"> Local Key password</param>
        public void SetKeyPassword(string password)
        {
            _KeyPassword = password;
        }

        /// <summary>
        /// Return local key's password, turning it to null in the process
        /// </summary>
        /// <returns>Local Key password</returns>
        public string GetKeyPassword()
        {
            string tempPassword = _KeyPassword;
            _KeyPassword = null;
            return tempPassword;
        }
        #endregion

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
