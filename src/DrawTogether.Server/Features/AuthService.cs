using System.Text.RegularExpressions;
using DrawTogether.Server.Data;
using DrawTogether.Shared.Messages;
using DrawTogether.Shared.Security;

namespace DrawTogether.Server.Features;

public sealed class AuthService
{
    private static readonly Regex UsernameRegex = new("^[a-zA-Z0-9_]{3,50}$", RegexOptions.Compiled);

    private readonly UserRepository _users;
    private readonly JwtTokenService _tokens;

    public AuthService(UserRepository users, JwtTokenService tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<ServiceResult<AuthResponse>> SignupAsync(
        SignupRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = NormalizeUsername(request.Username);
        var displayName = NormalizeDisplayName(request.DisplayName, username);

        var validationError = ValidateSignup(username, request.Password, displayName);
        if (validationError is not null)
        {
            return ServiceResult<AuthResponse>.Fail(validationError);
        }

        if (await _users.UsernameExistsAsync(username, cancellationToken))
        {
            return ServiceResult<AuthResponse>.Fail("Username already exists.");
        }

        var passwordHash = PasswordHelper.HashPassword(request.Password);
        var user = await _users.CreateAsync(username, passwordHash, displayName, cancellationToken);
        var (token, expiresAt) = _tokens.CreateToken(user);

        return ServiceResult<AuthResponse>.Ok(new AuthResponse
        {
            User = user,
            AccessToken = token,
            ExpiresAt = expiresAt
        }, "Signup successful.");
    }

    public async Task<ServiceResult<AuthResponse>> SigninAsync(
        SigninRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = NormalizeUsername(request.Username);

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return ServiceResult<AuthResponse>.Fail("Username and password are required.");
        }

        var authRecord = await _users.GetAuthRecordByUsernameAsync(username, cancellationToken);
        if (authRecord is null)
        {
            // Do not say whether username or password is wrong.
            return ServiceResult<AuthResponse>.Fail("Invalid username or password.");
        }

        if (!PasswordHelper.VerifyPassword(request.Password, authRecord.Value.PasswordHash))
        {
            return ServiceResult<AuthResponse>.Fail("Invalid username or password.");
        }

        var (token, expiresAt) = _tokens.CreateToken(authRecord.Value.User);

        return ServiceResult<AuthResponse>.Ok(new AuthResponse
        {
            User = authRecord.Value.User,
            AccessToken = token,
            ExpiresAt = expiresAt
        }, "Signin successful.");
    }

    public ServiceResult<AuthenticatedUser> ValidateAccessToken(string accessToken)
    {
        return _tokens.ValidateToken(accessToken);
    }

    // Stateless JWT logout: client deletes token. Server does not store/revoke sessions in DB.
    public ServiceResult<EmptyResult> Signout()
    {
        return ServiceResult<EmptyResult>.Ok(EmptyResult.Value, "Signout successful. Please delete access token on client.");
    }

    private static string NormalizeUsername(string username)
    {
        return username.Trim().ToLowerInvariant();
    }

    private static string NormalizeDisplayName(string displayName, string username)
    {
        var trimmed = displayName.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? username : trimmed;
    }

    private static string? ValidateSignup(string username, string password, string displayName)
    {
        if (!UsernameRegex.IsMatch(username))
        {
            return "Username must be 3-50 characters and contain only letters, digits, or underscore.";
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            return "Password must have at least 6 characters.";
        }

        if (displayName.Length is < 1 or > 100)
        {
            return "Display name must be 1-100 characters.";
        }

        return null;
    }
}
