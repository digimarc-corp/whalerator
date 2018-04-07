using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Whalerator.Support
{
    public class RSA : ICryptoAlgorithm
    {
        AsymmetricCipherKeyPair _KeyPair;
        const string SigningAlg = "SHA-256withRSA";
        public int KeyLength { get; }

        public RSA(int length = 2048)
        {
            KeyLength = length;
            var gen = new RsaKeyPairGenerator();
            gen.Init(new KeyGenerationParameters(SecureRandom.GetInstance("SHA256PRNG", true), length));
            _KeyPair = gen.GenerateKeyPair();
        }

        public RSA(string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using (var stream = new MemoryStream(keyBytes))
            using (var reader = new StreamReader(stream))
            {
                var pemReader = new PemReader(reader);
                _KeyPair = (AsymmetricCipherKeyPair)pemReader.ReadObject();
                KeyLength = ((RsaPrivateCrtKeyParameters)_KeyPair.Private).Modulus.BitLength;
            }
        }

        public RSACryptoServiceProvider ToRSACryptoServiceProvider()
        {
            RsaPrivateCrtKeyParameters keyParams = (RsaPrivateCrtKeyParameters)_KeyPair.Private;
            RSAParameters rsaParameters = DotNetUtilities.ToRSAParameters(keyParams);
            CspParameters cspParameters = new CspParameters();
            cspParameters.KeyContainerName = "MyKeyContainer";
            RSACryptoServiceProvider rsaKey = new RSACryptoServiceProvider(KeyLength, cspParameters);
            rsaKey.ImportParameters(rsaParameters);
            return rsaKey;
        }

        public string Sign(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);

            var signer = SignerUtilities.GetSigner(SigningAlg);

            signer.Init(true, _KeyPair.Private);
            signer.BlockUpdate(bytes, 0, bytes.Length);

            return Convert.ToBase64String(signer.GenerateSignature());
        }

        public bool VerifySignature(string text, string signature)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            var sigBytes = Convert.FromBase64String(signature);

            var signer = SignerUtilities.GetSigner(SigningAlg);

            signer.Init(false, _KeyPair.Public);
            signer.BlockUpdate(bytes, 0, bytes.Length);
            return signer.VerifySignature(sigBytes);
        }

        public string Encrypt(string clearText)
        {
            var bytes = Encoding.UTF8.GetBytes(clearText);

            var engine = new Pkcs1Encoding(new RsaEngine());
            engine.Init(true, _KeyPair.Public);

            var encrypted = Convert.ToBase64String(engine.ProcessBlock(bytes, 0, bytes.Length));
            return encrypted;
        }

        public string Decrypt(string cipherText)
        {
            var bytes = Convert.FromBase64String(cipherText);

            var encryptEngine = new Pkcs1Encoding(new RsaEngine());
            encryptEngine.Init(false, _KeyPair.Private);

            var clearText = encryptEngine.ProcessBlock(bytes, 0, bytes.Length);

            return Encoding.UTF8.GetString(clearText);
        }
    }
}
