namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Media.Imaging;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Helpers;

    /// <summary>
    /// View model for a single message.
    /// </summary>
    public partial class MessageViewModel : ObservableObject, IMessageListItem
    {
        private readonly IFileAttachmentService? fileAttachmentService;
        private readonly IAvatarService? avatarService;
        private string id;
        private string chatId;
        private string senderId;
        private string content;
        private MessageStatus status;
        private DateTime createdAt;
        private DateTime? editedAt;
        private bool isDeleted;
        private bool isEditing;
        private bool isCurrentUser;
        private ObservableCollection<AttachmentViewModel> attachments;
        private Dictionary<string, List<string>> reactions;
        private ObservableCollection<KeyValuePair<string, List<string>>>? reactionsList;
        private BitmapImage? senderAvatarImage;
        private string? senderName;
        private string? senderAvatarUrl;
        private bool isGroupChat;
        private bool isContentExpanded;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageViewModel"/> class.
        /// </summary>
        /// <param name="messageDto">The message DTO.</param>
        /// <param name="currentUserId">The current user's ID.</param>
        /// <param name="fileAttachmentService">Optional file attachment service for loading images.</param>
        /// <param name="avatarService">Optional avatar service for loading avatars.</param>
        /// <param name="senderName">Optional sender name.</param>
        /// <param name="senderAvatarUrl">Optional sender avatar URL.</param>
        /// <param name="isGroupChat">Whether this message is in a group chat.</param>
        public MessageViewModel(
            MessageDto messageDto,
            string currentUserId,
            IFileAttachmentService? fileAttachmentService = null,
            IAvatarService? avatarService = null,
            string? senderName = null,
            string? senderAvatarUrl = null,
            bool isGroupChat = false)
        {
            this.fileAttachmentService = fileAttachmentService;
            this.avatarService = avatarService;
            this.id = messageDto.Id;
            this.chatId = messageDto.ChatId;
            this.senderId = messageDto.SenderId;
            this.content = messageDto.Content;
            this.status = messageDto.Status;
            this.createdAt = messageDto.CreatedAt;
            this.editedAt = messageDto.EditedAt;
            this.isDeleted = messageDto.IsDeleted;
            this.isCurrentUser = messageDto.SenderId == currentUserId;
            this.senderName = senderName;
            this.senderAvatarUrl = senderAvatarUrl;
            this.isGroupChat = isGroupChat;
            this.attachments = new ObservableCollection<AttachmentViewModel>(
                messageDto.Attachments?.Select(a => new AttachmentViewModel(a, fileAttachmentService)) ?? Enumerable.Empty<AttachmentViewModel>());
            this.reactions = messageDto.Reactions ?? new Dictionary<string, List<string>>();
            this.UpdateReactionsList();

            // Load sender avatar if not current user
            if (!this.isCurrentUser && this.avatarService != null)
            {
                this.LoadSenderAvatarAsync();
            }
        }

        /// <summary>
        /// Gets the sender's avatar image.
        /// </summary>
        public BitmapImage? SenderAvatarImage
        {
            get => this.senderAvatarImage;
            private set => this.SetProperty(ref this.senderAvatarImage, value);
        }

        /// <summary>
        /// Gets the sender's name.
        /// </summary>
        public string? SenderName
        {
            get => this.senderName;
            private set => this.SetProperty(ref this.senderName, value);
        }

        /// <summary>
        /// Gets a value indicating whether this message is in a group chat.
        /// </summary>
        public bool IsGroupChat
        {
            get => this.isGroupChat;
            private set => this.SetProperty(ref this.isGroupChat, value);
        }

        /// <summary>
        /// Gets a value indicating whether the sender name should be displayed.
        /// In direct messages, never show name.
        /// In group chats, show name only for other users' messages (not own).
        /// </summary>
        public bool ShouldShowSenderName
        {
            get
            {
                // In direct messages, never show name
                if (!this.isGroupChat)
                {
                    return false;
                }

                // In group chats, show name only for other users' messages (not own)
                return !this.isCurrentUser && !string.IsNullOrEmpty(this.senderName);
            }
        }

        /// <summary>
        /// Updates the sender's avatar URL and reloads the image.
        /// </summary>
        /// <param name="newAvatarUrl">The new avatar URL.</param>
        public void UpdateSenderAvatarUrl(string? newAvatarUrl)
        {
            var urlChanged = this.senderAvatarUrl != newAvatarUrl;
            if (urlChanged)
            {
                this.senderAvatarUrl = newAvatarUrl;
            }

            // Always reload image to get fresh version (even if URL didn't change, server image might have)
            if (!string.IsNullOrEmpty(newAvatarUrl) || urlChanged)
            {
                this.LoadSenderAvatarAsync();
            }
        }

        /// <summary>
        /// Gets the message ID.
        /// </summary>
        public string Id
        {
            get => this.id;
            private set => this.SetProperty(ref this.id, value);
        }

        /// <summary>
        /// Gets the chat ID.
        /// </summary>
        public string ChatId
        {
            get => this.chatId;
            private set => this.SetProperty(ref this.chatId, value);
        }

        /// <summary>
        /// Gets the sender ID.
        /// </summary>
        public string SenderId
        {
            get => this.senderId;
            private set => this.SetProperty(ref this.senderId, value);
        }

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content
        {
            get => this.content;
            set
            {
                if (this.SetProperty(ref this.content, value))
                {
                    this.OnPropertyChanged(nameof(this.HasContent));
                    this.NotifyContentDisplayProperties();
                }
            }
        }

        /// <summary>
        /// Gets or sets the message status.
        /// </summary>
        public MessageStatus Status
        {
            get => this.status;
            set => this.SetProperty(ref this.status, value);
        }

        /// <summary>
        /// Gets the created timestamp.
        /// </summary>
        public DateTime CreatedAt
        {
            get => this.createdAt;
            private set => this.SetProperty(ref this.createdAt, value);
        }

        /// <summary>
        /// Gets or sets the edited timestamp.
        /// </summary>
        public DateTime? EditedAt
        {
            get => this.editedAt;
            set => this.SetProperty(ref this.editedAt, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the message is deleted.
        /// </summary>
        public bool IsDeleted
        {
            get => this.isDeleted;
            set => this.SetProperty(ref this.isDeleted, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the message is being edited.
        /// </summary>
        public bool IsEditing
        {
            get => this.isEditing;
            set => this.SetProperty(ref this.isEditing, value);
        }

        /// <summary>
        /// Gets a value indicating whether this message was sent by the current user.
        /// </summary>
        public bool IsCurrentUser
        {
            get => this.isCurrentUser;
            private set => this.SetProperty(ref this.isCurrentUser, value);
        }

        /// <summary>
        /// Gets a value indicating whether the message is edited.
        /// </summary>
        public bool IsEdited => this.EditedAt.HasValue;

        /// <summary>
        /// Gets the attachments collection.
        /// </summary>
        public ObservableCollection<AttachmentViewModel> Attachments
        {
            get => this.attachments;
        }

        /// <summary>
        /// Gets a value indicating whether this message has attachments.
        /// </summary>
        public bool HasAttachments => this.attachments != null && this.attachments.Count > 0;

        /// <summary>
        /// Gets a value indicating whether this message has image attachments.
        /// </summary>
        public bool HasImageAttachments => this.attachments != null && this.attachments.Any(a => a.IsImage);

        /// <summary>
        /// Gets a value indicating whether this message has visible content (not just whitespace).
        /// </summary>
        public bool HasContent => !string.IsNullOrWhiteSpace(this.content);

        /// <summary>
        /// Gets a value indicating whether the message is long enough to show Read more.
        /// </summary>
        public bool NeedsReadMore => MessageContentHelper.NeedsTruncation(this.content);

        /// <summary>
        /// Gets or sets a value indicating whether the full long message is expanded.
        /// </summary>
        public bool IsContentExpanded
        {
            get => this.isContentExpanded;
            set
            {
                if (this.SetProperty(ref this.isContentExpanded, value))
                {
                    this.NotifyContentDisplayProperties();
                }
            }
        }

        /// <summary>
        /// Gets the message text shown in the bubble, truncated until expanded.
        /// </summary>
        public string DisplayContent => MessageContentHelper.GetDisplayContent(this.content, this.isContentExpanded);

        /// <summary>
        /// Gets the label for the Read more / Show less control.
        /// </summary>
        public string ReadMoreButtonText => this.isContentExpanded ? "Show less" : "Read more";

        /// <summary>
        /// Toggles whether a long message is fully visible.
        /// </summary>
        [RelayCommand]
        private void ToggleContentExpansion()
        {
            this.IsContentExpanded = !this.IsContentExpanded;
        }

        /// <summary>
        /// Gets the reactions dictionary.
        /// Key is emoji, value is list of user IDs who reacted.
        /// </summary>
        public Dictionary<string, List<string>> Reactions
        {
            get => this.reactions;
            private set
            {
                if (this.SetProperty(ref this.reactions, value))
                {
                    this.UpdateReactionsList();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether this message has any reactions.
        /// </summary>
        public bool HasReactions => this.reactions != null && this.reactions.Count > 0;

        /// <summary>
        /// Checks if the current user has reacted with a specific emoji.
        /// </summary>
        /// <param name="emoji">The emoji to check.</param>
        /// <param name="currentUserId">The current user ID.</param>
        /// <returns>True if the user has reacted with this emoji.</returns>
        public bool HasUserReacted(string emoji, string currentUserId)
        {
            return this.reactions != null &&
                   this.reactions.ContainsKey(emoji) &&
                   this.reactions[emoji].Contains(currentUserId);
        }

        /// <summary>
        /// Gets the count of reactions for a specific emoji.
        /// </summary>
        /// <param name="emoji">The emoji.</param>
        /// <returns>The count of users who reacted with this emoji.</returns>
        public int GetReactionCount(string emoji)
        {
            return this.reactions != null && this.reactions.ContainsKey(emoji)
                ? this.reactions[emoji].Count
                : 0;
        }

        /// <summary>
        /// Gets the reactions as a collection for binding.
        /// </summary>
        public ObservableCollection<KeyValuePair<string, List<string>>> ReactionsList
        {
            get
            {
                if (this.reactionsList == null)
                {
                    this.reactionsList = new ObservableCollection<KeyValuePair<string, List<string>>>();
                    this.UpdateReactionsList();
                }

                return this.reactionsList!;
            }
        }

        private void UpdateReactionsList()
        {
            if (this.reactionsList == null)
            {
                this.reactionsList = new ObservableCollection<KeyValuePair<string, List<string>>>();
            }

            this.reactionsList.Clear();
            if (this.reactions != null)
            {
                foreach (var reaction in this.reactions)
                {
                    this.reactionsList.Add(reaction);
                }
            }

            this.OnPropertyChanged(nameof(this.ReactionsList));
            this.OnPropertyChanged(nameof(this.HasReactions));
        }

        /// <summary>
        /// Gets the status display text.
        /// </summary>
        public string StatusText => this.Status switch
        {
            MessageStatus.Sent => "Sent",
            MessageStatus.Delivered => "Delivered",
            MessageStatus.Read => "Read",
            MessageStatus.Failed => "Failed",
            _ => string.Empty,
        };

        /// <summary>
        /// Updates the message from a DTO.
        /// </summary>
        /// <param name="messageDto">The updated message DTO.</param>
        public void UpdateFromDto(MessageDto messageDto)
        {
            this.Content = messageDto.Content;
            this.Status = messageDto.Status;
            this.EditedAt = messageDto.EditedAt;
            this.IsDeleted = messageDto.IsDeleted;

            // Update attachments - clear and add new ones to ensure UI updates properly
            var newAttachmentCount = messageDto.Attachments?.Count ?? 0;
            var oldAttachmentCount = this.attachments.Count;

            // Check if attachments actually changed
            var attachmentsChanged = oldAttachmentCount != newAttachmentCount;
            if (!attachmentsChanged && messageDto.Attachments != null && this.attachments.Count > 0)
            {
                // Check if attachment IDs changed
                var oldIds = this.attachments.Select(a => a.AttachmentDto?.Id ?? string.Empty).OrderBy(id => id).ToList();
                var newIds = messageDto.Attachments.Select(a => a.Id).OrderBy(id => id).ToList();
                attachmentsChanged = !oldIds.SequenceEqual(newIds);
            }

            if (attachmentsChanged)
            {
                this.attachments.Clear();
                if (messageDto.Attachments != null)
                {
                    foreach (var attachment in messageDto.Attachments)
                    {
                        this.attachments.Add(new AttachmentViewModel(attachment, this.fileAttachmentService));
                    }
                }

                // Notify UI that attachments have changed
                this.OnPropertyChanged(nameof(this.HasAttachments));
                this.OnPropertyChanged(nameof(this.HasImageAttachments));
            }

            // Update reactions
            this.Reactions = messageDto.Reactions ?? new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Loads the sender's avatar image asynchronously.
        /// </summary>
        private async void LoadSenderAvatarAsync()
        {
            if (this.avatarService == null)
            {
                return;
            }

            try
            {
                if (string.IsNullOrEmpty(this.senderAvatarUrl))
                {
                    this.SenderAvatarImage = this.avatarService.GetDefaultAvatar();
                    return;
                }

                // Add timestamp to force refresh and bypass cache
                var separator = this.senderAvatarUrl.Contains('?') ? "&" : "?";
                var cacheBustingUrl = $"{this.senderAvatarUrl}{separator}t={DateTime.UtcNow.Ticks}";
                this.SenderAvatarImage = await this.avatarService.LoadAvatarAsync(cacheBustingUrl);
            }
            catch
            {
                // Silently fail and use default avatar
                this.SenderAvatarImage = this.avatarService.GetDefaultAvatar();
            }
        }

        private void NotifyContentDisplayProperties()
        {
            this.OnPropertyChanged(nameof(this.DisplayContent));
            this.OnPropertyChanged(nameof(this.NeedsReadMore));
            this.OnPropertyChanged(nameof(this.ReadMoreButtonText));
        }
    }
}
