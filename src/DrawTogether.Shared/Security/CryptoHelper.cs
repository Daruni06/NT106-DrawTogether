using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DrawTogether.Shared.Security
{
    public static class CryptoHelper
    {
        // Demo Session Key (AES-256)
        private static readonly byte[] Key =
            Encoding.UTF8.GetBytes("12345678901234567890123456789012");

        // AES IV (16 bytes)
        private static readonly byte[] IV =
            Encoding.UTF8.GetBytes("1234567890123456");

        // AES Payload Encryption
        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform encryptor =
                    aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs =
                        new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        // AES Payload Decryption
        public static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;

                ICryptoTransform decryptor =
                    aes.CreateDecryptor(aes.Key, aes.IV);

                byte[] cipherBytes =
                    Convert.FromBase64String(cipherText);

                using (MemoryStream ms =
                    new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cs =
                        new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr =
                            new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        // Secure Send Wrapper
        public static string SecureSend(string payload)
        {
            return Encrypt(payload);
        }

        // Secure Receive Wrapper
        public static string SecureReceive(string encryptedPayload)
        {
            return Decrypt(encryptedPayload);
        }

        // Session Key Generator
        public static byte[] GenerateSessionKey()
        {
            byte[] key = new byte[32];

            using (RandomNumberGenerator rng =
                RandomNumberGenerator.Create())
            {
                rng.GetBytes(key);
            }

            return key;
        }

        // Payload Validation
        public static bool ValidatePayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
                return false;

            if (payload.Length > 10000)
                return false;

            return true;
        }
    }
}