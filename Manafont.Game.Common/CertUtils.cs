using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Manafont.Game.Common
{
    public static class CertUtils
    {
        public static bool CheckManafontCert(object sender, X509Certificate? certificate, X509Chain? baseChain,
            SslPolicyErrors policyErrors) {
            if (policyErrors == SslPolicyErrors.RemoteCertificateNotAvailable || certificate == null) {
                throw new AuthenticationException("Remote certificate not supplied");
            }
            
            if (policyErrors != SslPolicyErrors.None &&
                policyErrors != SslPolicyErrors.RemoteCertificateChainErrors) return false;

            X509Chain chain = new X509Chain {
                ChainPolicy = {
                    RevocationMode = X509RevocationMode.NoCheck,
                    VerificationFlags = X509VerificationFlags.AllowUnknownCertificateAuthority,
                    TrustMode = X509ChainTrustMode.CustomRootTrust
                }
            };
            chain.ChainPolicy.CustomTrustStore.Add(new X509Certificate2(Resources.RootCert));
            return chain.Build((X509Certificate2) certificate);
        }
    }
}