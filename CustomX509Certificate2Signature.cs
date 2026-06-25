using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using iText.Signatures;

namespace PdfToolbox
{
    public class CustomX509Certificate2Signature : IExternalSignature
    {
        private X509Certificate2 certificate;
        private string hashAlgorithm;
        private string encryptionAlgorithm;

        public CustomX509Certificate2Signature(X509Certificate2 certificate, string hashAlgorithm)
        {
            this.certificate = certificate;
            this.hashAlgorithm = hashAlgorithm;
            
            if (certificate.GetRSAPrivateKey() != null)
                this.encryptionAlgorithm = "RSA";
            else if (certificate.GetECDsaPrivateKey() != null)
                this.encryptionAlgorithm = "ECDSA";
            else
                throw new ArgumentException("Unknown encryption algorithm");
        }

        public string GetDigestAlgorithmName()
        {
            return hashAlgorithm;
        }

        public string GetSignatureAlgorithmName()
        {
            return encryptionAlgorithm;
        }

        public ISignatureMechanismParams GetSignatureMechanismParameters()
        {
            return null;
        }

        public byte[] Sign(byte[] message)
        {
            string dotNetHashName = hashAlgorithm.Replace("-", "");

            if (encryptionAlgorithm == "RSA")
            {
                using (RSA rsa = certificate.GetRSAPrivateKey())
                {
                    return rsa.SignData(message, new HashAlgorithmName(dotNetHashName), RSASignaturePadding.Pkcs1);
                }
            }
            else if (encryptionAlgorithm == "ECDSA")
            {
                using (ECDsa ecdsa = certificate.GetECDsaPrivateKey())
                {
                    return ecdsa.SignData(message, new HashAlgorithmName(dotNetHashName));
                }
            }
            
            return null;
        }
    }
}
