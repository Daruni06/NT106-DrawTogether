using DrawTogether.Shared.Models;

namespace DrawTogether.Shared.Messages;

public sealed class SignupRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class SigninRequest
{
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class AuthResponse
{
    public User User { get; init; } = new();
    public string AccessToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
}

public sealed class AuthenticatedUser
{
    public long UserId { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}
