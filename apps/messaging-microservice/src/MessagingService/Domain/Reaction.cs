namespace MessagingService.Domain;

public class Reaction
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MessageId { get; init; }
    public Guid UserId { get; init; }
    public string Emoji { get; init; } = "";
}
