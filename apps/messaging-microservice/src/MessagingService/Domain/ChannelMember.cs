using MessagingService.Contracts;

namespace MessagingService.Domain;

public enum ChannelRole
{
    Member,
    Moderator,
    Admin
}

/// <summary>
/// A user's membership in a channel. Richer internally than what other services
/// see: it keeps the role, per-channel permission overrides, and an audit log of
/// every role change. The shared model (<see cref="ChannelMemberDto"/>) exposes
/// only the resolved role + effective permissions.
/// </summary>
public class ChannelMember
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid ChannelId { get; init; }
    public Guid UserId { get; init; }
    public ChannelRole Role { get; private set; }

    // Internal: per-channel overrides on top of the role's defaults.
    public List<PermissionOverride> Overrides { get; } = [];

    // Internal: audit log of every role change.
    public List<RoleChange> RoleHistory { get; } = [];

    public ChannelMember(Guid channelId, Guid userId, ChannelRole role = ChannelRole.Member)
    {
        ChannelId = channelId;
        UserId = userId;
        Role = role;
    }

    public void ChangeRole(ChannelRole newRole, Guid changedBy)
    {
        if (newRole == Role)
        {
            return;
        }

        RoleHistory.Add(new RoleChange(Role, newRole, changedBy, DateTimeOffset.UtcNow));
        Role = newRole;
    }

    /// <summary>
    /// Resolves the effective permissions for this member — role defaults plus any
    /// per-channel overrides. The override list itself stays internal.
    /// </summary>
    public ChannelPermissions EffectivePermissions()
    {
        var permissions = Role switch
        {
            ChannelRole.Admin => new ChannelPermissions(true, true, true),
            ChannelRole.Moderator => new ChannelPermissions(true, true, false),
            _ => new ChannelPermissions(true, false, false),
        };

        foreach (var o in Overrides)
        {
            permissions = o.Permission switch
            {
                nameof(ChannelPermissions.CanPost) => permissions with { CanPost = o.Allowed },
                nameof(ChannelPermissions.CanInvite) => permissions with { CanInvite = o.Allowed },
                nameof(ChannelPermissions.CanManageChannels) => permissions with { CanManageChannels = o.Allowed },
                _ => permissions,
            };
        }

        return permissions;
    }

    public ChannelMemberDto ToDto()
    {
        var roleType = Role switch
        {
            ChannelRole.Admin => ChannelRoleType.Admin,
            ChannelRole.Moderator => ChannelRoleType.Moderator,
            _ => ChannelRoleType.Member,
        };

        return new ChannelMemberDto(UserId, roleType, EffectivePermissions());
    }
}

/// <summary>A single permission toggle that overrides the role default.</summary>
public class PermissionOverride
{
    public string Permission { get; init; } = "";
    public bool Allowed { get; init; }
}

/// <summary>Audit entry recording a role change.</summary>
public class RoleChange
{
    public ChannelRole From { get; init; }
    public ChannelRole To { get; init; }
    public Guid ChangedBy { get; init; }
    public DateTimeOffset ChangedAt { get; init; }

    public RoleChange(ChannelRole from, ChannelRole to, Guid changedBy, DateTimeOffset changedAt)
    {
        From = from;
        To = to;
        ChangedBy = changedBy;
        ChangedAt = changedAt;
    }
}
