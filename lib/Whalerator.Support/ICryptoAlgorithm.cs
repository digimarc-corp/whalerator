namespace Whalerator.Support
{
    public interface ICryptoAlgorithm
    {
        string Decrypt(string cipherText);
        string Encrypt(string clearText);
        string Sign(string text);
        bool VerifySignature(string text, string signature);
        int KeyLength { get; }
        System.Security.Cryptography.RSACryptoServiceProvider ToRSACryptoServiceProvider();

    }
}