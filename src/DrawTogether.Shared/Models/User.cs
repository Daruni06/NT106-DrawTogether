namespace DrawTogether.Shared.Models;

public sealed class User
{
    public long Id { get; init; }
    public string Username { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
