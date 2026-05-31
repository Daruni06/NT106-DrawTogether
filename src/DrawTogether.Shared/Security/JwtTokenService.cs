using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Models;
using Microsoft.IdentityModel.Tokens;

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

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("display_name", user.DisplayName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expiresAt);
    }

    public ServiceResult<AuthenticatedUser> ValidateToken(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return ServiceResult<AuthenticatedUser>.Fail("Missing access token.");
        }

        var handler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.SecretKey);

        try
        {
            var principal = handler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,
                ValidateAudience = true,
                ValidAudience = _options.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            }, out _);

            var userIdText = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            var username = principal.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? string.Empty;
            var displayName = principal.FindFirstValue("display_name") ?? username;

            if (!long.TryParse(userIdText, out var userId))
            {
                return ServiceResult<AuthenticatedUser>.Fail("Invalid token subject.");
            }

            return ServiceResult<AuthenticatedUser>.Ok(new AuthenticatedUser
            {
                UserId = userId,
                Username = username,
                DisplayName = displayName
            });
        }
        catch (SecurityTokenExpiredException)
        {
            return ServiceResult<AuthenticatedUser>.Fail("Access token expired.");
        }
        catch (Exception)
        {
            return ServiceResult<AuthenticatedUser>.Fail("Invalid access token.");
        }
    }
}
