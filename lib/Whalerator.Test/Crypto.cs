using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Whalerator.Support;

namespace Whalerator.Test
{
    [TestClass]
    public class Crypto
    {
        [TestMethod]
        public void CanSignText()
        {
            var crypto = new RSA(TestKey);

            var signature = crypto.Sign(Sample);

            Assert.IsTrue(crypto.VerifySignature(Sample, signature));
        }

        [TestMethod]
        public void CanGetProviderWithPemKey()
        {
            var crypto = new RSA(TestKey);
            var provider = crypto.ToDotNetRSA();

            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void CanGetProviderWithSelfGeneratedKey()
        {
            var crypto = new RSA();
            var provider = crypto.ToDotNetRSA();

            Assert.IsNotNull(provider);
        }

        [TestMethod]
        public void CanEncryptTextWithPemKey()
        {
            var crypto = new RSA(TestKey);

            var cipherText = crypto.Encrypt(Sample);

            Assert.AreNotEqual(Sample, cipherText);
            Assert.AreEqual(Sample, crypto.Decrypt(cipherText));
        }

        [TestMethod]
        public void CanEncryptTextWithSelfGeneratedKey()
        {
            var crypto = new RSA();

            var cipherText = crypto.Encrypt(Sample);

            Assert.AreNotEqual(Sample, cipherText);
            Assert.AreEqual(Sample, crypto.Decrypt(cipherText));
        }

        const string Sample = "The quick brown fox jumped over the lazy dog";

        const string TestKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAlv26PZsTQiyyB5geiOYOGzNB1iWH6zS7da029iMGZcNFhNu+
zb6F5LkG2Tqav1fKmgTmh8AcCPNm4G7oSu0KmdpJ3Qb+8SSbIa5zgUpG562DVYyU
4uSlkD95h2Zih8IWqFDqH04xKvhHYOknS5COLRdxOcIq4xuW5ffI1f//T3GSeyOL
SxytgFWtn1bamOoyf6ae0x2zZTHhNbJByyaQuU+zc13s+5uU4o8BgCm7pdgjNYP2
3HR7xNMyaLqlNChMvuZLW8mEUoRvVlAQ29fytMhSL0gTXSQqQUJ2mI8kXl/huq0q
+NLxml6Y9ATKnnmlaJXD/IZTiPpMip/x7XufkwIDAQABAoIBACVZP79LY7kjuynb
u/nbBjQ+OpPRFszcb15NaWH7CfMUGVci0BCuhworpoEHWQ3plmkAu1Fq+MRSCOmN
JZKbDds+vrSYrWlSx7jOIS4jBGT8a5c2vgFd04JZ4SLX9ZllLhuWYEM8ITdi0K0t
HxY2/KQ0v0ItQFSLF4ltAFXHDA1HRlO07LSACIQ6xnyd8G653YOg5MMuvM8xEzfc
3MpjKZCHvD5Ovv5J/+MhQvVHsCm6yCZ+Lngt6tEA97Pvg9yBM+Jss8noyLNw+kTq
tCXXzUCQaVKt8pXbV4AWxv3I5Dfcfbc4Dkg4VsUkJOuYVZZ1hsNYRVeNst20u3ww
XmQkfHkCgYEAxO1BzPLq8mpBo6XrnD6pD6N6Z2T1fB2T4L//NanuQoi8MWhQmGOs
vW3DVxj7Qd6qRHu8DsKDR9Fz90B1OsoHHWdzL6a99K+/nu2A7irM23jQDCPG+4pk
pgu5DB7oVUmCP/1srfMKAry70rYCS3zqmlehvUpLACH6A42tarEVx78CgYEAxEjm
RO5Mh3V4u0sqgy2jV1wcaymxJXO1pr9yEd+/g97pj49rLRDx+z0AQDHr7BqLcCG7
kuo6rpcy8JsjCk5BlVy2/ltCkeP2U2dTO4Hv9YNalDp6/kZ4nySek1nlcF3UBXPz
QkHUwuDPs3A5Nk2FbCv04kR2cYbP4WEyGL+xPS0CgYEArhfZ/igbEShi0xwSCVVT
/KFXyyRz6b/0RdM2+eg63NMpHLzN04r64ZSyBsPtDLSe7mA9RwcrLEi9Lq7rdIe6
doJIUK4mbLUawJPTUbrA1J4fvzu55BLbG/htJYwFdbDA13VhqP6dsQHNQmDp8naC
qOQj9sZBO+LNtmqADzxytt0CgYAca3Wzy1EXV+HoNGTuY2BAGN0ggwPcKVnGz/dj
MSNYo6DroVdiSg7PUrDDmrbPE7TWwnuXNQTUHJ5KihvTtSr3xlnUkEAeQYR75Pz/
I7wrrx4hUipWwLtcR4ASU7TNxTgapgQ1trRolwZbs8cE3sqPs/mb/U+s9lkJB/qp
7K9r7QKBgFsgBs8/OEwKRYDDJmAAePJEqFS2MkAtnYdiYJ9HXjBmt1p/ijPXJya5
4uJo53haq8aAyKB9t67h67YVSRGliUGxAHBsClfmGPU69x+uwZGKJ7fFtyDsftJL
wrDWaEXqjSgdgeWwrHGCL0DUpC5i6XXzDIJCUDliuVdJr7NjgPns
-----END RSA PRIVATE KEY-----";

        const string TestCert = @"-----BEGIN CERTIFICATE-----
MIIDtTCCAp2gAwIBAgIJAJRsDd4L83Z1MA0GCSqGSIb3DQEBBQUAMEUxCzAJBgNV
BAYTAkFVMRMwEQYDVQQIEwpTb21lLVN0YXRlMSEwHwYDVQQKExhJbnRlcm5ldCBX
aWRnaXRzIFB0eSBMdGQwHhcNMTgwNDAyMjAwMjMwWhcNMTkwNDAyMjAwMjMwWjBF
MQswCQYDVQQGEwJBVTETMBEGA1UECBMKU29tZS1TdGF0ZTEhMB8GA1UEChMYSW50
ZXJuZXQgV2lkZ2l0cyBQdHkgTHRkMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIB
CgKCAQEAlv26PZsTQiyyB5geiOYOGzNB1iWH6zS7da029iMGZcNFhNu+zb6F5LkG
2Tqav1fKmgTmh8AcCPNm4G7oSu0KmdpJ3Qb+8SSbIa5zgUpG562DVYyU4uSlkD95
h2Zih8IWqFDqH04xKvhHYOknS5COLRdxOcIq4xuW5ffI1f//T3GSeyOLSxytgFWt
n1bamOoyf6ae0x2zZTHhNbJByyaQuU+zc13s+5uU4o8BgCm7pdgjNYP23HR7xNMy
aLqlNChMvuZLW8mEUoRvVlAQ29fytMhSL0gTXSQqQUJ2mI8kXl/huq0q+NLxml6Y
9ATKnnmlaJXD/IZTiPpMip/x7XufkwIDAQABo4GnMIGkMB0GA1UdDgQWBBTePlrS
fTxs3SihvOEu6YNBsqsi2jB1BgNVHSMEbjBsgBTePlrSfTxs3SihvOEu6YNBsqsi
2qFJpEcwRTELMAkGA1UEBhMCQVUxEzARBgNVBAgTClNvbWUtU3RhdGUxITAfBgNV
BAoTGEludGVybmV0IFdpZGdpdHMgUHR5IEx0ZIIJAJRsDd4L83Z1MAwGA1UdEwQF
MAMBAf8wDQYJKoZIhvcNAQEFBQADggEBAIBv8Qy1LwfW1dIS962TC51HJRcaO1/L
SdP4CyqqHRoo8whhfvhng31D5pv60tYi/pIO33WCwgYfcwxiPKFGpRG5bnGzA7t+
2F7HBp+oefZCy4zlc3LuqwNRv+w9AqhzXAPfCTFSxMOBLF5DpxwqCcvr707Q5GjN
AQLYKvkwvhFokzqBfKgYp2RF/r3HCtr1tPjtGev4tqcDgJiw66oTwz/Dvx8Et3oW
MejBBd22x/K2Q9h88iM/q3J4B+wFQbuCzB0wvs3enqsBOZOnCh5/ZiMNxnwToOR9
sDN+XZ9bxZPN3WPcVMss1Qt0BXhgKNhJLF6kXv8UHkD2KUbEuiGRMR0=
-----END CERTIFICATE-----";

    }
}
