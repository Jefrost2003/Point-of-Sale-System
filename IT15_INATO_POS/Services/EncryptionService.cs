using System.Security.Cryptography;
using System.Text;

namespace IT15_INATO_POS.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(ILogger<EncryptionService> logger)
        {
            var encryptionKey = Environment.GetEnvironmentVariable("AES_ENCRYPTION_KEY");

            if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length != 32)
            {
                logger.LogError("AES_ENCRYPTION_KEY must be exactly 32 characters!");
                encryptionKey = "K9vX2QpL8zN4mRwE5uT1yCjH6aB3Ds7Fa";
            }

            _key = Encoding.UTF8.GetBytes(encryptionKey);
            _iv = new byte[16];
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);
            sw.Write(plainText);
            sw.Close();
            cs.Close();
            return Convert.ToBase64String(ms.ToArray());
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return cipherText;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);
            return sr.ReadToEnd();
        }
    }
}