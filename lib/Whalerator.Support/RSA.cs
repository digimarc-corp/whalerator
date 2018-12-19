/*
   Copyright 2018 Digimarc, Inc

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.

   SPDX-License-Identifier: Apache-2.0
*/

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
//using System.Security.Cryptography;
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

        public System.Security.Cryptography.RSA ToDotNetRSA()
        {
            RsaPrivateCrtKeyParameters keyParams = (RsaPrivateCrtKeyParameters)_KeyPair.Private;
            System.Security.Cryptography.RSAParameters rsaParameters = DotNetUtilities.ToRSAParameters(keyParams);
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportParameters(rsaParameters);
            return rsa;
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
