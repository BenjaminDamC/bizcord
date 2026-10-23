using MessagingService.Data;
using MessagingService.Domain;
using Xunit;

namespace MessagingService.Tests;

public class ChannelMemberRepositoryTests
{
    [Fact]
    public async Task AddAsync_ThenGetAsync_ReturnsMember()
    {
        var repo = new InMemoryChannelMemberRepository();
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member = new ChannelMember(channelId, userId, ChannelRole.Member);

        await repo.AddAsync(member);

        var found = await repo.GetAsync(channelId, userId);
        Assert.NotNull(found);
        Assert.Equal(userId, found!.UserId);
    }

    [Fact]
    public async Task GetByChannelAsync_OnlyReturnsMembersOfThatChannel()
    {
        var repo = new InMemoryChannelMemberRepository();
        var channelA = Guid.NewGuid();
        var channelB = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        await repo.AddAsync(new ChannelMember(channelA, userA, ChannelRole.Member));
        await repo.AddAsync(new ChannelMember(channelA, userB, ChannelRole.Moderator));
        await repo.AddAsync(new ChannelMember(channelB, userA, ChannelRole.Admin));

        var members = await repo.GetByChannelAsync(channelA);

        Assert.Equal(2, members.Count);
        Assert.All(members, m => Assert.Equal(channelA, m.ChannelId));
    }
}
