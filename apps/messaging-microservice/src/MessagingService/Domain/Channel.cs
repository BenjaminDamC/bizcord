namespace MessagingService.Domain;

/// <summary>
/// A channel (or direct-message thread) in a workspace. This is the full internal
/// entity — the shared model (Contracts) exposes only a subset of these fields.
/// </summary>
public class Channel
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid WorkspaceId { get; init; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsDirectMessage { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
