using MessagingService.Domain;

namespace MessagingService.Data;

public interface IChannelMemberRepository
{
    Task<IReadOnlyList<ChannelMember>> GetByChannelAsync(Guid channelId);
    Task<ChannelMember?> GetAsync(Guid channelId, Guid userId);
    Task AddAsync(ChannelMember member);
}

public class InMemoryChannelMemberRepository : IChannelMemberRepository
{
    private readonly List<ChannelMember> _members = [];
    private readonly object _lock = new();

    public Task<IReadOnlyList<ChannelMember>> GetByChannelAsync(Guid channelId)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<ChannelMember>>(
                _members.Where(m => m.ChannelId == channelId).ToList());
        }
    }

    public Task<ChannelMember?> GetAsync(Guid channelId, Guid userId)
    {
        lock (_lock)
        {
            return Task.FromResult(_members.FirstOrDefault(m => m.ChannelId == channelId && m.UserId == userId));
        }
    }

    public Task AddAsync(ChannelMember member)
    {
        lock (_lock)
        {
            _members.Add(member);
        }
        return Task.CompletedTask;
    }
}
