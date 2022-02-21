using Ceptic.Security.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Ceptic.Security
{
    public class CertificateHelper
    {
        private static readonly Regex privateKeyRegex = new Regex(@"-----BEGIN ([A-Z ]+)-----([\s\S]*?)-----END [A-Z ]+-----", RegexOptions.Compiled);

        private const string BeginString = "-----BEGIN ";

        private const string RSAPrivateKey = "RSA PRIVATE KEY";
        private const string PrivateKey = "PRIVATE KEY";
        private const string EncryptedPrivateKey = "ENCRYPTED PRIVATE KEY";

        private const string RSAPublicKey = "RSA PUBLIC KEY";
        private const string PublicKey = "PUBLIC KEY";
        private const string CertificateString = "CERTIFICATE";

        public static X509Certificate2 GenerateFromSeparate(string certificate, string key)
        {
            // load public key
            X509Certificate2 cert;
            try
            {
                cert = new X509Certificate2(certificate);
            }
            catch (Exception e)
            {
                throw new SecurityException($"Certificate at '{certificate}' could not be loaded: {e.Message}", e);
            }
            // load private key
            try
            {
                AddPEMPrivateKeyToCertificate(cert, key, out cert);
            }
            catch (SecurityException e)
            {
                throw e;
            }
            if (cert.PrivateKey == null)
                throw new SecurityException($"Private Key could not be loaded from Key file at '{key}'.");
            return cert;
        }

        public static X509Certificate2 GenerateFromCombined(string certificate)
        {
            try
            {
                var cert = new X509Certificate2(certificate);
                if (cert.PrivateKey == null)
                {
                    try
                    {
                        AddPEMPrivateKeyToCertificate(cert, certificate, out cert);
                        if (cert.PrivateKey == null)
                            throw new SecurityException($"Certificate at did not include a private key as expected." +
                                "Either use a file containing both certificate and key, or provide both cert and key files.");
                    }
                    catch (SecurityPEMException e)
                    {
                        throw e;
                    }
                    catch (Exception e)
                    {
                        throw new SecurityException($"Certificate at did not include a private key as expected." +
                            "Either use a file containing both certificate and key, or provide both cert and key files.", e);
                    }
                }
                return cert;
            }
            catch (Exception e)
            {
                throw new SecurityException($"Certificate file at '{certificate}' (assumed to have both cert and key) could not be loaded: {e.Message}", e);
            }
        }

        private static void AddPEMPrivateKeyToCertificate(X509Certificate2 cert, string key, out X509Certificate2 combined)
        {
            string pemRaw = null;
            try
            {
                pemRaw = File.ReadAllText(key);
            }
            catch (Exception e)
            {
                throw new SecurityPEMException($"Key file at '{key}' could not be read: {e}", e);
            }
            pemRaw = pemRaw.Trim();
            if (!pemRaw.StartsWith(BeginString))
                throw new SecurityException($"No Key found in file at '{key}'.");
            pemRaw = pemRaw.Replace("\r\n", "");
            pemRaw = pemRaw.Replace("\n", "");
            var match = privateKeyRegex.Match(pemRaw);
            while (match.Success)
            {
                var keyType = match.Groups[1].Value;
                var pemBase64 = match.Groups[2].Value.Trim();
                var pemBytes = Convert.FromBase64String(pemBase64);
                using var rsa = RSA.Create();
                try
                {
                    switch (keyType)
                    {
                        case RSAPrivateKey:
                            rsa.ImportRSAPrivateKey(pemBytes, out _); break;
                        case PrivateKey:
                            rsa.ImportPkcs8PrivateKey(pemBytes, out _); break;
                        /*case EncryptedPrivateKey:
                            rsa.ImportEncryptedPkcs8PrivateKey(pemBytes, out _); break;*/
                        case RSAPublicKey:
                        case PublicKey:
                        case CertificateString:
                            match = match.NextMatch(); continue;
                        default:
                            throw new SecurityPEMException($"Private Key type '{keyType}' not recognized in file at '{key}'.");
                    }
                }
                catch (CryptographicException e)
                {
                    throw new SecurityPEMException($"Error while attempting to load private key: {e.Message}", e);
                }
                catch (RegexMatchTimeoutException)
                {
                    throw new SecurityPEMException("Regex timeout experienced while searching for private key");
                }
                // assign private key to certificate
                // workaround for issue documented here: https://github.com/dotnet/runtime/issues/23749
                using (var certWithKey = cert.CopyWithPrivateKey(rsa))
                {
                    combined = new X509Certificate2(certWithKey.Export(X509ContentType.Pkcs12));
                }
                return;
            }
            throw new SecurityPEMException($"No Key found in file at '{key}'.");
        }

    }
}
