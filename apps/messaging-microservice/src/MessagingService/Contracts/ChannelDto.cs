namespace MessagingService.Contracts;

/// <summary>
/// Shared model for a channel — only the fields other services need to integrate
/// with. Internal details (workspace id, DM flag) stay in the domain entity.
/// </summary>
public record ChannelDto(Guid Id, string Name, string Description, DateTimeOffset CreatedAt);

public record CreateChannelRequest(string Name, string Description);
