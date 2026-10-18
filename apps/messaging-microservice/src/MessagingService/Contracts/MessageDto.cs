namespace MessagingService.Contracts;

public record MessageDto(Guid Id, Guid ChannelId, Guid AuthorId, string Content, DateTimeOffset CreatedAt);

public record CreateMessageRequest(Guid AuthorId, string Content);
