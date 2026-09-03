namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using CommunityToolkit.Mvvm.ComponentModel;
    using NexusTeam.Client.Services;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// View model for a conversation in the conversation list.
    /// </summary>
    public partial class ConversationViewModel : ObservableObject
    {
        private readonly IAvatarService? avatarService;
        private string id;
        private ChatType type;
        private string name;
        private string? avatarUrl;
        private DateTime? lastMessageAt;
        private string lastMessagePreview;
        private int unreadCount;
        private bool isSelected;
        private bool isTyping;
        private UserStatus status = UserStatus.Offline;
        private string? otherUserId;
        private BitmapImage? avatarImage;
        private List<UserDto> participants;
        private string createdBy = string.Empty;
        private string? currentUserId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConversationViewModel"/> class.
        /// </summary>
        /// <param name="chatDto">The chat DTO.</param>
        /// <param name="currentUserId">The current user's ID to identify the other participant.</param>
        /// <param name="avatarService">Optional avatar service for loading avatars.</param>
        public ConversationViewModel(ChatDto chatDto, string? currentUserId = null, IAvatarService? avatarService = null)
        {
            this.id = chatDto.Id;
            this.type = chatDto.Type;
            this.name = chatDto.Name ?? "Direct Message";
            this.avatarUrl = chatDto.AvatarUrl;
            this.lastMessageAt = chatDto.LastMessageAt;
            this.lastMessagePreview = string.Empty;
            this.avatarService = avatarService;
            this.participants = chatDto.Participants ?? new List<UserDto>();
            this.createdBy = chatDto.CreatedBy ?? string.Empty;
            this.currentUserId = currentUserId;

            if ((this.type == ChatType.Group || this.type == ChatType.Channel)
                && string.IsNullOrWhiteSpace(this.avatarUrl))
            {
                this.avatarUrl = $"/api/users/avatar/chat_{this.id}";
            }

            // Personal chats: DirectMessage type, or exactly two participants (legacy/mis-typed chats)
            var isSavedMessages = this.type == ChatType.SavedMessages
                || (!string.IsNullOrEmpty(this.id) && this.id.StartsWith("saved-", StringComparison.Ordinal));
            var isPersonalChat = !isSavedMessages
                && (this.type == ChatType.DirectMessage || this.participants.Count == 2);

            if (isSavedMessages)
            {
                this.name = "Saved Messages";
            }
            else if (isPersonalChat && this.participants.Count > 0)
            {
                // Find the other participant (not the current user)
                var otherUser = !string.IsNullOrEmpty(currentUserId)
                    ? this.participants.FirstOrDefault(p => p.Id != currentUserId)
                    : this.participants.FirstOrDefault();

                if (otherUser != null)
                {
                    this.otherUserId = otherUser.Id;
                    this.status = otherUser.Status == UserStatus.Invisible
                        ? UserStatus.Offline
                        : otherUser.Status;

                    // Use DisplayName for personal chats if available
                    if (!string.IsNullOrWhiteSpace(otherUser.DisplayName))
                    {
                        this.name = otherUser.DisplayName;
                    }
                    else if (!string.IsNullOrWhiteSpace(otherUser.Username))
                    {
                        this.name = otherUser.Username;
                    }

                    if (!string.IsNullOrWhiteSpace(otherUser.AvatarUrl))
                    {
                        this.avatarUrl = otherUser.AvatarUrl;
                    }
                }
            }

            // Load avatar asynchronously
            this.LoadAvatarAsync();
        }

        /// <summary>
        /// Gets the avatar image.
        /// </summary>
        public BitmapImage? AvatarImage
        {
            get => this.avatarImage;
            private set => this.SetProperty(ref this.avatarImage, value);
        }

        /// <summary>
        /// Updates the avatar URL and reloads the image.
        /// </summary>
        /// <param name="newAvatarUrl">The new avatar URL.</param>
        public void UpdateAvatarUrl(string? newAvatarUrl)
        {
            var urlChanged = this.avatarUrl != newAvatarUrl;
            if (urlChanged)
            {
                this.avatarUrl = newAvatarUrl;
                this.OnPropertyChanged(nameof(this.AvatarUrl));
            }

            // Always reload image to get fresh version (even if URL didn't change, server image might have)
            if (!string.IsNullOrEmpty(newAvatarUrl) || urlChanged)
            {
                this.LoadAvatarAsync();
            }
        }

        /// <summary>
        /// Gets the chat ID.
        /// </summary>
        public string Id
        {
            get => this.id;
            private set => this.SetProperty(ref this.id, value);
        }

        /// <summary>
        /// Gets the chat type.
        /// </summary>
        public ChatType Type
        {
            get => this.type;
            private set => this.SetProperty(ref this.type, value);
        }

        /// <summary>
        /// Gets or sets the chat name.
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        /// <summary>
        /// Gets or sets the avatar URL.
        /// </summary>
        public string? AvatarUrl
        {
            get => this.avatarUrl;
            set => this.SetProperty(ref this.avatarUrl, value);
        }

        /// <summary>
        /// Gets or sets the last message timestamp.
        /// </summary>
        public DateTime? LastMessageAt
        {
            get => this.lastMessageAt;
            set => this.SetProperty(ref this.lastMessageAt, value);
        }

        /// <summary>
        /// Gets or sets the last message preview text.
        /// </summary>
        public string LastMessagePreview
        {
            get => this.lastMessagePreview;
            set => this.SetProperty(ref this.lastMessagePreview, value);
        }

        /// <summary>
        /// Gets or sets the unread message count.
        /// </summary>
        public int UnreadCount
        {
            get => this.unreadCount;
            set => this.SetProperty(ref this.unreadCount, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this conversation is selected.
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether someone is typing.
        /// </summary>
        public bool IsTyping
        {
            get => this.isTyping;
            set => this.SetProperty(ref this.isTyping, value);
        }

        /// <summary>
        /// Gets or sets the current status of the user in this conversation.
        /// </summary>
        public UserStatus Status
        {
            get => this.status;
            set => this.SetProperty(ref this.status, value);
        }

        /// <summary>
        /// Gets a value indicating whether the online-status circle should be shown (personal chats).
        /// </summary>
        public bool ShowStatusIndicator => this.type != ChatType.SavedMessages
            && !(this.id ?? string.Empty).StartsWith("saved-", StringComparison.Ordinal)
            && (!string.IsNullOrEmpty(this.otherUserId)
            || this.type == ChatType.DirectMessage
            || this.participants.Count == 2);

        /// <summary>
        /// Gets a value indicating whether this conversation is a group or channel.
        /// </summary>
        public bool IsGroup => this.type == ChatType.Group || this.type == ChatType.Channel;

        /// <summary>
        /// Gets a value indicating whether the current user owns this group.
        /// </summary>
        public bool IsOwner => this.IsGroup
            && !string.IsNullOrEmpty(this.currentUserId)
            && string.Equals(this.createdBy, this.currentUserId, StringComparison.Ordinal);

        /// <summary>
        /// Gets the number of members in this conversation.
        /// </summary>
        public int MemberCount => this.participants?.Count ?? 0;

        /// <summary>
        /// Gets a short members label for group headers.
        /// </summary>
        public string MemberCountText => this.MemberCount == 1
            ? "1 member"
            : $"{this.MemberCount} members";

        /// <summary>
        /// Gets a value indicating whether this conversation can be deleted by the current user.
        /// Groups can only be deleted by the owner. Saved Messages cannot be deleted.
        /// </summary>
        public bool CanDeleteChat => this.type != ChatType.SavedMessages
            && (!this.IsGroup || this.IsOwner);

        /// <summary>
        /// Gets the creator/owner user ID.
        /// </summary>
        public string CreatedBy
        {
            get => this.createdBy;
            private set => this.SetProperty(ref this.createdBy, value);
        }

        /// <summary>
        /// Gets the user ID of the other participant (for direct messages).
        /// </summary>
        public string? OtherUserId
        {
            get => this.otherUserId;
            private set => this.otherUserId = value;
        }

        /// <summary>
        /// Gets the list of participants in this chat.
        /// </summary>
        public List<UserDto> Participants
        {
            get => this.participants;
            private set => this.participants = value;
        }

        /// <summary>
        /// Gets the display name of a participant by their user ID.
        /// </summary>
        /// <param name="userId">The user ID to look up.</param>
        /// <returns>The display name or username, or null if not found.</returns>
        public string? GetParticipantName(string userId)
        {
            var participant = this.participants.FirstOrDefault(p => p.Id == userId);
            if (participant == null)
            {
                return null;
            }

            return !string.IsNullOrWhiteSpace(participant.DisplayName)
                ? participant.DisplayName
                : participant.Username;
        }

        /// <summary>
        /// Gets the avatar URL of a participant by their user ID.
        /// </summary>
        /// <param name="userId">The user ID to look up.</param>
        /// <returns>The avatar URL, or null if not found.</returns>
        public string? GetParticipantAvatarUrl(string userId)
        {
            var participant = this.participants.FirstOrDefault(p => p.Id == userId);
            return participant?.AvatarUrl;
        }

        /// <summary>
        /// Updates the participants list.
        /// </summary>
        /// <param name="participants">The new list of participants.</param>
        public void UpdateParticipants(List<UserDto> participants)
        {
            if (participants != null)
            {
                this.participants = participants;
                this.OnPropertyChanged(nameof(this.Participants));
                this.OnPropertyChanged(nameof(this.MemberCount));
                this.OnPropertyChanged(nameof(this.MemberCountText));
            }
        }

        /// <summary>
        /// Updates the owner ID after a membership change.
        /// </summary>
        /// <param name="createdBy">The new owner user ID.</param>
        public void UpdateCreatedBy(string createdBy)
        {
            this.CreatedBy = createdBy ?? string.Empty;
            this.OnPropertyChanged(nameof(this.IsOwner));
            this.OnPropertyChanged(nameof(this.CanDeleteChat));
        }

        /// <summary>
        /// Gets the formatted last message time.
        /// </summary>
        public string LastMessageTime
        {
            get
            {
                if (!this.LastMessageAt.HasValue)
                {
                    return string.Empty;
                }

                var now = DateTime.UtcNow;
                var diff = now - this.LastMessageAt.Value;

                if (diff.TotalMinutes < 1)
                {
                    return "Just now";
                }

                if (diff.TotalHours < 1)
                {
                    return $"{(int)diff.TotalMinutes}m ago";
                }

                if (diff.TotalDays < 1)
                {
                    return $"{(int)diff.TotalHours}h ago";
                }

                if (diff.TotalDays < 7)
                {
                    return $"{(int)diff.TotalDays}d ago";
                }

                return this.LastMessageAt.Value.ToString("MMM dd");
            }
        }

        /// <summary>
        /// Loads the avatar image asynchronously.
        /// </summary>
        private async void LoadAvatarAsync()
        {
            if (this.avatarService == null)
            {
                return;
            }

            try
            {
                var avatarUrl = this.AvatarUrl;
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    this.AvatarImage = this.avatarService.GetDefaultAvatar();
                    return;
                }

                // Add timestamp to force refresh and bypass cache
                var separator = avatarUrl.Contains('?') ? "&" : "?";
                var cacheBustingUrl = $"{avatarUrl}{separator}t={DateTime.UtcNow.Ticks}";
                this.AvatarImage = await this.avatarService.LoadAvatarAsync(cacheBustingUrl);
            }
            catch
            {
                // Silently fail and use default avatar
                this.AvatarImage = this.avatarService.GetDefaultAvatar();
            }
        }
    }
}
