using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;

namespace DrawTogether.Shared.Security;

public sealed class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(JwtOptions options)
    {
        _options = options;
        if (string.IsNullOrWhiteSpace(_options.SecretKey) || _options.SecretKey.Length < 32)
        {
            throw new ArgumentException("JWT secret key must have at least 32 characters.");
        }
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes);
        var payload = new TokenPayload
        {
            UserId = user.Id,
            Username = user.Username,
            DisplayName = user.DisplayName,
            ExpiresAtUnix = new DateTimeOffset(expiresAt).ToUnixTimeSeconds()
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadPart = Base64UrlEncode(Encoding.UTF8.GetBytes(payloadJson));
        var signaturePart = Sign(payloadPart);

        return ($"{payloadPart}.{signaturePart}", expiresAt);
    }

    public ServiceResult<AuthenticatedUser> ValidateToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ServiceResult<AuthenticatedUser>.Fail("Missing access token.");
        }

        var parts = accessToken.Split('.');
        if (parts.Length != 2)
        {
            return ServiceResult<AuthenticatedUser>.Fail("Invalid access token.");
        }

        var expectedSignature = Sign(parts[0]);

        if (!string.Equals(parts[1], expectedSignature, StringComparison.Ordinal))
        {
            return ServiceResult<AuthenticatedUser>.Fail("Invalid access token.");
        }

        try
        {
            var payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[0]));
            var payload = JsonSerializer.Deserialize<TokenPayload>(payloadJson);

            if (payload is null)
            {
                return ServiceResult<AuthenticatedUser>.Fail("Invalid access token.");
            }

            var nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (payload.ExpiresAtUnix < nowUnix)
            {
                return ServiceResult<AuthenticatedUser>.Fail("Access token expired.");
            }

            return ServiceResult<AuthenticatedUser>.Ok(new AuthenticatedUser
            {
                UserId = payload.UserId,
                Username = payload.Username,
                DisplayName = payload.DisplayName
            });
        }
        catch
        {
            return ServiceResult<AuthenticatedUser>.Fail("Invalid access token.");
        }
    }

    private string Sign(string payloadPart)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        return Base64UrlEncode(hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadPart)));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private sealed class TokenPayload
    {
        public long UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public long ExpiresAtUnix { get; set; }
    }
}
