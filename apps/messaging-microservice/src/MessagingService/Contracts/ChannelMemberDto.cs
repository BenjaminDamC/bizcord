namespace MessagingService.Contracts;

/// <summary>
/// Shared model for a channel membership. Other services only need to know who is
/// in a channel, their role, and what they can do — not the override list or the
/// audit log the domain keeps internally.
/// </summary>
public record ChannelMemberDto(Guid UserId, ChannelRoleType RoleType, ChannelPermissions Permissions);

public enum ChannelRoleType
{
    Member,
    Moderator,
    Admin
}

public record ChannelPermissions(bool CanPost, bool CanInvite, bool CanManageChannels);

public record CreateChannelMemberRequest(Guid UserId, ChannelRoleType RoleType);

public record UpdateMemberRoleRequest(ChannelRoleType RoleType, Guid ChangedBy);
