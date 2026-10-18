namespace MessagingService.Domain;

public class Message
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ChannelId { get; init; }
    public Guid AuthorId { get; init; }
    public string Content { get; set; } = "";
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EditedAt { get; set; }
}
