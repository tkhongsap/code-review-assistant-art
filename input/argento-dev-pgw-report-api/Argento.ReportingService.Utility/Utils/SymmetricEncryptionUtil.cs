using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Argento.ReportingService.Utility.Utils
{
    /// <summary>
    /// เข้ารหัสและถอดรหัสได้
    /// </summary>
    public static class SymmetricEncryptionUtil
    {
        public static string Encrypt(string stringToEncrypt, string key, string salt)
        {
            byte[] encryptingBytes = Encoding.UTF8.GetBytes(stringToEncrypt);
            byte[] encryptedBytes = Encrypt<AesCryptoServiceProvider>(encryptingBytes, key, salt);
            string encryptedString = Convert.ToBase64String(encryptedBytes);
            return encryptedString;
        }

        public static string Decrypt(string stringToDecrypt, string key, string salt)
        {
            byte[] decryptingBytes = Convert.FromBase64String(stringToDecrypt);
            byte[] decryptedBytes = Decrypt<AesCryptoServiceProvider>(decryptingBytes, key, salt);
            string decryptedString = Encoding.UTF8.GetString(decryptedBytes);
            return decryptedString;
        }

        private static byte[] Encrypt<T>(byte[] encryptingBytes, string key, string salt)
            where T : SymmetricAlgorithm, new()
        {
            byte[] encryptedBytes = null;
            using (SymmetricAlgorithm algorithm = new T())
            {
                byte[] saltBytes = Encoding.UTF8.GetBytes(salt);

                using (DeriveBytes keyDeriveBytes = new Rfc2898DeriveBytes(key, saltBytes))
                {
                    algorithm.BlockSize = algorithm.LegalBlockSizes[0].MaxSize;
                    algorithm.KeySize = algorithm.LegalKeySizes[0].MaxSize;
                    algorithm.Key = keyDeriveBytes.GetBytes(algorithm.KeySize >> 3);
                    keyDeriveBytes.Reset();
                    algorithm.IV = keyDeriveBytes.GetBytes(algorithm.BlockSize >> 3);
                }

                using (ICryptoTransform transform = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV))
                using (var buffer = new MemoryStream())
                {
                    using (var stream = new CryptoStream(buffer, transform, CryptoStreamMode.Write))
                    {
                        stream.Write(encryptingBytes, 0, encryptingBytes.Length);
                    }

                    encryptedBytes = buffer.ToArray();
                }

            }

            return encryptedBytes;

        }

        private static byte[] Decrypt<T>(byte[] decryptingBytes, string key, string salt)
            where T : SymmetricAlgorithm, new()
        {

            byte[] decryptedBytes = null;
            using (SymmetricAlgorithm algorithm = new T())
            {

                byte[] saltBytes = Encoding.UTF8.GetBytes(salt);

                DeriveBytes keyDeriveBytes = new Rfc2898DeriveBytes(key, saltBytes);

                algorithm.BlockSize = algorithm.LegalBlockSizes[0].MaxSize;
                algorithm.KeySize = algorithm.LegalKeySizes[0].MaxSize;
                algorithm.Key = keyDeriveBytes.GetBytes(algorithm.KeySize >> 3);
                algorithm.Mode = CipherMode.CBC;
                algorithm.Padding = PaddingMode.PKCS7;
                keyDeriveBytes.Reset();
                algorithm.IV = keyDeriveBytes.GetBytes(algorithm.BlockSize >> 3);

                ICryptoTransform transform = algorithm.CreateDecryptor();
                decryptedBytes = transform.TransformFinalBlock(decryptingBytes, 0, decryptingBytes.Length);
            }

            return decryptedBytes;
        }
    }
}
