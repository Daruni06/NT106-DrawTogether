namespace DrawTogether.Shared.Security;

public sealed class JwtOptions
{
    public string Issuer { get; init; } = "DrawTogether";
    public string Audience { get; init; } = "DrawTogether.Client";

    // Use at least 32 chars. Put the real value in config/env, not in GitHub.
    public string SecretKey { get; init; } = "CHANGE_THIS_SECRET_KEY_TO_32_CHARS_MIN";

    public int AccessTokenMinutes { get; init; } = 30;
}
