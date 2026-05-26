// Helper ma hoa/giai ma payload neu nhom lam phan Cryptography.
// Co the dung AES cho noi dung message truoc khi gui qua socket.
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace DrawTogether.Shared.Security
{
    public static class CryptoHelper
    {
        public static readonly byte[] Key =
            Encoding.UTF8.GetBytes(
                "12345678901234567890123456789012");

        public static readonly byte[] IV =
            Encoding.UTF8.GetBytes(
                "1234567890123456");

        public static string Encrypt(string plainText)
        {
            using Aes aes = Aes.Create();

            aes.Key = Key;

            aes.IV = IV;

            ICryptoTransform encryptor =
                aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream ms =
                new MemoryStream();

            using CryptoStream cs =
                new CryptoStream(
                    ms,
                    encryptor,
                    CryptoStreamMode.Write);

            using StreamWriter sw =
                new StreamWriter(cs);

            sw.Write(plainText);

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            using Aes aes = Aes.Create();

            aes.Key = Key;

            aes.IV = IV;

            ICryptoTransform decryptor =
                aes.CreateDecryptor(aes.Key, aes.IV);

            byte[] buffer =
                Convert.FromBase64String(cipherText);

            using MemoryStream ms =
                new MemoryStream(buffer);

            using CryptoStream cs =
                new CryptoStream(
                    ms,
                    decryptor,
                    CryptoStreamMode.Read);

            using StreamReader sr =
                new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}