namespace DrawTogether.Shared.Security;

public static class PasswordHelper
{
    public static string HashPassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new ArgumentException("Password is required.", nameof(plainPassword));
        }

        return BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public static bool VerifyPassword(string plainPassword, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(plainPassword) || string.IsNullOrWhiteSpace(passwordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
    }
}
