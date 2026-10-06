namespace MessagingService.Messages;

/// <summary>
/// Published when a user posts a message. Downstream services (Notifications,
/// Search) subscribe to this so the sender never waits on them.
/// </summary>
public record MessageCreated(
    Guid MessageId,
    Guid ChannelId,
    Guid AuthorId,
    string Content,
    DateTimeOffset CreatedAt);
