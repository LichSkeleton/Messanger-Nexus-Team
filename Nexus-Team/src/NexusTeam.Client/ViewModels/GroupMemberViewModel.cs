namespace NexusTeam.Client.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// View model for a member in the group members list.
    /// </summary>
    public class GroupMemberViewModel : ObservableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupMemberViewModel"/> class.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="isOwner">Whether this user owns the group.</param>
        /// <param name="canRemove">Whether the current user can remove this member.</param>
        public GroupMemberViewModel(UserDto user, bool isOwner, bool canRemove)
        {
            this.User = user;
            this.IsOwner = isOwner;
            this.CanRemove = canRemove;
        }

        /// <summary>
        /// Gets the underlying user.
        /// </summary>
        public UserDto User { get; }

        /// <summary>
        /// Gets the user ID.
        /// </summary>
        public string Id => this.User.Id;

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName => !string.IsNullOrWhiteSpace(this.User.DisplayName)
            ? this.User.DisplayName
            : this.User.Username;

        /// <summary>
        /// Gets the username.
        /// </summary>
        public string Username => this.User.Username;

        /// <summary>
        /// Gets the avatar URL.
        /// </summary>
        public string? AvatarUrl => this.User.AvatarUrl;

        /// <summary>
        /// Gets the user status.
        /// </summary>
        public UserStatus Status => this.User.Status;

        /// <summary>
        /// Gets a value indicating whether this member is the group owner.
        /// </summary>
        public bool IsOwner { get; }

        /// <summary>
        /// Gets a value indicating whether the current user can remove this member.
        /// </summary>
        public bool CanRemove { get; }

        /// <summary>
        /// Gets the role label shown next to the name.
        /// </summary>
        public string RoleLabel => this.IsOwner ? "Owner" : "Member";
    }
}
