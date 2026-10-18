namespace MessagingService.Domain;

public class ReadState
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ChannelId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset LastReadAt { get; set; }
}
