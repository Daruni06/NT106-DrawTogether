namespace DrawTogether.Shared.Models;

public sealed class Room
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public long OwnerUserId { get; init; }
    public int MaxMembers { get; init; }
    public bool IsClosed { get; init; }
    public DateTime CreatedAt { get; init; }
    public int ActiveMemberCount { get; init; }
}
