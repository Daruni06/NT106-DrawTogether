using System;
using System.Security.Cryptography;
using System.Text;

namespace DrawTogether.Shared.Security
{
    public static class PasswordHelper
    {
        public static string HashPassword(string password)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] bytes =
                    Encoding.UTF8.GetBytes(password);

                byte[] hash =
                    sha.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(
            string password,
            string storedHash)
        {
            string hash =
                HashPassword(password);

            return hash == storedHash;
        }

        // Token Validation
        public static bool ValidateToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;

            if (token.Length < 16)
                return false;

            return true;
        }

        public static string GenerateToken()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}