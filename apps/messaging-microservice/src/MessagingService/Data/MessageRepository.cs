using MessagingService.Domain;

namespace MessagingService.Data;

public interface IMessageRepository
{
    Task<IReadOnlyList<Message>> GetByChannelAsync(Guid channelId);
    Task AddAsync(Message message);
}

public class InMemoryMessageRepository : IMessageRepository
{
    private readonly List<Message> _messages = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<Message>> GetByChannelAsync(Guid channelId)
    {
        lock (_lock)
        {
            var result = _messages
                .Where(m => m.ChannelId == channelId)
                .OrderBy(m => m.CreatedAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<Message>>(result);
        }
    }

    public Task AddAsync(Message message)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }
        return Task.CompletedTask;
    }
}
