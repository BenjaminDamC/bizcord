using MessagingService.Domain;

namespace MessagingService.Data;

public interface IChannelRepository
{
    Task<IReadOnlyList<Channel>> GetAllAsync();
    Task<Channel?> GetByIdAsync(Guid id);
    Task AddAsync(Channel channel);
}

/// <summary>In-memory implementation — swap for a real store later.</summary>
public class InMemoryChannelRepository : IChannelRepository
{
    private readonly List<Channel> _channels = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<Channel>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<Channel>>(_channels.ToList());
        }
    }

    public Task<Channel?> GetByIdAsync(Guid id)
    {
        lock (_lock)
        {
            return Task.FromResult(_channels.FirstOrDefault(c => c.Id == id));
        }
    }

    public Task AddAsync(Channel channel)
    {
        lock (_lock)
        {
            _channels.Add(channel);
        }
        return Task.CompletedTask;
    }
}
