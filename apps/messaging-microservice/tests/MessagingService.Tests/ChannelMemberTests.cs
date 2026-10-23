using MessagingService.Contracts;
using MessagingService.Domain;
using Xunit;

namespace MessagingService.Tests;

/// <summary>
/// Task 5 — tests for the "first feature": channel membership. These cover the
/// domain logic that the REST API and shared model build on (role changes, the
/// audit log, effective permissions and the narrow shared DTO).
/// </summary>
public class ChannelMemberTests
{
    private readonly Guid _channelId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void ChangeRole_UpdatesRole_AndRecordsAuditEntry()
    {
        var member = new ChannelMember(_channelId, _userId, ChannelRole.Member);

        member.ChangeRole(ChannelRole.Moderator, Guid.NewGuid());

        Assert.Equal(ChannelRole.Moderator, member.Role);
        var entry = Assert.Single(member.RoleHistory);
        Assert.Equal(ChannelRole.Member, entry.From);
        Assert.Equal(ChannelRole.Moderator, entry.To);
    }

    [Fact]
    public void ChangeRole_ToSameRole_IsNoop()
    {
        var member = new ChannelMember(_channelId, _userId, ChannelRole.Member);

        member.ChangeRole(ChannelRole.Member, Guid.NewGuid());

        Assert.Empty(member.RoleHistory);
        Assert.Equal(ChannelRole.Member, member.Role);
    }

    [Fact]
    public void EffectivePermissions_Member_HasDefaults()
    {
        var member = new ChannelMember(_channelId, _userId, ChannelRole.Member);

        var p = member.EffectivePermissions();

        Assert.True(p.CanPost);
        Assert.False(p.CanInvite);
        Assert.False(p.CanManageChannels);
    }

    [Fact]
    public void EffectivePermissions_Admin_CanManageChannels()
    {
        var member = new ChannelMember(_channelId, _userId, ChannelRole.Admin);

        var p = member.EffectivePermissions();

        Assert.True(p.CanPost);
        Assert.True(p.CanInvite);
        Assert.True(p.CanManageChannels);
    }

    [Fact]
    public void EffectivePermissions_OverrideTogglesPermission()
    {
        var member = new ChannelMember(_channelId, _userId, ChannelRole.Member);
        member.Overrides.Add(new PermissionOverride { Permission = "CanInvite", Allowed = true });

        var p = member.EffectivePermissions();

        Assert.True(p.CanInvite);
        Assert.False(p.CanManageChannels); // role default still applies elsewhere
    }

    [Fact]
    public void ToDto_ExposesOnlySharedFields()
    {
        var member = new ChannelMember(_channelId, _userId, ChannelRole.Admin);

        var dto = member.ToDto();

        Assert.Equal(_userId, dto.UserId);
        Assert.Equal(ChannelRoleType.Admin, dto.RoleType);
        Assert.True(dto.Permissions.CanManageChannels);
    }
}
