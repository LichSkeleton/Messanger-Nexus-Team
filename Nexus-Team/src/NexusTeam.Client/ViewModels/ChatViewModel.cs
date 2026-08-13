namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Input;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Helpers;
    using NexusTeam.Client.Services;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using Serilog;

    /// <summary>
    /// View model for the main chat view with conversation list and messages.
    /// </summary>
    public partial class ChatViewModel : ViewModelBase
    {
        private readonly IMessagingService messagingService;
        private readonly IAuthenticationService authenticationService;
        private readonly IUserDirectoryService userDirectoryService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly IFileAttachmentService fileAttachmentService;
        private readonly IAttachmentPreviewService attachmentPreviewService;
        private readonly IImageCompressionService imageCompressionService;
        private readonly IAvatarService avatarService;
        private readonly INavigationService navigationService;
        private readonly IServiceProvider serviceProvider;
        private readonly ILogger logger;
        private readonly CallViewModel callViewModel;
        private readonly IVoiceMessageService voiceMessageService;
        private readonly HashSet<string> receivedMessageIds;
        private readonly System.Windows.Threading.DispatcherTimer voiceRecordingTimer;
        private ConversationViewModel? selectedConversation;
        private Dictionary<string, UserDto> usersCache;
        private string messageText;
        private bool isLoadingMessages;
        private bool isLoadingConversations;
        private string? errorMessage;
        private MessageViewModel? messageBeingEdited;
        private CancellationTokenSource? typingCancellationTokenSource;
        private CancellationTokenSource? searchCancellationTokenSource;
        private ObservableCollection<AttachmentViewModel> pendingAttachments;
        private AttachmentViewModel? currentlyPlayingAudio;
        private bool isSubscribedToEvents;
        private bool isEmojiPickerOpen;
        private System.Windows.Threading.DispatcherTimer? avatarRefreshTimer;
        private bool isProcessingVoiceMessage;
        private DateTime? voiceRecordingStartedAt;
        private string voiceRecordingDurationText = "00:00";
        private bool isVoiceRecordingActive;

        // --- SEARCH FIELDS ---
        private string chatSearchText;
        private string messageSearchText;
        private bool isSearchingMessages;

        // --- FOLDER FIELDS ---
        private ObservableCollection<ChatFolderViewModel> folders;
        private ChatFolderViewModel? selectedFolder;

        // --- OWN STATUS FIELDS ---
        private UserStatus myStatus = UserStatus.Online;
        private bool isStatusDropdownOpen;
        private bool isUpdatingStatus;

        // --- USER PROFILE MODAL FIELDS ---
        private bool isUserProfileVisible;
        private string profileEmail;
        private string profileUsername;
        private string profileDisplayName;
        private System.Windows.Media.Imaging.BitmapImage? profileAvatarImage;
        private UserStatus profileStatus = UserStatus.Offline;
        private DateTime? profileLastSeenAt;
        private int filesCount;
        private int imagesCount;
        private int videosCount;

        // --- FILES AND IMAGES LIST FIELDS ---
        private ObservableCollection<AttachmentViewModel> filesList;
        private ObservableCollection<AttachmentViewModel> imagesList;

        // --- GROUP CHAT PARTICIPANTS FIELDS ---
        private ObservableCollection<UserDto> groupChatParticipants;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatViewModel"/> class.
        /// </summary>
        /// <param name="messagingService">The messaging service.</param>
        /// <param name="authenticationService">The authentication service.</param>
        /// <param name="userDirectoryService">The user directory service.</param>
        /// <param name="errorHandlingService">The error handling service.</param>
        /// <param name="fileAttachmentService">The file attachment service.</param>
        /// <param name="attachmentPreviewService">The attachment preview service.</param>
        /// <param name="imageCompressionService">The image compression service.</param>
        /// <param name="avatarService">The avatar service.</param>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="serviceProvider">The service provider.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="callViewModel">The call view model.</param>
        /// <param name="voiceMessageService">The voice message service.</param>
        public ChatViewModel(
            IMessagingService messagingService,
            IAuthenticationService authenticationService,
            IUserDirectoryService userDirectoryService,
            IErrorHandlingService errorHandlingService,
            IFileAttachmentService fileAttachmentService,
            IAttachmentPreviewService attachmentPreviewService,
            IImageCompressionService imageCompressionService,
            IAvatarService avatarService,
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ILogger logger,
            CallViewModel callViewModel,
            IVoiceMessageService voiceMessageService)
        {
            this.messagingService = messagingService;
            this.authenticationService = authenticationService;
            this.userDirectoryService = userDirectoryService;
            this.errorHandlingService = errorHandlingService;
            this.fileAttachmentService = fileAttachmentService;
            this.attachmentPreviewService = attachmentPreviewService;
            this.imageCompressionService = imageCompressionService;
            this.avatarService = avatarService;
            this.navigationService = navigationService;
            this.serviceProvider = serviceProvider;
            this.logger = logger;
            this.callViewModel = callViewModel ?? throw new ArgumentNullException(nameof(callViewModel));
            this.voiceMessageService = voiceMessageService ?? throw new ArgumentNullException(nameof(voiceMessageService));
            this.receivedMessageIds = new HashSet<string>();
            this.usersCache = new Dictionary<string, UserDto>();
            this.Title = "Nexus Team - Messages";
            this.messageText = string.Empty;

            // Initialize search fields to avoid CS8618
            this.chatSearchText = string.Empty;
            this.messageSearchText = string.Empty;

            // Initialize profile fields
            this.profileEmail = string.Empty;
            this.profileUsername = string.Empty;
            this.profileDisplayName = string.Empty;

            // Initialize files and images lists
            this.filesList = new ObservableCollection<AttachmentViewModel>();
            this.imagesList = new ObservableCollection<AttachmentViewModel>();

            // Initialize group chat participants
            this.groupChatParticipants = new ObservableCollection<UserDto>();

            this.pendingAttachments = new ObservableCollection<AttachmentViewModel>();
            this.pendingAttachments.CollectionChanged += (s, e) => this.SendMessageCommand.NotifyCanExecuteChanged();

            this.Conversations = new ObservableCollection<ConversationViewModel>();
            this.Messages = new ObservableCollection<IMessageListItem>();
            this.folders = new ObservableCollection<ChatFolderViewModel>();

            // Create "All Chats" folder
            var allChatsFolder = new ChatFolderViewModel(new Shared.Dtos.ChatFolderDto
            {
                Id = "all",
                Name = "All Chats",
                ChatIds = new List<string>(),
            });
            this.folders.Add(allChatsFolder);
            this.selectedFolder = allChatsFolder;

            // --- CHAT SEARCH INITIALIZATION ---
            this.ConversationsView = CollectionViewSource.GetDefaultView(this.Conversations);
            this.ConversationsView.Filter = this.FilterConversations;

            // Subscribe to voice message service events
            this.voiceMessageService.RecordingStopped += this.OnVoiceRecordingStopped;

            this.voiceRecordingTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            this.voiceRecordingTimer.Tick += this.OnVoiceRecordingTimerTick;

            // Event subscription moved to OnNavigatedTo for proper lifecycle management
        }

        /// <summary>
        /// Gets the conversations collection (Source).
        /// </summary>
        public ObservableCollection<ConversationViewModel> Conversations { get; }

        /// <summary>
        /// Gets the filtered view of conversations.
        /// </summary>
        public ICollectionView ConversationsView { get; }

        /// <summary>
        /// Gets the folders collection.
        /// </summary>
        public ObservableCollection<ChatFolderViewModel> Folders
        {
            get => this.folders;
        }

        /// <summary>
        /// Gets or sets the selected folder.
        /// </summary>
        public ChatFolderViewModel? SelectedFolder
        {
            get => this.selectedFolder;
            set
            {
                if (this.SetProperty(ref this.selectedFolder, value))
                {
                    // Update folder selection state
                    foreach (var folder in this.Folders)
                    {
                        folder.IsSelected = folder == value;
                    }

                    // Refresh conversations view to apply folder filter
                    this.ConversationsView.Refresh();
                }
            }
        }

        /// <summary>
        /// Gets or sets the current user's Online/Invisible status.
        /// </summary>
        public UserStatus MyStatus
        {
            get => this.myStatus;
            set
            {
                if (this.SetProperty(ref this.myStatus, value))
                {
                    this.OnPropertyChanged(nameof(this.StatusLabel));
                }
            }
        }

        /// <summary>
        /// Gets the display label for the current user's status.
        /// </summary>
        public string StatusLabel => this.myStatus == UserStatus.Invisible ? "Invisible" : "Online";

        /// <summary>
        /// Gets or sets a value indicating whether the status dropdown is open.
        /// </summary>
        public bool IsStatusDropdownOpen
        {
            get => this.isStatusDropdownOpen;
            set => this.SetProperty(ref this.isStatusDropdownOpen, value);
        }

        /// <summary>
        /// Gets the messages collection for the selected conversation.
        /// Contains both messages and date separators.
        /// </summary>
        public ObservableCollection<IMessageListItem> Messages { get; }

        /// <summary>
        /// Gets the pending attachments collection to be sent with next message.
        /// </summary>
        public ObservableCollection<AttachmentViewModel> PendingAttachments
        {
            get => this.pendingAttachments;
        }

        /// <summary>
        /// Gets or sets the selected conversation.
        /// </summary>
        public ConversationViewModel? SelectedConversation
        {
            get => this.selectedConversation;
            set
            {
                if (this.SetProperty(ref this.selectedConversation, value))
                {
                    // Clear search when changing chats
                    this.MessageSearchText = string.Empty;
                    _ = this.LoadMessagesForSelectedConversationAsync();
                    this.SendMessageCommand.NotifyCanExecuteChanged();
                    this.StartCallCommand.NotifyCanExecuteChanged();
                    this.StartVoiceRecordingCommand.NotifyCanExecuteChanged();
                    this.StopVoiceRecordingCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the message text being composed.
        /// </summary>
        public string MessageText
        {
            get => this.messageText;
            set
            {
                if (this.SetProperty(ref this.messageText, value))
                {
                    _ = this.SendTypingIndicatorAsync();
                    this.SendMessageCommand.NotifyCanExecuteChanged();  // ✅ ДОБАВЛЕНО
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether messages are being loaded.
        /// </summary>
        public bool IsLoadingMessages
        {
            get => this.isLoadingMessages;
            set
            {
                if (this.SetProperty(ref this.isLoadingMessages, value))
                {
                    this.SendMessageCommand.NotifyCanExecuteChanged();  // ✅ ДОБАВЛЕНО
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether conversations are being loaded.
        /// </summary>
        public bool IsLoadingConversations
        {
            get => this.isLoadingConversations;
            set => this.SetProperty(ref this.isLoadingConversations, value);
        }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string? ErrorMessage
        {
            get => this.errorMessage;
            set => this.SetProperty(ref this.errorMessage, value);
        }

        /// <summary>
        /// Gets or sets the message being edited.
        /// </summary>
        public MessageViewModel? MessageBeingEdited
        {
            get => this.messageBeingEdited;
            set => this.SetProperty(ref this.messageBeingEdited, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the user profile modal is visible.
        /// </summary>
        public bool IsUserProfileVisible
        {
            get => this.isUserProfileVisible;
            set => this.SetProperty(ref this.isUserProfileVisible, value);
        }

        /// <summary>
        /// Gets or sets the profile email of the selected conversation user.
        /// </summary>
        public string ProfileEmail
        {
            get => this.profileEmail;
            set => this.SetProperty(ref this.profileEmail, value);
        }

        /// <summary>
        /// Gets or sets the profile username of the selected conversation user.
        /// </summary>
        public string ProfileUsername
        {
            get => this.profileUsername;
            set => this.SetProperty(ref this.profileUsername, value);
        }

        /// <summary>
        /// Gets or sets the profile display name of the selected conversation user.
        /// </summary>
        public string ProfileDisplayName
        {
            get => this.profileDisplayName;
            set => this.SetProperty(ref this.profileDisplayName, value);
        }

        /// <summary>
        /// Gets or sets the profile avatar image.
        /// </summary>
        public System.Windows.Media.Imaging.BitmapImage? ProfileAvatarImage
        {
            get => this.profileAvatarImage;
            set => this.SetProperty(ref this.profileAvatarImage, value);
        }

        /// <summary>
        /// Gets or sets the profile status.
        /// </summary>
        public UserStatus ProfileStatus
        {
            get => this.profileStatus;
            set => this.SetProperty(ref this.profileStatus, value);
        }

        /// <summary>
        /// Gets or sets the profile last seen at.
        /// </summary>
        public DateTime? ProfileLastSeenAt
        {
            get => this.profileLastSeenAt;
            set => this.SetProperty(ref this.profileLastSeenAt, value);
        }

        /// <summary>
        /// Gets the list of participants for group chats.
        /// </summary>
        public ObservableCollection<UserDto> GroupChatParticipants
        {
            get => this.groupChatParticipants;
            private set => this.SetProperty(ref this.groupChatParticipants, value);
        }

        /// <summary>
        /// Gets the formatted profile status text.
        /// </summary>
        public string ProfileStatusText
        {
            get
            {
                if (this.profileStatus == UserStatus.Online)
                {
                    return "Online";
                }

                if (this.ProfileLastSeenAt.HasValue)
                {
                    var now = DateTime.UtcNow;
                    var diff = now - this.ProfileLastSeenAt.Value;

                    if (diff.TotalMinutes < 1)
                    {
                        return "last seen just now";
                    }

                    if (diff.TotalHours < 1)
                    {
                        return $"last seen {(int)diff.TotalMinutes}m ago";
                    }

                    if (diff.TotalDays < 1)
                    {
                        return $"last seen {(int)diff.TotalHours}h ago";
                    }

                    if (diff.TotalDays < 7)
                    {
                        return $"last seen {(int)diff.TotalDays}d ago";
                    }

                    return $"last seen {this.ProfileLastSeenAt.Value:MMM dd}";
                }

                return "Offline";
            }
        }

        /// <summary>
        /// Gets or sets the number of files in the current chat.
        /// </summary>
        public int FilesCount
        {
            get => this.filesCount;
            set => this.SetProperty(ref this.filesCount, value);
        }

        /// <summary>
        /// Gets or sets the number of images in the current chat.
        /// </summary>
        public int ImagesCount
        {
            get => this.imagesCount;
            set => this.SetProperty(ref this.imagesCount, value);
        }

        /// <summary>
        /// Gets or sets the number of videos in the current chat.
        /// </summary>
        public int VideosCount
        {
            get => this.videosCount;
            set => this.SetProperty(ref this.videosCount, value);
        }

        /// <summary>
        /// Gets the files list collection.
        /// </summary>
        public ObservableCollection<AttachmentViewModel> FilesList => this.filesList;

        /// <summary>
        /// Gets the images list collection.
        /// </summary>
        public ObservableCollection<AttachmentViewModel> ImagesList => this.imagesList;

        /// <summary>
        /// Gets or sets a value indicating whether the emoji picker is open.
        /// </summary>
        public bool IsEmojiPickerOpen
        {
            get => this.isEmojiPickerOpen;
            set => this.SetProperty(ref this.isEmojiPickerOpen, value);
        }

        /// <summary>
        /// Gets a value indicating whether voice recording is active.
        /// </summary>
        public bool IsVoiceRecordingActive
        {
            get => this.isVoiceRecordingActive;
            private set => this.SetProperty(ref this.isVoiceRecordingActive, value);
        }

        /// <summary>
        /// Gets the formatted duration of the current voice recording.
        /// </summary>
        public string VoiceRecordingDurationText
        {
            get => this.voiceRecordingDurationText;
            private set => this.SetProperty(ref this.voiceRecordingDurationText, value);
        }

        /// <summary>
        /// Gets or sets the chat search text.
        /// </summary>
        public string ChatSearchText
        {
            get => this.chatSearchText;
            set
            {
                if (this.SetProperty(ref this.chatSearchText, value))
                {
                    this.ConversationsView.Refresh();
                }
            }
        }

        /// <summary>
        /// Gets or sets the message search text.
        /// </summary>
        public string MessageSearchText
        {
            get => this.messageSearchText;
            set
            {
                if (this.SetProperty(ref this.messageSearchText, value))
                {
                    this.ClearMessageSearchCommand.NotifyCanExecuteChanged();
                    this.SearchMessagesInChatCommand.NotifyCanExecuteChanged();
                    _ = this.TriggerSearchAsYouTypeAsync();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether messages are being searched.
        /// </summary>
        public bool IsSearchingMessages
        {
            get => this.isSearchingMessages;
            set => this.SetProperty(ref this.isSearchingMessages, value);
        }

        /// <inheritdoc/>
        public override void OnNavigatedTo()
        {
            this.SubscribeToMessagingEvents();
            this.StartAvatarRefreshTimer();
            _ = this.InitializeAsync();
        }

        /// <inheritdoc/>
        public override void OnNavigatedFrom()
        {
            this.StopAvatarRefreshTimer();
            this.UnsubscribeFromMessagingEvents();
        }

        /// <summary>
        /// Saves an image attachment to a custom location.
        /// </summary>
        /// <param name="attachmentDto">The attachment DTO to save.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveImageAsAsync(Shared.Dtos.MessageAttachmentDto attachmentDto)
        {
            if (attachmentDto == null)
            {
                return;
            }

            try
            {
                await this.fileAttachmentService.SaveAttachmentAsAsync(attachmentDto);
                this.logger.Information("Image saved: {FileName}", attachmentDto.FileName);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save image: {FileName}", attachmentDto.FileName);
                this.errorHandlingService.ShowError($"Failed to save image: {ex.Message}");
            }
        }

        /// <summary>
        /// Command to send a message.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSendMessage))]
        private async Task SendMessageAsync()
        {
            if (this.SelectedConversation == null)
            {
                return;
            }

            // Allow sending if we have text OR attachments
            if (string.IsNullOrWhiteSpace(this.MessageText) && this.PendingAttachments.Count == 0)
            {
                return;
            }

            try
            {
                if (this.MessageBeingEdited != null)
                {
                    // Editing - attachments not supported for now
                    await this.messagingService.EditMessageAsync(
                        this.MessageBeingEdited.Id,
                        this.MessageText.Trim());

                    this.MessageBeingEdited.IsEditing = false;
                    this.MessageBeingEdited = null;
                }
                else
                {
                    // Prepare message content - if empty but has attachments, use a placeholder
                    var messageContent = this.MessageText?.Trim() ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(messageContent) && this.PendingAttachments.Count > 0)
                    {
                        messageContent = " "; // Single space as placeholder for attachments-only message
                    }

                    // First, send the message to get the real messageId (use HTTP for attachments)
                    var messageDto = await this.messagingService.SendMessageViaHttpAsync(
                        this.SelectedConversation.Id,
                        messageContent,
                        replyToId: null,
                        attachmentIds: new List<string>());

                    // Message will be added to UI via WebSocket MessageReceived event
                    // No optimistic UI update to prevent duplicates

                    // Now upload attachments with the real message ID
                    if (this.PendingAttachments.Count > 0)
                    {
                        foreach (var attachment in this.PendingAttachments.ToList())
                        {
                            try
                            {
                                attachment.IsUploading = true;

                                Stream? contentStream = null;
                                bool shouldCompress = attachment.IsImage && attachment.IsCompressed && attachment.CanCompress;

                                this.logger.Information(
                                    "Uploading attachment: {FileName}, IsImage: {IsImage}, IsCompressed: {IsCompressed}, CanCompress: {CanCompress}, ShouldCompress: {ShouldCompress}",
                                    attachment.FileName,
                                    attachment.IsImage,
                                    attachment.IsCompressed,
                                    attachment.CanCompress,
                                    shouldCompress);

                                if (shouldCompress)
                                {
                                    try
                                    {
                                        contentStream = await this.imageCompressionService.CompressImageAsync(attachment.FilePath);
                                        if (contentStream != null && contentStream.CanSeek)
                                        {
                                            this.logger.Information(
                                                "Image compressed successfully: {FileName}, Compressed stream length: {Length}",
                                                attachment.FileName,
                                                contentStream.Length);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        // Compression failed or didn't reduce size - use original file
                                        this.logger.Warning(
                                            ex,
                                            "Compression skipped for {FileName}, using original file. Reason: {Reason}",
                                            attachment.FileName,
                                            ex.Message);
                                        contentStream = null; // Will use original file
                                    }
                                }
                                else
                                {
                                    this.logger.Information("Skipping compression for {FileName}, using original file", attachment.FileName);
                                }

                                await this.fileAttachmentService.UploadAttachmentAsync(
                                    attachment.FilePath,
                                    messageDto.Id,
                                    contentStream);

                                if (contentStream != null)
                                {
                                    await ((IAsyncDisposable)contentStream).DisposeAsync();
                                }

                                this.logger.Information("Uploaded attachment: {FileName}", attachment.FileName);
                            }
                            catch (Exception ex)
                            {
                                this.logger.Error(ex, "Failed to upload attachment: {FileName}", attachment.FileName);
                                this.errorHandlingService.ShowError($"Failed to upload {attachment.FileName}");
                                attachment.IsUploading = false;
                            }
                        }

                        // Clear pending attachments after upload attempts
                        this.PendingAttachments.Clear();

                        // Attachments will be updated automatically via WebSocket EditMessage event
                        // No need to reload messages manually
                        this.logger.Information("All attachments uploaded, waiting for WebSocket update");
                    }
                }

                this.MessageText = string.Empty;
                this.ErrorMessage = null;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to send message");
                this.ErrorMessage = "Failed to send message. Please try again.";
            }
        }

        /// <summary>
        /// Command to edit a message.
        /// </summary>
        /// <param name="message">The message to edit.</param>
        [RelayCommand]
        private void EditMessage(MessageViewModel message)
        {
            if (message.IsCurrentUser && !message.IsDeleted)
            {
                this.MessageBeingEdited = message;
                this.MessageText = message.Content;
                message.IsEditing = true;
            }
        }

        /// <summary>
        /// Command to cancel editing.
        /// </summary>
        [RelayCommand]
        private void CancelEdit()
        {
            if (this.MessageBeingEdited != null)
            {
                this.MessageBeingEdited.IsEditing = false;
                this.MessageBeingEdited = null;
            }

            this.MessageText = string.Empty;
        }

        /// <summary>
        /// Command to copy a message to clipboard.
        /// </summary>
        /// <param name="message">The message to copy.</param>
        [RelayCommand]
        private void CopyMessage(MessageViewModel message)
        {
            if (message == null || message.IsDeleted)
            {
                return;
            }

            try
            {
                Clipboard.SetText(message.Content);
                this.logger.Information("Message copied to clipboard: {MessageId}", message.Id);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to copy message to clipboard");
                this.errorHandlingService.ShowError("Failed to copy message to clipboard.");
            }
        }

        /// <summary>
        /// Command to attach files to message.
        /// </summary>
        [RelayCommand]
        private async Task AttachFileAsync()
        {
            try
            {
                var files = await this.fileAttachmentService.PickFilesAsync(multiSelect: true);

                foreach (var filePath in files)
                {
                    // Validate file
                    var validation = this.fileAttachmentService.ValidateFile(filePath);
                    if (!validation.IsValid)
                    {
                        this.errorHandlingService.ShowWarning($"{System.IO.Path.GetFileName(filePath)}: {validation.ErrorMessage}");
                        continue;
                    }

                    // Add to pending attachments
                    var attachmentVm = new AttachmentViewModel(filePath);
                    this.PendingAttachments.Add(attachmentVm);

                    this.logger.Information("File attached: {FileName} ({Size})", attachmentVm.FileName, attachmentVm.FileSizeFormatted);
                }

                this.SendMessageCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to attach files");
                this.errorHandlingService.ShowError("Failed to attach files. Please try again.");
            }
        }

        /// <summary>
        /// Command to start recording a voice message.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartVoiceRecording))]
        private void StartVoiceRecording()
        {
            if (this.SelectedConversation == null)
            {
                this.logger.Warning("Cannot start voice recording: no conversation selected");
                return;
            }

            try
            {
                if (this.voiceMessageService.IsRecording)
                {
                    this.logger.Warning("Voice recording already in progress");
                    return;
                }

                this.voiceMessageService.StartRecording();
                this.logger.Information("Voice message recording started");

                this.voiceRecordingStartedAt = DateTime.UtcNow;
                this.VoiceRecordingDurationText = "00:00";
                this.IsVoiceRecordingActive = true;
                this.voiceRecordingTimer.Start();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to start voice recording");
                this.errorHandlingService.ShowError("Failed to start voice recording. Please check your microphone permissions.");
            }
        }

        /// <summary>
        /// Determines if voice recording can be started.
        /// </summary>
        /// <returns>True if recording can be started, false otherwise.</returns>
        private bool CanStartVoiceRecording()
        {
            return this.SelectedConversation != null && !this.voiceMessageService.IsRecording;
        }

        /// <summary>
        /// Command to stop recording a voice message.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStopVoiceRecording))]
        private void StopVoiceRecording()
        {
            try
            {
                if (!this.voiceMessageService.IsRecording)
                {
                    this.logger.Warning("No voice recording in progress");
                    return;
                }

                this.voiceMessageService.StopRecording();
                this.logger.Information("Voice message recording stopped");

                this.voiceRecordingTimer.Stop();
                this.IsVoiceRecordingActive = false;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to stop voice recording");
                this.errorHandlingService.ShowError("Failed to stop voice recording.");
            }
        }

        /// <summary>
        /// Determines if voice recording can be stopped.
        /// </summary>
        /// <returns>True if recording can be stopped, false otherwise.</returns>
        private bool CanStopVoiceRecording()
        {
            return this.voiceMessageService.IsRecording;
        }

        /// <summary>
        /// Handles voice recording stopped event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="filePath">The path to the recorded audio file.</param>
        private async void OnVoiceRecordingStopped(object? sender, string filePath)
        {
            if (this.isProcessingVoiceMessage)
            {
                this.logger.Warning("Voice recording is already being processed, skipping duplicate.");
                return;
            }

            this.isProcessingVoiceMessage = true;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                this.logger.Warning("Voice recording file is empty or does not exist");
                this.isProcessingVoiceMessage = false;
                this.voiceRecordingTimer.Stop();
                this.IsVoiceRecordingActive = false;
                return;
            }

            try
            {
                // Add the recorded file as an attachment
                var attachmentVm = new AttachmentViewModel(filePath);
                this.PendingAttachments.Add(attachmentVm);

                this.logger.Information("Voice message recorded and added as attachment: {FileName}", attachmentVm.FileName);

                // Automatically send the message with the voice attachment
                await this.SendMessageAsync();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to process recorded voice message");
                this.errorHandlingService.ShowError("Failed to process voice message. Please try again.");

                // Clean up the file if it exists
                try
                {
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
            finally
            {
                this.isProcessingVoiceMessage = false;
            }
        }

        private void OnVoiceRecordingTimerTick(object? sender, EventArgs e)
        {
            if (!this.IsVoiceRecordingActive || this.voiceRecordingStartedAt == null)
            {
                this.VoiceRecordingDurationText = "00:00";
                return;
            }

            var elapsed = DateTime.UtcNow - this.voiceRecordingStartedAt.Value;
            this.VoiceRecordingDurationText = $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        }

        /// <summary>
        /// Command to remove a pending attachment.
        /// </summary>
        /// <param name="attachment">The attachment to remove.</param>
        [RelayCommand]
        private void RemoveAttachment(AttachmentViewModel attachment)
        {
            this.PendingAttachments.Remove(attachment);
            this.logger.Information("Removed pending attachment: {FileName}", attachment.FileName);
            this.SendMessageCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Views an image attachment in the application.
        /// </summary>
        /// <param name="attachment">The attachment to view.</param>
        [RelayCommand]
        private void ViewImage(AttachmentViewModel attachment)
        {
            if (attachment.AttachmentDto == null || !attachment.IsImage)
            {
                return;
            }

            try
            {
                // Collect all image attachments from all messages in the current conversation
                var allImages = new List<Shared.Dtos.MessageAttachmentDto>();
                foreach (var message in this.GetMessageViewModels())
                {
                    foreach (var msgAttachment in message.Attachments)
                    {
                        if (msgAttachment.IsImage && msgAttachment.AttachmentDto != null)
                        {
                            allImages.Add(msgAttachment.AttachmentDto);
                        }
                    }
                }

                // Find current image index
                var currentIndex = allImages.FindIndex(img => img.Id == attachment.AttachmentDto.Id);
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }

                var imageViewModel = new ImageViewModel(allImages, currentIndex, this.fileAttachmentService);
                var dialog = new Views.ImageViewerDialog(imageViewModel)
                {
                    Owner = Application.Current.MainWindow,
                };

                dialog.ShowDialog();
                this.logger.Information("Viewed image: {FileName}", attachment.FileName);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to view image: {FileName}", attachment.FileName);
                this.errorHandlingService.ShowError($"Failed to view image: {ex.Message}");
            }
        }

        /// <summary>
        /// Opens a non-image attachment with the default system application.
        /// </summary>
        /// <param name="attachment">The attachment to open.</param>
        [RelayCommand]
        private async Task OpenAttachment(AttachmentViewModel attachment)
        {
            if (attachment.AttachmentDto == null || attachment.IsImage)
            {
                return; // Images should use ViewImageCommand
            }

            try
            {
                await this.fileAttachmentService.OpenAttachmentAsync(attachment.AttachmentDto);
                this.logger.Information("Opened attachment: {FileName}", attachment.FileName);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to open attachment: {FileName}", attachment.FileName);
                this.errorHandlingService.ShowError($"Failed to open file: {ex.Message}");
            }
        }

        /// <summary>
        /// Command to play or pause audio attachment.
        /// </summary>
        /// <param name="attachment">The audio attachment to play/pause.</param>
        [RelayCommand]
        private void PlayPauseAudio(AttachmentViewModel attachment)
        {
            if (attachment == null || !attachment.IsAudio || attachment.AttachmentDto == null)
            {
                return;
            }

            // If this is the currently playing audio, toggle pause
            if (this.currentlyPlayingAudio == attachment)
            {
                // Toggle pause/play
                this.OnAudioPlayPauseRequested?.Invoke(attachment);
                return;
            }

            // Stop currently playing audio if any
            if (this.currentlyPlayingAudio != null)
            {
                this.currentlyPlayingAudio.IsPlaying = false;
                this.OnAudioStopRequested?.Invoke(this.currentlyPlayingAudio);
            }

            // Start playing new audio
            this.currentlyPlayingAudio = attachment;
            attachment.IsPlaying = true;
            this.OnAudioPlayRequested?.Invoke(attachment, attachment.AttachmentDto.DownloadUrl);
        }

        /// <summary>
        /// Event raised when audio play is requested.
        /// </summary>
        public event Action<AttachmentViewModel, string>? OnAudioPlayRequested;

        /// <summary>
        /// Event raised when audio play/pause toggle is requested.
        /// </summary>
        public event Action<AttachmentViewModel>? OnAudioPlayPauseRequested;

        /// <summary>
        /// Event raised when audio stop is requested.
        /// </summary>
        public event Action<AttachmentViewModel>? OnAudioStopRequested;

        /// <summary>
        /// Downloads an attachment to a temporary file for playback.
        /// </summary>
        /// <param name="attachment">The attachment DTO.</param>
        /// <returns>The local file path.</returns>
        public async Task<string> DownloadAttachmentForPlaybackAsync(MessageAttachmentDto attachment)
        {
            // Download to temp directory instead of Downloads
            var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nexusteam_audio_{Guid.NewGuid()}{System.IO.Path.GetExtension(attachment.FileName)}");

            try
            {
                // Use DownloadImageStreamAsync pattern - it works for any file type
                var stream = await this.fileAttachmentService.DownloadImageStreamAsync(attachment);

                using (var fileStream = System.IO.File.Create(tempPath))
                using (stream)
                {
                    await stream.CopyToAsync(fileStream);
                }

                return tempPath;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download audio for playback: {AttachmentId}", attachment.Id);
                throw;
            }
        }

        /// <summary>
        /// Downloads an attachment to local storage.
        /// </summary>
        /// <param name="attachment">The attachment to download.</param>
        [RelayCommand]
        private async Task DownloadAttachment(AttachmentViewModel attachment)
        {
            if (attachment.AttachmentDto == null)
            {
                this.errorHandlingService.ShowError("Attachment information is not available.");
                return;
            }

            try
            {
                var localPath = await this.fileAttachmentService.DownloadAttachmentAsync(attachment.AttachmentDto);
                this.logger.Information("Downloaded attachment: {FileName} to {Path}", attachment.FileName, localPath);
                this.errorHandlingService.ShowInfo($"File downloaded to: {localPath}");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to download attachment: {FileName}", attachment.FileName);
                this.errorHandlingService.ShowError($"Failed to download file: {ex.Message}");
            }
        }

        /// <summary>
        /// Command to delete a message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        [RelayCommand]
        private async Task DeleteMessageAsync(MessageViewModel message)
        {
            if (!message.IsCurrentUser || message.IsDeleted)
            {
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to delete this message?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await this.messagingService.DeleteMessageAsync(message.Id);

                    // Optimistically remove message from UI immediately
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var messageToRemove = this.FindMessageViewModel(message.Id);
                        if (messageToRemove != null)
                        {
                            this.Messages.Remove(messageToRemove);
                            this.receivedMessageIds.Remove(message.Id);

                            // Clean up orphaned date separators
                            this.CleanupOrphanedSeparators();

                            this.logger.Information("Message removed from UI immediately: {MessageId}", message.Id);
                        }
                    });

                    this.ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Failed to delete message");
                    this.ErrorMessage = "Failed to delete message. Please try again.";
                }
            }
        }

        /// <summary>
        /// Command to preview a supported attachment before downloading.
        /// </summary>
        [RelayCommand]
        private async Task PreviewAttachmentAsync(AttachmentViewModel attachment)
        {
            if (attachment.AttachmentDto == null)
            {
                return;
            }

            try
            {
                await this.attachmentPreviewService.PreviewAsync(
                    attachment,
                    this.SelectedConversation?.Id,
                    Application.Current?.MainWindow);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to preview attachment: {FileName}", attachment.FileName);
                this.errorHandlingService.ShowError($"Failed to preview file: {ex.Message}");
            }
        }

        /// <summary>
        /// Command to search messages in the current chat.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSearchMessages))]
        private async Task SearchMessagesInChatAsync()
        {
            if (this.SelectedConversation == null || string.IsNullOrWhiteSpace(this.MessageSearchText))
            {
                return;
            }

            this.IsSearchingMessages = true;
            this.ErrorMessage = null;

            try
            {
                var results = await this.messagingService.SearchMessagesAsync(
                    this.SelectedConversation.Id,
                    this.MessageSearchText);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    this.Messages.Clear();
                    this.receivedMessageIds.Clear();

                    var currentUserId = this.authenticationService.CurrentUser?.Id ?? string.Empty;

                    foreach (var msgDto in results)
                    {
                        // Get sender info from selected conversation if available
                        string? senderName = null;
                        string? senderAvatarUrl = null;
                        var isGroupChat = this.SelectedConversation?.Type == ChatType.Group || this.SelectedConversation?.Type == ChatType.Channel;

                        if (this.SelectedConversation != null)
                        {
                            // For group chats, get from participants list for other users only
                            if (isGroupChat && msgDto.SenderId != currentUserId)
                            {
                                senderName = this.SelectedConversation.GetParticipantName(msgDto.SenderId);
                                senderAvatarUrl = this.SelectedConversation.GetParticipantAvatarUrl(msgDto.SenderId);
                            }

                            // For direct messages and own messages in group chats, don't set name (won't be displayed)
                        }

                        var vm = new MessageViewModel(
                            msgDto,
                            currentUserId,
                            this.fileAttachmentService,
                            this.avatarService,
                            senderName,
                            senderAvatarUrl,
                            isGroupChat);
                        this.Messages.Add(vm);
                    }
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to search messages");
                this.ErrorMessage = "Search failed. Please try again.";
            }
            finally
            {
                this.IsSearchingMessages = false;
            }
        }

        /// <summary>
        /// Command to clear message search.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanClearSearch))]
        private async Task ClearMessageSearchAsync()
        {
            this.MessageSearchText = string.Empty;
            await this.LoadMessagesForSelectedConversationAsync();
        }

        /// <summary>
        /// Determines if search can be executed.
        /// </summary>
        private bool CanSearchMessages()
        {
            return this.SelectedConversation != null && !string.IsNullOrWhiteSpace(this.MessageSearchText);
        }

        /// <summary>
        /// Determines if search can be cleared.
        /// </summary>
        private bool CanClearSearch()
        {
            return !string.IsNullOrWhiteSpace(this.MessageSearchText);
        }

        // --- USER PROFILE MODAL COMMANDS ---
        [RelayCommand]
        private void OpenUserProfile()
        {
            if (this.SelectedConversation != null)
            {
                if (!this.IsUserProfileVisible)
                {
                    // Update status from selected conversation first
                    this.ProfileStatus = this.SelectedConversation.Status;
                    this.UpdateProfileData();
                    this.CalculateProfileStatistics();
                    this.PopulateProfileImages();
                    this.PopulateProfileFiles();
                    this.IsUserProfileVisible = true;

                    // Log for debugging
                    this.logger.Debug(
                        "Opened profile for chat {ChatId}, Type: {ChatType}, Participants count: {Count}",
                        this.SelectedConversation.Id,
                        this.SelectedConversation.Type,
                        this.GroupChatParticipants.Count);
                }
                else
                {
                    this.IsUserProfileVisible = false;
                }
            }
        }

        [RelayCommand]
        private void CloseUserProfile()
        {
            this.IsUserProfileVisible = false;
        }

        /// <summary>
        /// Opens the user profile modal for a user from a message.
        /// </summary>
        /// <param name="message">The message view model containing the sender information.</param>
        [RelayCommand]
        private async Task OpenUserProfileFromMessageAsync(object? message)
        {
            if (message is not MessageViewModel messageViewModel)
            {
                return;
            }

            // Don't open profile for current user's messages
            if (messageViewModel.IsCurrentUser)
            {
                return;
            }

            try
            {
                var senderId = messageViewModel.SenderId;
                if (string.IsNullOrEmpty(senderId))
                {
                    return;
                }

                // Try to find user in current chat participants first
                UserDto? user = null;
                if (this.SelectedConversation != null)
                {
                    var chats = await this.messagingService.GetChatsAsync();
                    var chat = chats.FirstOrDefault(c => c.Id == this.SelectedConversation.Id);
                    user = chat?.Participants?.FirstOrDefault(p => p.Id == senderId);
                }

                // If not found in current chat, try to get from available users
                if (user == null)
                {
                    var availableUsers = await this.userDirectoryService.GetAvailableUsersAsync();
                    user = availableUsers.FirstOrDefault(u => u.Id == senderId);
                }

                if (user != null)
                {
                    // Update profile data for this user
                    this.ProfileEmail = user.Email;
                    this.ProfileUsername = user.Username;
                    this.ProfileDisplayName = !string.IsNullOrWhiteSpace(user.DisplayName) ? user.DisplayName : user.Username;
                    this.ProfileStatus = user.Status;
                    this.ProfileLastSeenAt = user.LastSeenAt;
                    this.OnPropertyChanged(nameof(this.ProfileStatusText));

                    // Load avatar
                    _ = this.LoadProfileAvatarAsync(user.AvatarUrl);

                    // Calculate statistics and populate files/images from current chat
                    this.CalculateProfileStatistics();
                    this.PopulateProfileImages();
                    this.PopulateProfileFiles();

                    // Open the profile modal
                    this.IsUserProfileVisible = true;
                }
                else
                {
                    this.logger.Warning("User not found for profile: {SenderId}", senderId);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to open user profile from message");
                this.errorHandlingService.ShowError("Failed to open user profile.");
            }
        }

        /// <summary>
        /// Gets a value indicating whether a call can be started.
        /// </summary>
        /// <returns>True if a call can be started; otherwise, false.</returns>
        private bool CanStartCall()
        {
            return this.SelectedConversation != null
                && !string.IsNullOrEmpty(this.SelectedConversation.OtherUserId);
        }

        /// <summary>
        /// Gets the command to start a call with the selected user.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStartCall))]
        private async Task StartCallAsync()
        {
            this.logger.Information("StartCallCommand executed");

            if (this.SelectedConversation == null)
            {
                this.logger.Warning("Cannot start call: no conversation selected");
                return;
            }

            var otherUserId = this.SelectedConversation.OtherUserId;
            if (string.IsNullOrEmpty(otherUserId))
            {
                this.logger.Warning("Cannot start call: other user ID is null or empty");
                this.errorHandlingService.ShowWarning("Cannot start call: user information not available");
                return;
            }

            this.logger.Information("Starting call to user: {UserId}, ChatId: {ChatId}", otherUserId, this.SelectedConversation.Id);

            // Close user profile overlay when starting a call
            this.IsUserProfileVisible = false;

            try
            {
                await this.callViewModel.StartCallAsync(otherUserId, this.SelectedConversation.Id);
                this.logger.Information("Call started successfully to user: {UserId}", otherUserId);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to start call to user: {UserId}", otherUserId);
                this.errorHandlingService.ShowError("Failed to start call. Please try again.");
            }
        }

        /// <summary>
        /// Gets the call view model.
        /// </summary>
        public CallViewModel CallViewModel => this.callViewModel;

        // --- END USER PROFILE MODAL ---

        // --- FILES AND IMAGES LIST COMMANDS ---
        [RelayCommand]
        private async Task OpenFilesListAsync()
        {
            if (this.SelectedConversation == null)
            {
                return;
            }

            this.logger.Debug("Opening files list for chat {ChatId}", this.SelectedConversation.Id);

            // Load all messages to get all files
            await this.LoadAllMessagesForAttachmentsAsync();

            // Populate files list on UI thread after messages are loaded
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.PopulateFilesList();
                this.logger.Debug("Files list populated with {Count} items", this.filesList.Count);
            });

            // Store data before navigation
            var filesToPass = new ObservableCollection<AttachmentViewModel>(this.filesList);
            var conversationNameToPass = this.SelectedConversation.Name ?? "Chat";

            // Navigate first
            this.navigationService.NavigateTo<FilesListViewModel>();

            // Set data on the ViewModel instance after View is loaded
            // Use InvokeAsync with lower priority to ensure View is created
            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    if (this.navigationService.CurrentViewModel is FilesListViewModel filesListViewModel)
                    {
                        filesListViewModel.SetFiles(filesToPass, conversationNameToPass);
                        this.logger.Debug("Set files on FilesListViewModel: {Count} files", filesToPass.Count);
                    }
                    else
                    {
                        this.logger.Warning("CurrentViewModel is not FilesListViewModel after navigation. Type: {Type}", this.navigationService.CurrentViewModel?.GetType().Name);
                    }
                },
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        [RelayCommand]
        private async Task OpenImagesGridAsync()
        {
            if (this.SelectedConversation == null)
            {
                return;
            }

            this.logger.Debug("Opening images grid for chat {ChatId}", this.SelectedConversation.Id);

            // Load all messages to get all images
            await this.LoadAllMessagesForAttachmentsAsync();

            // Populate images list on UI thread after messages are loaded
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                this.PopulateImagesList();
                this.logger.Debug("Images list populated with {Count} items", this.imagesList.Count);
            });

            // Store data before navigation
            var imagesToPass = new ObservableCollection<AttachmentViewModel>(this.imagesList);
            var conversationNameToPass = this.SelectedConversation.Name ?? "Chat";

            // Navigate first
            this.navigationService.NavigateTo<ImagesGridViewModel>();

            // Set data on the ViewModel instance after View is loaded
            // Use InvokeAsync with lower priority to ensure View is created
            await Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    if (this.navigationService.CurrentViewModel is ImagesGridViewModel imagesGridViewModel)
                    {
                        imagesGridViewModel.SetImages(imagesToPass, conversationNameToPass);
                        this.logger.Debug("Set images on ImagesGridViewModel: {Count} images", imagesToPass.Count);
                    }
                    else
                    {
                        this.logger.Warning("CurrentViewModel is not ImagesGridViewModel after navigation. Type: {Type}", this.navigationService.CurrentViewModel?.GetType().Name);
                    }
                },
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        // --- END FILES AND IMAGES LIST ---

        /// <summary>
        /// Triggers search as user types with debounce.
        /// </summary>
        private async Task TriggerSearchAsYouTypeAsync()
        {
            this.searchCancellationTokenSource?.Cancel();
            this.searchCancellationTokenSource = new CancellationTokenSource();
            var token = this.searchCancellationTokenSource.Token;

            try
            {
                await Task.Delay(300, token);

                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (string.IsNullOrWhiteSpace(this.MessageSearchText))
                {
                    await this.LoadMessagesForSelectedConversationAsync();
                }
                else
                {
                    await this.SearchMessagesInChatAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // Ignored - normal when user types quickly
            }
        }

        /// <summary>
        /// Filters conversations based on chat search text and selected folder.
        /// </summary>
        /// <param name="item">The conversation item to filter.</param>
        /// <returns>True if the conversation matches the search criteria, false otherwise.</returns>
        private bool FilterConversations(object item)
        {
            if (item is not ConversationViewModel chat)
            {
                return false;
            }

            // Filter by folder
            if (this.SelectedFolder != null && !this.SelectedFolder.IsAllChatsFolder)
            {
                if (!this.SelectedFolder.ChatIds.Contains(chat.Id))
                {
                    return false;
                }
            }

            // Filter by search text
            if (!string.IsNullOrWhiteSpace(this.ChatSearchText))
            {
                return chat.Name != null && chat.Name.Contains(this.ChatSearchText, StringComparison.OrdinalIgnoreCase);
            }

            return true;
        }

        /// <summary>
        /// Command to refresh conversations.
        /// </summary>
        [RelayCommand]
        private async Task RefreshConversationsAsync()
        {
            await this.LoadConversationsAsync();
        }

        /// <summary>
        /// Command to create a new folder.
        /// </summary>
        [RelayCommand]
        private async Task CreateFolderAsync()
        {
            try
            {
                var availableChats = this.Conversations.ToList();

                var dialog = new Views.CreateFolderDialog
                {
                    Owner = Application.Current.MainWindow,
                };

                dialog.ViewModel.PopulateChats(availableChats);

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    var folderName = dialog.ViewModel.FolderName;
                    var selectedChatIds = dialog.ViewModel.GetSelectedChatIds();

                    await this.messagingService.CreateFolderAsync(folderName, selectedChatIds);

                    // Reload folders to get updated data from server
                    await this.LoadFoldersAsync();
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error creating folder");
                this.errorHandlingService.HandleError(ex, "Failed to create folder");
            }
        }

        /// <summary>
        /// Command to edit a folder.
        /// </summary>
        /// <param name="folder">The folder to edit.</param>
        [RelayCommand]
        private async Task EditFolderAsync(ChatFolderViewModel? folder)
        {
            if (folder == null || folder.IsAllChatsFolder)
            {
                return;
            }

            try
            {
                var availableChats = this.Conversations.ToList();

                var dialog = new Views.CreateFolderDialog
                {
                    Owner = Application.Current.MainWindow,
                };

                // Set edit mode
                dialog.ViewModel.FolderId = folder.Id;
                dialog.ViewModel.FolderName = folder.Name;
                dialog.ViewModel.PopulateChats(availableChats, folder.ChatIds);

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    var folderName = dialog.ViewModel.FolderName;
                    var selectedChatIds = dialog.ViewModel.GetSelectedChatIds();

                    await this.messagingService.UpdateFolderAsync(folder.Id, folderName, selectedChatIds);

                    // Reload folders to get updated data from server
                    await this.LoadFoldersAsync();
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error editing folder");
                this.errorHandlingService.HandleError(ex, "Failed to edit folder");
            }
        }

        /// <summary>
        /// Command to delete a folder.
        /// </summary>
        /// <param name="folder">The folder to delete.</param>
        [RelayCommand]
        private async Task DeleteFolderAsync(ChatFolderViewModel? folder)
        {
            if (folder == null || folder.IsAllChatsFolder)
            {
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to delete the folder '{folder.Name}'? The chats will remain, but the folder will be removed.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await this.messagingService.DeleteFolderAsync(folder.Id);

                    // If deleted folder was selected, switch to "All Chats"
                    if (this.SelectedFolder == folder)
                    {
                        this.SelectedFolder = this.Folders.FirstOrDefault(f => f.IsAllChatsFolder);
                    }

                    this.Folders.Remove(folder);
                    this.ConversationsView.Refresh();
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Error deleting folder");
                    this.errorHandlingService.HandleError(ex, "Failed to delete folder");
                }
            }
        }

        /// <summary>
        /// Command to select a folder.
        /// </summary>
        /// <param name="folder">The folder to select.</param>
        [RelayCommand]
        private void SelectFolder(ChatFolderViewModel? folder)
        {
            if (folder != null)
            {
                this.SelectedFolder = folder;
            }
        }

        /// <summary>
        /// Loads folders from the server.
        /// </summary>
        private async Task LoadFoldersAsync()
        {
            try
            {
                var folders = await this.messagingService.GetFoldersAsync();
                var currentUserId = this.authenticationService.CurrentUser?.Id;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Keep "All Chats" folder and remove others
                    var allChatsFolder = this.Folders.FirstOrDefault(f => f.IsAllChatsFolder);
                    this.Folders.Clear();

                    if (allChatsFolder != null)
                    {
                        this.Folders.Add(allChatsFolder);
                    }

                    // Add user folders
                    foreach (var folderDto in folders)
                    {
                        var folderVm = new ChatFolderViewModel(folderDto);
                        this.Folders.Add(folderVm);
                    }

                    // Restore selection
                    if (this.SelectedFolder != null)
                    {
                        var restoredFolder = this.Folders.FirstOrDefault(f => f.Id == this.SelectedFolder.Id);
                        if (restoredFolder != null)
                        {
                            this.SelectedFolder = restoredFolder;
                        }
                        else
                        {
                            this.SelectedFolder = allChatsFolder;
                        }
                    }
                    else
                    {
                        this.SelectedFolder = allChatsFolder;
                    }

                    // Update unread counts for folders
                    this.UpdateFolderUnreadCounts();

                    // Refresh conversations view to apply folder filter
                    this.ConversationsView.Refresh();
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load folders");
            }
        }

        /// <summary>
        /// Updates unread counts for folders based on conversations.
        /// </summary>
        private void UpdateFolderUnreadCounts()
        {
            foreach (var folder in this.Folders)
            {
                if (folder.IsAllChatsFolder)
                {
                    folder.UnreadCount = this.Conversations.Sum(c => c.UnreadCount);
                }
                else
                {
                    folder.UnreadCount = this.Conversations
                        .Where(c => folder.ChatIds.Contains(c.Id))
                        .Sum(c => c.UnreadCount);
                }
            }
        }

        /// <summary>
        /// Command to create a new chat.
        /// </summary>
        [RelayCommand]
        private async Task CreateChatAsync()
        {
            try
            {
                var availableUsers = await this.userDirectoryService.GetAvailableUsersAsync();

                var dialog = new Views.CreateChatDialog
                {
                    Owner = Application.Current.MainWindow,
                };

                // Передаем текущего пользователя в ViewModel
                dialog.ViewModel.CurrentUser = this.authenticationService.CurrentUser;
                dialog.ViewModel.PopulateUsers(availableUsers);

                var result = dialog.ShowDialog();

                if (result == true)
                {
                    // Используем GetFinalChatName() для получения финального названия чата
                    var chatName = dialog.ViewModel.GetFinalChatName();
                    var participantIds = dialog.ViewModel.GetSelectedParticipants();

                    var newChat = await this.messagingService.CreateChatAsync(chatName, participantIds);

                    var currentUserId = this.authenticationService.CurrentUser?.Id;
                    var conversationVm = new ConversationViewModel(newChat, currentUserId, this.avatarService);
                    this.Conversations.Insert(0, conversationVm);
                    this.SelectedConversation = conversationVm;

                    await this.LoadMessagesForSelectedConversationAsync();
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error creating chat");
                this.errorHandlingService.HandleError(ex, "Failed to create chat");
            }
        }

        /// <summary>
        /// Command to delete a chat and all its data.
        /// </summary>
        /// <param name="conversation">The conversation to delete.</param>
        [RelayCommand]
        private async Task DeleteChatAsync(ConversationViewModel? conversation)
        {
            if (conversation == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"Are you sure you want to permanently delete the chat \"{conversation.Name}\"?\n\n" +
                "This will delete all messages, images, and files.\n" +
                "This action cannot be undone.",
                "Delete Chat",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var chatId = conversation.Id;
                    var wasSelected = this.SelectedConversation?.Id == chatId;

                    await this.messagingService.DeleteChatAsync(chatId);

                    // Remove from local collection
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var chatToRemove = this.Conversations.FirstOrDefault(c => c.Id == chatId);
                        if (chatToRemove != null)
                        {
                            this.Conversations.Remove(chatToRemove);
                        }

                        // If the deleted chat was selected, select another one
                        if (wasSelected)
                        {
                            this.SelectedConversation = this.Conversations.FirstOrDefault();
                            if (this.SelectedConversation != null)
                            {
                                _ = this.LoadMessagesForSelectedConversationAsync();
                            }
                            else
                            {
                                this.Messages.Clear();
                                this.receivedMessageIds.Clear();
                            }
                        }

                        // Remove chat from all folders
                        foreach (var folder in this.Folders.Where(f => !f.IsAllChatsFolder))
                        {
                            if (folder.ChatIds.Contains(chatId))
                            {
                                folder.ChatIds.Remove(chatId);
                            }
                        }

                        this.UpdateFolderUnreadCounts();
                        this.ConversationsView.Refresh();
                    });

                    this.logger.Information("Chat {ChatId} deleted successfully", chatId);
                    this.errorHandlingService.ShowInfo($"Chat \"{conversation.Name}\" has been deleted.");
                }
                catch (Exception ex)
                {
                    this.logger.Error(ex, "Failed to delete chat {ChatId}", conversation.Id);
                    this.errorHandlingService.HandleError(ex, "Failed to delete chat. Please try again.");
                }
            }
        }

        /// <summary>
        /// Leaves a group chat.
        /// </summary>
        /// <param name="conversation">The group conversation.</param>
        [RelayCommand]
        private async Task LeaveGroupAsync(ConversationViewModel? conversation)
        {
            if (conversation == null || !conversation.IsGroup)
            {
                return;
            }

            var isLastMember = conversation.Participants == null || conversation.Participants.Count <= 1;
            var confirmText = isLastMember
                ? $"You are the last member. Leaving will permanently delete the group \"{conversation.Name}\" and all its messages."
                : $"Leave the group \"{conversation.Name}\"?\n\nYou can be added again by a member later.";

            var result = MessageBox.Show(
                confirmText,
                isLastMember ? "Delete Group" : "Leave Group",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                var chatId = conversation.Id;
                var wasSelected = this.SelectedConversation?.Id == chatId;

                await this.messagingService.LeaveChatAsync(chatId);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var chatToRemove = this.Conversations.FirstOrDefault(c => c.Id == chatId);
                    if (chatToRemove != null)
                    {
                        this.Conversations.Remove(chatToRemove);
                    }

                    if (wasSelected)
                    {
                        this.SelectedConversation = this.Conversations.FirstOrDefault();
                        if (this.SelectedConversation != null)
                        {
                            _ = this.LoadMessagesForSelectedConversationAsync();
                        }
                        else
                        {
                            this.Messages.Clear();
                            this.receivedMessageIds.Clear();
                        }
                    }

                    foreach (var folder in this.Folders.Where(f => !f.IsAllChatsFolder))
                    {
                        folder.ChatIds.Remove(chatId);
                    }

                    this.UpdateFolderUnreadCounts();
                    this.ConversationsView.Refresh();
                });

                this.errorHandlingService.ShowInfo(
                    isLastMember
                        ? $"Group \"{conversation.Name}\" has been deleted."
                        : $"You left \"{conversation.Name}\".");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to leave group {ChatId}", conversation.Id);
                this.errorHandlingService.HandleError(ex, "Failed to leave group. Please try again.");
            }
        }

        /// <summary>
        /// Edits group name and avatar (owner only).
        /// </summary>
        /// <param name="conversation">The group conversation.</param>
        [RelayCommand]
        private async Task EditGroupAsync(ConversationViewModel? conversation)
        {
            if (conversation == null || !conversation.IsOwner)
            {
                return;
            }

            try
            {
                var dialog = new Views.EditGroupDialog
                {
                    Owner = Application.Current.MainWindow,
                };
                dialog.ViewModel.Initialize(conversation.Name, conversation.AvatarImage);

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                var newName = dialog.ViewModel.GroupName.Trim();
                var updated = await this.messagingService.UpdateChatAsync(conversation.Id, newName);

                if (!string.IsNullOrEmpty(dialog.ViewModel.AvatarFilePath))
                {
                    updated = await this.messagingService.UploadChatAvatarAsync(
                        conversation.Id,
                        dialog.ViewModel.AvatarFilePath);
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    conversation.Name = updated.Name ?? newName;
                    if (!string.IsNullOrEmpty(updated.AvatarUrl))
                    {
                        conversation.UpdateAvatarUrl(updated.AvatarUrl);
                    }

                    this.ConversationsView.Refresh();
                });

                this.errorHandlingService.ShowInfo("Group updated.");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to edit group {ChatId}", conversation.Id);
                this.errorHandlingService.HandleError(ex, "Failed to update group. Please try again.");
            }
        }

        /// <summary>
        /// Adds a chat to a personal folder.
        /// </summary>
        /// <param name="parameter">Tuple-like object: conversation and folder via object array, or folder with conversation from selected.</param>
        [RelayCommand]
        private async Task AddChatToFolderAsync(object? parameter)
        {
            ConversationViewModel? conversation = null;
            ChatFolderViewModel? folder = null;

            if (parameter is object[] args && args.Length >= 2)
            {
                conversation = args[0] as ConversationViewModel;
                folder = args[1] as ChatFolderViewModel;
            }

            if (conversation == null || folder == null || folder.IsAllChatsFolder)
            {
                return;
            }

            try
            {
                if (folder.ChatIds.Contains(conversation.Id))
                {
                    this.errorHandlingService.ShowInfo($"\"{conversation.Name}\" is already in \"{folder.Name}\".");
                    return;
                }

                var chatIds = folder.ChatIds.ToList();
                chatIds.Add(conversation.Id);
                await this.messagingService.UpdateFolderAsync(folder.Id, folder.Name, chatIds);
                folder.ChatIds.Add(conversation.Id);
                this.UpdateFolderUnreadCounts();
                this.ConversationsView.Refresh();
                this.errorHandlingService.ShowInfo($"Added to \"{folder.Name}\".");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to add chat to folder");
                this.errorHandlingService.HandleError(ex, "Failed to add chat to folder.");
            }
        }

        /// <summary>
        /// Removes a chat from a personal folder.
        /// </summary>
        /// <param name="parameter">Object array with conversation and folder.</param>
        [RelayCommand]
        private async Task RemoveChatFromFolderAsync(object? parameter)
        {
            ConversationViewModel? conversation = null;
            ChatFolderViewModel? folder = null;

            if (parameter is object[] args && args.Length >= 2)
            {
                conversation = args[0] as ConversationViewModel;
                folder = args[1] as ChatFolderViewModel;
            }

            if (conversation == null || folder == null || folder.IsAllChatsFolder)
            {
                return;
            }

            try
            {
                if (!folder.ChatIds.Contains(conversation.Id))
                {
                    return;
                }

                var chatIds = folder.ChatIds.Where(id => id != conversation.Id).ToList();
                await this.messagingService.UpdateFolderAsync(folder.Id, folder.Name, chatIds);
                folder.ChatIds.Remove(conversation.Id);
                this.UpdateFolderUnreadCounts();
                this.ConversationsView.Refresh();
                this.errorHandlingService.ShowInfo($"Removed from \"{folder.Name}\".");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to remove chat from folder");
                this.errorHandlingService.HandleError(ex, "Failed to remove chat from folder.");
            }
        }

        /// <summary>
        /// Toggles the Online/Invisible status dropdown behind the + button.
        /// </summary>
        [RelayCommand]
        private void ToggleStatusDropdown()
        {
            this.IsStatusDropdownOpen = !this.IsStatusDropdownOpen;
        }

        /// <summary>
        /// Sets the current user status to Online.
        /// </summary>
        [RelayCommand]
        private async Task SetOnlineStatusAsync()
        {
            await this.SetMyStatusAsync(UserStatus.Online);
        }

        /// <summary>
        /// Sets the current user status to Invisible.
        /// </summary>
        [RelayCommand]
        private async Task SetInvisibleStatusAsync()
        {
            await this.SetMyStatusAsync(UserStatus.Invisible);
        }

        private async Task LoadMyStatusAsync()
        {
            try
            {
                var status = await this.messagingService.GetMyStatusAsync();
                this.MyStatus = status.Status == UserStatus.Invisible
                    ? UserStatus.Invisible
                    : UserStatus.Online;
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to load own status");
                this.MyStatus = UserStatus.Online;
            }
        }

        private async Task SetMyStatusAsync(UserStatus targetStatus)
        {
            if (this.isUpdatingStatus)
            {
                return;
            }

            var previous = this.MyStatus;
            this.MyStatus = targetStatus;
            this.IsStatusDropdownOpen = false;
            this.isUpdatingStatus = true;

            try
            {
                await this.messagingService.SetMyStatusAsync(targetStatus);
            }
            catch (Exception ex)
            {
                this.MyStatus = previous;
                this.logger.Error(ex, "Failed to set status");
                this.errorHandlingService.HandleError(ex, "Failed to update status.");
            }
            finally
            {
                this.isUpdatingStatus = false;
            }
        }

        /// <summary>
        /// Command to navigate to the image generator view.
        /// </summary>
        [RelayCommand]
        private void NavigateToGenerator()
        {
            this.navigationService.NavigateTo<GeneratorViewModel>();
        }

        /// <summary>
        /// Command to toggle the emoji picker.
        /// </summary>
        [RelayCommand]
        private void ToggleEmojiPicker()
        {
            this.IsEmojiPickerOpen = !this.IsEmojiPickerOpen;
        }

        /// <summary>
        /// Command to insert an emoji into the message text.
        /// </summary>
        /// <param name="emoji">The emoji to insert.</param>
        [RelayCommand]
        private void InsertEmoji(string? emoji)
        {
            if (string.IsNullOrEmpty(emoji))
            {
                return;
            }

            this.MessageText += emoji;
        }

        /// <summary>
        /// Command to navigate to the settings view.
        /// </summary>
        [RelayCommand]
        private void NavigateToSettings()
        {
            this.navigationService.NavigateTo<SettingsViewModel>();
        }

        private bool CanSendMessage()
        {
            return this.SelectedConversation != null &&
                   !this.IsLoadingMessages &&
                   (!string.IsNullOrWhiteSpace(this.MessageText) || this.PendingAttachments.Count > 0);
        }

        /// <summary>
        /// Processes files dropped via drag and drop.
        /// </summary>
        /// <param name="files">The file paths that were dropped.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Method must be internal to be accessible from view code-behind")]
        internal void ProcessDroppedFilesAsync(string[] files)
        {
            try
            {
                foreach (var filePath in files)
                {
                    // Validate file
                    var validation = this.fileAttachmentService.ValidateFile(filePath);
                    if (!validation.IsValid)
                    {
                        this.errorHandlingService.ShowWarning($"{System.IO.Path.GetFileName(filePath)}: {validation.ErrorMessage}");
                        continue;
                    }

                    // Add to pending attachments
                    var attachmentVm = new AttachmentViewModel(filePath);
                    this.PendingAttachments.Add(attachmentVm);

                    this.logger.Information("File attached via drag-drop: {FileName} ({Size})", attachmentVm.FileName, attachmentVm.FileSizeFormatted);
                }

                this.SendMessageCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to process dropped files");
                this.errorHandlingService.ShowError("Failed to process dropped files. Please try again.");
            }
        }

        private async Task InitializeAsync()
        {
            try
            {
                this.logger.Information("ChatViewModel.InitializeAsync called");

                if (!this.messagingService.IsConnected)
                {
                    var token = this.authenticationService.AccessToken;
                    if (!string.IsNullOrEmpty(token))
                    {
                        this.logger.Information("Attempting to connect WebSocket with token");
                        await this.messagingService.ConnectAsync(token);
                        this.logger.Information("WebSocket connection completed");
                    }
                    else
                    {
                        this.logger.Warning("No access token available for WebSocket connection");
                    }
                }

                // Load users cache for fallback when participants list is empty
                await this.LoadUsersCacheAsync();

                await this.LoadConversationsAsync();
                await this.LoadFoldersAsync();
                await this.LoadMyStatusAsync();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to initialize chat view");
                this.ErrorMessage = "Failed to connect to chat service. Please try again.";
            }
        }

        /// <summary>
        /// Loads users cache for fallback when participants list is empty.
        /// </summary>
        private async Task LoadUsersCacheAsync()
        {
            try
            {
                var users = await this.userDirectoryService.GetAvailableUsersAsync();
                this.usersCache.Clear();
                foreach (var user in users)
                {
                    this.usersCache[user.Id] = user;
                }

                // Also add current user to cache
                var currentUser = this.authenticationService.CurrentUser;
                if (currentUser != null)
                {
                    this.usersCache[currentUser.Id] = currentUser;
                }

                this.logger.Debug("Loaded {Count} users into cache", this.usersCache.Count);
            }
            catch (Exception ex)
            {
                this.logger.Warning(ex, "Failed to load users cache");
            }
        }

        private async Task LoadConversationsAsync()
        {
            this.IsLoadingConversations = true;
            this.ErrorMessage = null;

            try
            {
                // BUG FIX #3: Save current selection before clearing
                var selectedChatId = this.SelectedConversation?.Id;
                this.logger.Debug("Saved selected chat ID before refresh: {ChatId}", selectedChatId);

                var chats = await this.messagingService.GetChatsAsync();
                var currentUserId = this.authenticationService.CurrentUser?.Id;

                // BUG FIX #2: Load last message preview for each chat
                var conversationTasks = chats.Select(async chat =>
                {
                    var conversation = new ConversationViewModel(chat, currentUserId, this.avatarService);

                    // Load last message for preview
                    try
                    {
                        var messages = await this.messagingService.GetMessageHistoryAsync(chat.Id, limit: 1);
                        if (messages.Any())
                        {
                            var lastMessage = messages.Last();
                            conversation.LastMessagePreview = this.GetMessagePreview(lastMessage, 50);
                            this.logger.Debug("Loaded preview for chat {ChatId}: {Preview}", chat.Id, conversation.LastMessagePreview);
                        }
                        else
                        {
                            conversation.LastMessagePreview = "No messages yet";
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Failed to load preview for chat {ChatId}", chat.Id);
                        conversation.LastMessagePreview = "Unable to load preview";
                    }

                    return conversation;
                }).ToList();

                var conversationVms = await Task.WhenAll(conversationTasks);

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    this.Conversations.Clear();
                    foreach (var conversation in conversationVms.OrderByDescending(c => c.LastMessageAt))
                    {
                        this.Conversations.Add(conversation);
                    }

                    // BUG FIX #3: Restore previous selection
                    ConversationViewModel? restoredChat = null;
                    if (!string.IsNullOrEmpty(selectedChatId))
                    {
                        restoredChat = this.Conversations.FirstOrDefault(c => c.Id == selectedChatId);
                        if (restoredChat != null)
                        {
                            this.logger.Information("Selection restored: {ChatId}", selectedChatId);
                        }
                        else
                        {
                            // Selected chat was deleted, select first available
                            restoredChat = this.Conversations.FirstOrDefault();
                            this.logger.Warning("Selected chat was deleted, selecting first chat");
                        }
                    }
                    else if (this.Conversations.Any() && this.SelectedConversation == null)
                    {
                        // No previous selection, select first
                        restoredChat = this.Conversations.First();
                        this.logger.Debug("No previous selection, selecting first chat");
                    }

                    // Update SelectedConversation if needed to trigger UI refresh
                    if (restoredChat != null)
                    {
                        this.SelectedConversation = restoredChat;
                    }

                    // Update folder unread counts after conversations are loaded
                    this.UpdateFolderUnreadCounts();
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load conversations");
                this.ErrorMessage = "Failed to load conversations. Please try again.";
            }
            finally
            {
                this.IsLoadingConversations = false;
            }
        }

        private async Task LoadMessagesForSelectedConversationAsync()
        {
            if (this.SelectedConversation == null)
            {
                return;
            }

            this.IsLoadingMessages = true;
            this.ErrorMessage = null;

            try
            {
                // For group chats, refresh participants list
                if (this.SelectedConversation.Type == ChatType.Group || this.SelectedConversation.Type == ChatType.Channel)
                {
                    try
                    {
                        var chats = await this.messagingService.GetChatsAsync();
                        var chat = chats.FirstOrDefault(c => c.Id == this.SelectedConversation.Id);
                        if (chat != null && chat.Participants != null && chat.Participants.Any())
                        {
                            this.SelectedConversation.UpdateParticipants(chat.Participants);

                            // Update GroupChatParticipants for UI display
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                this.GroupChatParticipants.Clear();
                                foreach (var participant in chat.Participants)
                                {
                                    this.GroupChatParticipants.Add(participant);
                                }
                            });

                            this.logger.Debug("Refreshed participants list for chat {ChatId}: {Count} participants", this.SelectedConversation.Id, chat.Participants.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.logger.Warning(ex, "Failed to refresh participants list for chat {ChatId}", this.SelectedConversation.Id);
                    }
                }

                var messages = await this.messagingService.GetMessageHistoryAsync(
                    this.SelectedConversation.Id,
                    limit: 50);

                var currentUserId = this.authenticationService.CurrentUser?.Id ?? string.Empty;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    this.Messages.Clear();

                    // DON'T clear receivedMessageIds - preserve it for deduplication
                    // this.receivedMessageIds.Clear();
                    foreach (var message in messages.OrderBy(m => m.CreatedAt))
                    {
                        // CRITICAL: Filter messages by ChatId and track received IDs
                        if (message.ChatId == this.SelectedConversation.Id)
                        {
                            this.receivedMessageIds.Add(message.Id);

                            // Get sender info from selected conversation if available
                            string? senderName = null;
                            string? senderAvatarUrl = null;
                            var isGroupChat = this.SelectedConversation.Type == ChatType.Group || this.SelectedConversation.Type == ChatType.Channel;

                            // For group chats, get from participants list for other users only
                            if (isGroupChat && message.SenderId != currentUserId)
                            {
                                senderName = this.SelectedConversation.GetParticipantName(message.SenderId);
                                senderAvatarUrl = this.SelectedConversation.GetParticipantAvatarUrl(message.SenderId);

                                // Fallback: if participant not found, try users cache
                                if (string.IsNullOrEmpty(senderName) && this.usersCache.TryGetValue(message.SenderId, out var cachedUser))
                                {
                                    senderName = !string.IsNullOrWhiteSpace(cachedUser.DisplayName) ? cachedUser.DisplayName : cachedUser.Username;
                                    senderAvatarUrl = cachedUser.AvatarUrl;
                                    this.logger.Debug("Found participant {SenderId} in users cache", message.SenderId);
                                }
                                else if (string.IsNullOrEmpty(senderName))
                                {
                                    this.logger.Debug("Participant {SenderId} not found in participants list (Count: {Count}) or cache for chat {ChatId}", message.SenderId, this.SelectedConversation.Participants.Count, this.SelectedConversation.Id);
                                }
                            }

                            // For direct messages and own messages in group chats, don't set name (won't be displayed)
                            this.Messages.Add(new MessageViewModel(
                                message,
                                currentUserId,
                                this.fileAttachmentService,
                                this.avatarService,
                                senderName,
                                senderAvatarUrl,
                                isGroupChat));
                        }
                        else
                        {
                            this.logger.Warning(
                                "Message {MessageId} belongs to chat {MessageChatId}, not selected chat {SelectedChatId}",
                                message.Id,
                                message.ChatId,
                                this.SelectedConversation.Id);
                        }
                    }

                    // Insert date separators after all messages are loaded
                    this.InsertDateSeparators();

                    this.CalculateProfileStatistics();

                    var messageCount = this.GetMessageViewModels().Count();
                    this.logger.Information(
                        "Loaded {Count} messages for chat {ChatId}",
                        messageCount,
                        this.SelectedConversation.Id);
                });

                if (messages.Any())
                {
                    // Get the message with the latest creation time (truly last message)
                    var lastMessage = messages.OrderByDescending(m => m.CreatedAt).First();
                    this.SelectedConversation.LastMessagePreview = this.GetMessagePreview(lastMessage, 50);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load messages for conversation {ConversationId}", this.SelectedConversation.Id);
                this.ErrorMessage = "Failed to load messages. Please try again.";
            }
            finally
            {
                this.IsLoadingMessages = false;
            }
        }

        private async Task SendTypingIndicatorAsync()
        {
            if (this.SelectedConversation == null || string.IsNullOrWhiteSpace(this.MessageText))
            {
                return;
            }

            this.typingCancellationTokenSource?.Cancel();
            this.typingCancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Delay(1000, this.typingCancellationTokenSource.Token);
                await this.messagingService.SendTypingIndicatorAsync(this.SelectedConversation.Id);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to send typing indicator");
            }
        }

        private void SubscribeToMessagingEvents()
        {
            if (this.isSubscribedToEvents)
            {
                this.logger.Warning("Already subscribed to messaging events, skipping duplicate subscription");
                return;
            }

            this.messagingService.MessageReceived += this.OnMessageReceived;
            this.messagingService.MessageEdited += this.OnMessageEdited;
            this.messagingService.MessageDeleted += this.OnMessageDeleted;
            this.messagingService.MessageReactionUpdated += this.OnMessageReactionUpdated;
            this.messagingService.UserTyping += this.OnUserTyping;
            this.messagingService.UserStatusChanged += this.OnUserStatusChanged;
            this.messagingService.UserAvatarChanged += this.OnUserAvatarChanged;
            this.messagingService.ConnectionStateChanged += this.OnConnectionStateChanged;
            this.messagingService.ChatDeleted += this.OnChatDeleted;
            this.messagingService.ChatCreated += this.OnChatCreated;
            this.messagingService.ChatUpdated += this.OnChatUpdated;
            this.isSubscribedToEvents = true;
            this.logger.Information("Subscribed to messaging events");
        }

        private void UnsubscribeFromMessagingEvents()
        {
            if (!this.isSubscribedToEvents)
            {
                this.logger.Warning("Not subscribed to messaging events, skipping unsubscribe");
                return;
            }

            this.messagingService.MessageReceived -= this.OnMessageReceived;
            this.messagingService.MessageEdited -= this.OnMessageEdited;
            this.messagingService.MessageDeleted -= this.OnMessageDeleted;
            this.messagingService.MessageReactionUpdated -= this.OnMessageReactionUpdated;
            this.messagingService.UserTyping -= this.OnUserTyping;
            this.messagingService.UserStatusChanged -= this.OnUserStatusChanged;
            this.messagingService.UserAvatarChanged -= this.OnUserAvatarChanged;
            this.messagingService.ConnectionStateChanged -= this.OnConnectionStateChanged;
            this.messagingService.ChatDeleted -= this.OnChatDeleted;
            this.messagingService.ChatCreated -= this.OnChatCreated;
            this.messagingService.ChatUpdated -= this.OnChatUpdated;
            this.isSubscribedToEvents = false;
            this.logger.Information("Unsubscribed from messaging events");
        }

        private void OnMessageReceived(object? sender, MessageDto messageDto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // CRITICAL: Deduplicate messages by ID
                if (this.receivedMessageIds.Contains(messageDto.Id))
                {
                    this.logger.Warning(
                        "Duplicate message received: {MessageId} in chat {ChatId}",
                        messageDto.Id,
                        messageDto.ChatId);
                    return;
                }

                // CRITICAL: Only add message to UI if it belongs to the selected conversation
                if (this.SelectedConversation != null && messageDto.ChatId == this.SelectedConversation.Id)
                {
                    this.receivedMessageIds.Add(messageDto.Id);
                    var currentUserId = this.authenticationService.CurrentUser?.Id ?? string.Empty;

                    // Get sender info from selected conversation if available
                    string? senderName = null;
                    string? senderAvatarUrl = null;
                    var isGroupChat = this.SelectedConversation.Type == ChatType.Group || this.SelectedConversation.Type == ChatType.Channel;

                    if (this.SelectedConversation != null)
                    {
                        // For group chats, get from participants list for other users only
                        if (isGroupChat && messageDto.SenderId != currentUserId)
                        {
                            senderName = this.SelectedConversation.GetParticipantName(messageDto.SenderId);
                            senderAvatarUrl = this.SelectedConversation.GetParticipantAvatarUrl(messageDto.SenderId);
                        }

                        // For direct messages and own messages in group chats, don't set name (won't be displayed)
                    }

                    var messageViewModel = new MessageViewModel(
                        messageDto,
                        currentUserId,
                        this.fileAttachmentService,
                        this.avatarService,
                        senderName,
                        senderAvatarUrl,
                        isGroupChat);

                    // Determine where to insert the message (keep messages sorted by date)
                    int insertIndex = this.Messages.Count;
                    for (int i = 0; i < this.Messages.Count; i++)
                    {
                        if (this.Messages[i] is MessageViewModel existingMessage)
                        {
                            if (existingMessage.CreatedAt > messageDto.CreatedAt)
                            {
                                insertIndex = i;
                                break;
                            }
                        }
                    }

                    // Insert date separator if needed (returns adjusted index)
                    insertIndex = this.InsertDateSeparatorIfNeeded(messageDto.CreatedAt, insertIndex);

                    this.Messages.Insert(insertIndex, messageViewModel);

                    this.logger.Information(
                        "Message {MessageId} added to chat {ChatId} by user {SenderId}",
                        messageDto.Id,
                        messageDto.ChatId,
                        messageDto.SenderId);
                }
                else if (messageDto.ChatId != this.SelectedConversation?.Id)
                {
                    // Message for different conversation - update unread count
                    var conversation = this.Conversations.FirstOrDefault(c => c.Id == messageDto.ChatId);
                    if (conversation != null)
                    {
                        conversation.UnreadCount++;
                        this.logger.Information(
                            "Message {MessageId} for inactive chat {ChatId}, unread count now {UnreadCount}",
                            messageDto.Id,
                            messageDto.ChatId,
                            conversation.UnreadCount);
                    }
                    else
                    {
                        // Chat not in list yet (e.g. recipient never got ChatCreated) — fetch and apply.
                        this.logger.Information(
                            "Message {MessageId} for unknown chat {ChatId}, fetching conversation",
                            messageDto.Id,
                            messageDto.ChatId);
                        _ = this.EnsureConversationExistsAndApplyMessageAsync(messageDto);
                        return;
                    }
                }

                // Update conversation preview for all conversations
                var targetConversation = this.Conversations.FirstOrDefault(c => c.Id == messageDto.ChatId);
                if (targetConversation != null)
                {
                    targetConversation.LastMessagePreview = this.GetMessagePreview(messageDto, 50);
                    targetConversation.LastMessageAt = messageDto.CreatedAt;
                    this.logger.Debug("Chat preview updated for {ChatId}", messageDto.ChatId);
                }
            });
        }

        private void OnChatCreated(object? sender, ChatDto chat)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.AddConversationIfMissing(chat, markUnread: false);
            });
        }

        private void OnChatUpdated(object? sender, ChatDto chat)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var conversation = this.Conversations.FirstOrDefault(c => c.Id == chat.Id);
                if (conversation == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(chat.Name))
                {
                    conversation.Name = chat.Name;
                }

                conversation.UpdateAvatarUrl(chat.AvatarUrl);
                this.ConversationsView.Refresh();
                this.logger.Information("Applied chat update for {ChatId}: Name={Name}", chat.Id, chat.Name);
            });
        }

        private ConversationViewModel? AddConversationIfMissing(ChatDto chat, bool markUnread)
        {
            var existing = this.Conversations.FirstOrDefault(c => c.Id == chat.Id);
            if (existing != null)
            {
                return existing;
            }

            var currentUserId = this.authenticationService.CurrentUser?.Id;
            var conversationVm = new ConversationViewModel(chat, currentUserId, this.avatarService);
            if (markUnread)
            {
                conversationVm.UnreadCount = 1;
            }

            this.Conversations.Insert(0, conversationVm);
            this.ConversationsView.Refresh();
            this.logger.Information("Added new conversation {ChatId} to list", chat.Id);
            return conversationVm;
        }

        private async Task EnsureConversationExistsAndApplyMessageAsync(MessageDto messageDto)
        {
            try
            {
                var chat = await this.messagingService.GetChatAsync(messageDto.ChatId);
                if (chat == null)
                {
                    this.logger.Warning("Could not load unknown chat {ChatId} for message {MessageId}", messageDto.ChatId, messageDto.Id);
                    return;
                }

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (this.receivedMessageIds.Contains(messageDto.Id))
                    {
                        return;
                    }

                    var alreadyExisted = this.Conversations.Any(c => c.Id == chat.Id);
                    var conversation = this.AddConversationIfMissing(chat, markUnread: !alreadyExisted);

                    if (conversation == null)
                    {
                        return;
                    }

                    if (alreadyExisted && this.SelectedConversation?.Id != messageDto.ChatId)
                    {
                        conversation.UnreadCount++;
                    }

                    conversation.LastMessagePreview = this.GetMessagePreview(messageDto, 50);
                    conversation.LastMessageAt = messageDto.CreatedAt;
                    this.UpdateFolderUnreadCounts();
                    this.logger.Information(
                        "Applied message {MessageId} to newly loaded chat {ChatId}",
                        messageDto.Id,
                        messageDto.ChatId);
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load conversation for message {MessageId} in chat {ChatId}", messageDto.Id, messageDto.ChatId);
            }
        }

        private void OnMessageEdited(object? sender, MessageDto messageDto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.logger.Information(
                    "EditMessage received for message {MessageId}, ChatId: {ChatId}, HasAttachments: {HasAttachments}, AttachmentCount: {AttachmentCount}",
                    messageDto.Id,
                    messageDto.ChatId,
                    messageDto.Attachments != null && messageDto.Attachments.Count > 0,
                    messageDto.Attachments?.Count ?? 0);

                var message = this.FindMessageViewModel(messageDto.Id);
                if (message != null)
                {
                    // Message exists in UI, update it
                    var oldAttachmentCount = message.Attachments.Count;
                    message.UpdateFromDto(messageDto);
                    this.logger.Information(
                        "Message {MessageId} updated in UI: old attachments: {OldCount}, new attachments: {NewCount}",
                        messageDto.Id,
                        oldAttachmentCount,
                        messageDto.Attachments?.Count ?? 0);
                }
                else if (this.SelectedConversation != null && messageDto.ChatId == this.SelectedConversation.Id)
                {
                    // Message doesn't exist in UI but belongs to selected conversation
                    // This can happen if EditMessage arrives before NewMessage or if message was edited before being displayed
                    // Add it to UI if it's for the current chat
                    if (!this.receivedMessageIds.Contains(messageDto.Id))
                    {
                        this.receivedMessageIds.Add(messageDto.Id);
                        var currentUserId = this.authenticationService.CurrentUser?.Id ?? string.Empty;

                        // Get sender info from selected conversation if available
                        string? senderName = null;
                        string? senderAvatarUrl = null;
                        var isGroupChat = this.SelectedConversation.Type == ChatType.Group || this.SelectedConversation.Type == ChatType.Channel;

                        if (isGroupChat && messageDto.SenderId != currentUserId)
                        {
                            senderName = this.SelectedConversation.GetParticipantName(messageDto.SenderId);
                            senderAvatarUrl = this.SelectedConversation.GetParticipantAvatarUrl(messageDto.SenderId);
                        }

                        var messageViewModel = new MessageViewModel(
                            messageDto,
                            currentUserId,
                            this.fileAttachmentService,
                            this.avatarService,
                            senderName,
                            senderAvatarUrl,
                            isGroupChat);

                        // Determine where to insert the message (keep messages sorted by date)
                        int insertIndex = this.Messages.Count;
                        for (int i = 0; i < this.Messages.Count; i++)
                        {
                            if (this.Messages[i] is MessageViewModel existingMessage)
                            {
                                if (existingMessage.CreatedAt > messageDto.CreatedAt)
                                {
                                    insertIndex = i;
                                    break;
                                }
                            }
                        }

                        // Insert date separator if needed (returns adjusted index)
                        insertIndex = this.InsertDateSeparatorIfNeeded(messageDto.CreatedAt, insertIndex);

                        this.Messages.Insert(insertIndex, messageViewModel);

                        this.logger.Information(
                            "Message {MessageId} added to UI via EditMessage event with {AttachmentCount} attachments",
                            messageDto.Id,
                            messageDto.Attachments?.Count ?? 0);
                    }
                    else
                    {
                        this.logger.Warning(
                            "EditMessage received for message {MessageId} that is already in receivedMessageIds but not in UI. This might indicate a synchronization issue.",
                            messageDto.Id);
                    }
                }
                else
                {
                    this.logger.Debug(
                        "EditMessage received for message {MessageId} but it's not in UI and not for selected conversation (SelectedChat: {SelectedChatId}, MessageChat: {MessageChatId})",
                        messageDto.Id,
                        this.SelectedConversation?.Id ?? "null",
                        messageDto.ChatId);
                }
            });
        }

        private void OnMessageReactionUpdated(object? sender, MessageDto messageDto)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var message = this.FindMessageViewModel(messageDto.Id);
                if (message != null)
                {
                    message.UpdateFromDto(messageDto);
                }
            });
        }

        private void OnMessageDeleted(object? sender, string messageId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // BUG FIX #1: Remove deleted message from UI completely
                var message = this.FindMessageViewModel(messageId);
                if (message != null)
                {
                    this.Messages.Remove(message);
                    this.receivedMessageIds.Remove(messageId);

                    // Clean up orphaned date separators
                    this.CleanupOrphanedSeparators();

                    this.logger.Information("Message removed from UI: {MessageId}", messageId);
                }
            });
        }

        private void OnChatDeleted(object? sender, string chatId)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var chatToRemove = this.Conversations.FirstOrDefault(c => c.Id == chatId);
                if (chatToRemove != null)
                {
                    var wasSelected = this.SelectedConversation?.Id == chatId;

                    this.Conversations.Remove(chatToRemove);
                    this.logger.Information("Chat {ChatId} removed from conversations list", chatId);

                    // If the deleted chat was selected, select another one
                    if (wasSelected)
                    {
                        this.SelectedConversation = this.Conversations.FirstOrDefault();
                        if (this.SelectedConversation != null)
                        {
                            _ = this.LoadMessagesForSelectedConversationAsync();
                        }
                        else
                        {
                            this.Messages.Clear();
                            this.receivedMessageIds.Clear();
                        }
                    }

                    // Remove chat from all folders
                    foreach (var folder in this.Folders.Where(f => !f.IsAllChatsFolder))
                    {
                        if (folder.ChatIds.Contains(chatId))
                        {
                            folder.ChatIds.Remove(chatId);
                        }
                    }

                    this.UpdateFolderUnreadCounts();
                    this.ConversationsView.Refresh();
                }
            });
        }

        /// <summary>
        /// Command to toggle a reaction on a message.
        /// </summary>
        /// <param name="parameter">Tuple of (MessageViewModel, emoji string).</param>
        [RelayCommand]
        private async Task ToggleReaction(object? parameter)
        {
            if (parameter == null || this.SelectedConversation == null)
            {
                return;
            }

            try
            {
                // Parameter should be a tuple: (MessageViewModel, string emoji)
                if (parameter is System.Tuple<MessageViewModel, string> tuple)
                {
                    var message = tuple.Item1;
                    var emoji = tuple.Item2;
                    var currentUserId = this.authenticationService.CurrentUser?.Id;

                    if (string.IsNullOrEmpty(currentUserId))
                    {
                        return;
                    }

                    // Check if user already reacted with this emoji
                    if (message.HasUserReacted(emoji, currentUserId))
                    {
                        // Remove reaction
                        await this.messagingService.RemoveReactionAsync(
                            this.SelectedConversation.Id,
                            message.Id,
                            emoji);
                    }
                    else
                    {
                        // Add reaction
                        await this.messagingService.AddReactionAsync(
                            this.SelectedConversation.Id,
                            message.Id,
                            emoji);
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error toggling reaction");
                this.errorHandlingService.HandleError(ex, "Failed to update reaction");
            }
        }

        /// <summary>
        /// Removes date separators that no longer have messages before or after them.
        /// </summary>
        private void CleanupOrphanedSeparators()
        {
            var separatorsToRemove = new List<DateSeparatorViewModel>();

            for (int i = 0; i < this.Messages.Count; i++)
            {
                if (this.Messages[i] is DateSeparatorViewModel separator)
                {
                    // Check if separator has messages on both sides
                    bool hasMessageBefore = false;
                    bool hasMessageAfter = false;

                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (this.Messages[j] is MessageViewModel)
                        {
                            hasMessageBefore = true;
                            break;
                        }

                        if (this.Messages[j] is DateSeparatorViewModel)
                        {
                            break; // Stop at previous separator
                        }
                    }

                    for (int j = i + 1; j < this.Messages.Count; j++)
                    {
                        if (this.Messages[j] is MessageViewModel)
                        {
                            hasMessageAfter = true;
                            break;
                        }

                        if (this.Messages[j] is DateSeparatorViewModel)
                        {
                            break; // Stop at next separator
                        }
                    }

                    // Remove separator if it doesn't separate messages
                    if (!hasMessageBefore || !hasMessageAfter)
                    {
                        separatorsToRemove.Add(separator);
                    }
                }
            }

            foreach (var separator in separatorsToRemove)
            {
                this.Messages.Remove(separator);
            }
        }

        private void OnUserTyping(object? sender, string userId)
        {
        }

        private void OnUserStatusChanged(object? sender, StatusUpdateDto statusUpdate)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.logger.Information("User {UserId} status changed to {Status}", statusUpdate.UserId, statusUpdate.Status);

                // Update status for all conversations involving this user
                foreach (var conversation in this.Conversations)
                {
                    if (conversation.OtherUserId == statusUpdate.UserId)
                    {
                        // Invisible appears as Offline to other users
                        conversation.Status = statusUpdate.Status == UserStatus.Invisible
                            ? UserStatus.Offline
                            : statusUpdate.Status;
                        this.logger.Debug("Updated conversation status for user {UserId}: {Status}", statusUpdate.UserId, conversation.Status);
                    }
                }

                // Force update SelectedConversation if it's the same user to trigger UI refresh
                if (this.SelectedConversation != null && this.SelectedConversation.OtherUserId == statusUpdate.UserId)
                {
                    // Trigger property change to update UI bindings
                    var currentConversation = this.SelectedConversation;
                    this.SelectedConversation = null;
                    this.SelectedConversation = currentConversation;
                    this.logger.Debug("Force refreshed SelectedConversation for user {UserId} to update UI", statusUpdate.UserId);
                }

                // Update profile status if profile is open and showing this user
                if (this.IsUserProfileVisible &&
                    this.SelectedConversation != null &&
                    this.SelectedConversation.OtherUserId == statusUpdate.UserId)
                {
                    this.ProfileStatus = statusUpdate.Status == UserStatus.Invisible
                        ? UserStatus.Offline
                        : statusUpdate.Status;
                    this.OnPropertyChanged(nameof(this.ProfileStatusText));
                }
            });
        }

        private void OnUserAvatarChanged(object? sender, AvatarUpdateDto avatarUpdate)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                this.logger.Information("Received avatar update for user {UserId} with URL: {AvatarUrl}", avatarUpdate.UserId, avatarUpdate.AvatarUrl);

                // Update avatar for all conversations involving this user
                var updatedConversations = 0;
                bool selectedConversationUpdated = false;
                foreach (var conversation in this.Conversations)
                {
                    if (conversation.Type == Shared.Enums.ChatType.DirectMessage && conversation.OtherUserId == avatarUpdate.UserId)
                    {
                        conversation.UpdateAvatarUrl(avatarUpdate.AvatarUrl);
                        updatedConversations++;
                        this.logger.Debug("Updated conversation avatar for user {UserId}", avatarUpdate.UserId);

                        // Check if this is the selected conversation
                        if (this.SelectedConversation != null && this.SelectedConversation.Id == conversation.Id)
                        {
                            selectedConversationUpdated = true;
                        }
                    }
                }

                // Force update SelectedConversation if it's the same user to trigger UI refresh
                if (selectedConversationUpdated && this.SelectedConversation != null)
                {
                    var currentConversation = this.SelectedConversation;
                    this.SelectedConversation = null;
                    this.SelectedConversation = currentConversation;
                    this.logger.Debug("Force refreshed SelectedConversation avatar for user {UserId} to update UI", avatarUpdate.UserId);
                }

                // Update avatar for all messages from this user
                var updatedMessages = 0;
                foreach (var messageItem in this.Messages)
                {
                    if (messageItem is MessageViewModel messageVm && messageVm.SenderId == avatarUpdate.UserId)
                    {
                        messageVm.UpdateSenderAvatarUrl(avatarUpdate.AvatarUrl);
                        updatedMessages++;
                    }
                }

                this.logger.Debug("Avatar update completed: {Conversations} conversations, {Messages} messages updated for user {UserId}", updatedConversations, updatedMessages, avatarUpdate.UserId);

                // If the current user's avatar was updated, refresh settings view
                if (this.authenticationService.CurrentUser?.Id == avatarUpdate.UserId)
                {
                    // Force refresh of current user's avatar in settings if it's the current user
                    this.authenticationService.UpdateCurrentUserAvatar(avatarUpdate.AvatarUrl);
                    this.logger.Debug("Updated current user's avatar in AuthenticationService.");
                }
            });
        }

        /// <summary>
        /// Starts a timer to periodically refresh avatars to ensure they stay up-to-date.
        /// </summary>
        private void StartAvatarRefreshTimer()
        {
            this.StopAvatarRefreshTimer(); // Stop existing timer if any

            this.avatarRefreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30), // Refresh every 30 seconds
            };
            this.avatarRefreshTimer.Tick += this.AvatarRefreshTimer_Tick;
            this.avatarRefreshTimer.Start();
            this.logger.Debug("Started avatar refresh timer (30 second interval)");
        }

        /// <summary>
        /// Stops the avatar refresh timer.
        /// </summary>
        private void StopAvatarRefreshTimer()
        {
            if (this.avatarRefreshTimer != null)
            {
                this.avatarRefreshTimer.Stop();
                this.avatarRefreshTimer.Tick -= this.AvatarRefreshTimer_Tick;
                this.avatarRefreshTimer = null;
                this.logger.Debug("Stopped avatar refresh timer");
            }
        }

        /// <summary>
        /// Handles the avatar refresh timer tick event.
        /// </summary>
        private void AvatarRefreshTimer_Tick(object? sender, EventArgs e)
        {
            // Refresh avatars for all conversations and messages
            _ = Task.Run(async () =>
            {
                try
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        // Refresh avatars for all conversations
                        foreach (var conversation in this.Conversations)
                        {
                            if (!string.IsNullOrEmpty(conversation.AvatarUrl))
                            {
                                conversation.UpdateAvatarUrl(conversation.AvatarUrl);
                            }
                        }

                        // Refresh avatars for all messages - use conversation avatar URL
                        if (this.SelectedConversation != null && !string.IsNullOrEmpty(this.SelectedConversation.AvatarUrl))
                        {
                            foreach (var messageItem in this.Messages)
                            {
                                if (messageItem is MessageViewModel messageVm &&
                                    messageVm.SenderId == this.SelectedConversation.OtherUserId)
                                {
                                    messageVm.UpdateSenderAvatarUrl(this.SelectedConversation.AvatarUrl);
                                }
                            }
                        }
                    });

                    this.logger.Debug("Periodic avatar refresh completed");
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Error during periodic avatar refresh");
                }
            });
        }

        /// <summary>
        /// Gets a preview text for a message, handling attachments and content.
        /// If the message has only attachments (content is whitespace), shows attachment names.
        /// Otherwise shows truncated content.
        /// </summary>
        /// <param name="messageDto">The message DTO to preview.</param>
        /// <param name="maxLength">Maximum length of the preview text.</param>
        /// <returns>The preview text for the message.</returns>
        private string GetMessagePreview(MessageDto messageDto, int maxLength = 50)
        {
            // Check if content is effectively empty (only whitespace)
            if (string.IsNullOrWhiteSpace(messageDto.Content))
            {
                // If there are attachments, show their names
                if (messageDto.Attachments != null && messageDto.Attachments.Count > 0)
                {
                    var attachmentNames = string.Join(", ", messageDto.Attachments.Select(a => a.FileName).Take(3));
                    if (messageDto.Attachments.Count > 3)
                    {
                        attachmentNames += $" +{messageDto.Attachments.Count - 3} more";
                    }

                    return $"📎 {attachmentNames}";
                }

                // No content and no attachments
                return "No content";
            }

            // Content exists, truncate if needed
            var trimmedContent = messageDto.Content.Trim();
            if (trimmedContent.Length <= maxLength)
            {
                return trimmedContent;
            }

            return trimmedContent.Substring(0, maxLength - 3) + "...";
        }

        private string TruncateContent(string content, int maxLength)
        {
            if (string.IsNullOrEmpty(content))
            {
                return string.Empty;
            }

            if (content.Length <= maxLength)
            {
                return content;
            }

            return content.Substring(0, maxLength - 3) + "...";
        }

        private void OnConnectionStateChanged(object? sender, bool isConnected)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                if (!isConnected)
                {
                    // Connection lost - clear all UI data
                    this.logger.Information("Connection lost, clearing all messages and conversations");
                    this.ErrorMessage = "Connection lost. Attempting to reconnect...";

                    // Clear messages and conversations from UI
                    this.Messages.Clear();
                    this.receivedMessageIds.Clear();
                    this.Conversations.Clear();
                    this.SelectedConversation = null;
                }
                else
                {
                    // Connection restored - reload all data automatically
                    this.logger.Information("Connection restored, reloading conversations and messages");
                    this.ErrorMessage = null;

                    try
                    {
                        // Reload conversations
                        await this.LoadConversationsAsync();

                        // Messages will be loaded automatically when SelectedConversation is set
                        // in LoadConversationsAsync (first available conversation will be selected)
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(ex, "Failed to reload data after reconnection");
                        this.ErrorMessage = "Reconnected, but failed to load data. Please refresh manually.";
                    }
                }
            });
        }

        /// <summary>
        /// Inserts date separators into the messages collection based on message dates.
        /// </summary>
        private void InsertDateSeparators()
        {
            // First, remove any existing date separators to avoid duplicates
            var existingSeparators = this.Messages.OfType<DateSeparatorViewModel>().ToList();
            foreach (var separator in existingSeparators)
            {
                this.Messages.Remove(separator);
            }

            DateTime? lastDate = null;
            var itemsToAdd = new List<(int Index, IMessageListItem Item)>();

            // Go through messages and identify where separators are needed
            for (int i = 0; i < this.Messages.Count; i++)
            {
                if (this.Messages[i] is MessageViewModel messageVm)
                {
                    var messageDate = DateFormatter.GetDateKey(messageVm.CreatedAt);

                    // Insert separator if this is the first message or if day changed
                    if (lastDate == null || DateFormatter.AreDifferentDays(lastDate.Value, messageDate))
                    {
                        itemsToAdd.Add((i, new DateSeparatorViewModel(messageDate)));
                        lastDate = messageDate;
                    }
                }
            }

            // Insert separators in reverse order to maintain correct indices
            foreach (var (index, item) in itemsToAdd.OrderByDescending(x => x.Index))
            {
                this.Messages.Insert(index, item);
            }
        }

        /// <summary>
        /// Inserts a date separator before a message if needed.
        /// </summary>
        /// <param name="messageDate">The date of the message to check.</param>
        /// <param name="insertIndex">The index where the message will be inserted (will be adjusted if separator is added).</param>
        /// <returns>The adjusted insert index after potential separator insertion.</returns>
        private int InsertDateSeparatorIfNeeded(DateTime messageDate, int insertIndex)
        {
            var messageDateKey = DateFormatter.GetDateKey(messageDate);

            // Find the previous message date (skip separators)
            DateTime? previousMessageDate = null;
            for (int i = insertIndex - 1; i >= 0; i--)
            {
                if (this.Messages[i] is MessageViewModel previousMessage)
                {
                    previousMessageDate = DateFormatter.GetDateKey(previousMessage.CreatedAt);
                    break;
                }
            }

            // Check if we need a separator (different day or no previous message)
            if (previousMessageDate == null || DateFormatter.AreDifferentDays(previousMessageDate.Value, messageDateKey))
            {
                // Check if there's already a separator for this date at the insert position
                bool separatorExists = false;
                if (insertIndex > 0 && this.Messages[insertIndex - 1] is DateSeparatorViewModel existingSeparator)
                {
                    var existingDate = DateFormatter.GetDateKey(existingSeparator.Date);
                    if (existingDate == messageDateKey)
                    {
                        separatorExists = true;
                    }
                }

                if (!separatorExists)
                {
                    this.Messages.Insert(insertIndex, new DateSeparatorViewModel(messageDateKey));
                    return insertIndex + 1; // Return adjusted index
                }
            }

            return insertIndex;
        }

        private void CalculateProfileStatistics()
        {
            var messageViewModels = this.GetMessageViewModels();
            var allAttachments = messageViewModels
                .SelectMany(m => m.Attachments)
                .ToList();

            this.FilesCount = allAttachments.Count(a => a.AttachmentType == Shared.Enums.AttachmentType.Document ||
                                                        a.AttachmentType == Shared.Enums.AttachmentType.Archive ||
                                                        a.AttachmentType == Shared.Enums.AttachmentType.Code ||
                                                        a.AttachmentType == Shared.Enums.AttachmentType.Audio);
            this.ImagesCount = allAttachments.Count(a => a.AttachmentType == Shared.Enums.AttachmentType.Image);
            this.VideosCount = allAttachments.Count(a => a.AttachmentType == Shared.Enums.AttachmentType.Video);
        }

        private void PopulateFilesList()
        {
            var messageViewModels = this.GetMessageViewModels();
            this.logger.Debug("Populating files list from {Count} messages", messageViewModels.Count());

            var files = messageViewModels
                .SelectMany(m => m.Attachments)
                .Where(a => a.AttachmentType == Shared.Enums.AttachmentType.Document ||
                           a.AttachmentType == Shared.Enums.AttachmentType.Archive ||
                           a.AttachmentType == Shared.Enums.AttachmentType.Code ||
                           a.AttachmentType == Shared.Enums.AttachmentType.Audio)
                .OrderByDescending(a => a.AttachmentDto?.UploadedAt)
                .ToList();

            this.logger.Debug("Found {Count} files to add to list", files.Count);

            this.filesList.Clear();
            foreach (var file in files)
            {
                this.filesList.Add(file);
            }

            this.logger.Debug("Files list populated with {Count} items", this.filesList.Count);
        }

        private void PopulateImagesList()
        {
            var messageViewModels = this.GetMessageViewModels();
            this.logger.Debug("Populating images list from {Count} messages", messageViewModels.Count());

            var images = messageViewModels
                .SelectMany(m => m.Attachments)
                .Where(a => a.AttachmentType == Shared.Enums.AttachmentType.Image)
                .OrderByDescending(a => a.AttachmentDto?.UploadedAt)
                .ToList();

            this.logger.Debug("Found {Count} images to add to list", images.Count);

            this.imagesList.Clear();
            foreach (var image in images)
            {
                this.imagesList.Add(image);
            }

            this.logger.Debug("Images list populated with {Count} items", this.imagesList.Count);
        }

        private void PopulateProfileImages()
        {
            // Populate images list for profile display
            // Thumbnails will be loaded automatically by AttachmentViewModel constructor via LoadThumbnailFromService
            this.PopulateImagesList();
        }

        private void PopulateProfileFiles()
        {
            // Populate files list for profile display
            // Thumbnails will be loaded automatically by AttachmentViewModel constructor via LoadThumbnailFromService
            this.PopulateFilesList();
        }

        private async void UpdateProfileData()
        {
            if (this.SelectedConversation == null)
            {
                return;
            }

            try
            {
                // Get all chats and find the one we need
                var chats = await this.messagingService.GetChatsAsync();
                var chat = chats.FirstOrDefault(c => c.Id == this.SelectedConversation.Id);

                if (chat == null)
                {
                    this.ProfileEmail = string.Empty;
                    this.ProfileUsername = string.Empty;
                    return;
                }

                var currentUserId = this.authenticationService.CurrentUser?.Id ?? string.Empty;

                // Check if this is a group chat or channel
                var isGroupChat = chat.Type == ChatType.Group || chat.Type == ChatType.Channel;

                if (isGroupChat)
                {
                    // For group chats, display the chat name
                    this.ProfileDisplayName = !string.IsNullOrWhiteSpace(chat.Name) ? chat.Name : this.SelectedConversation.Name;
                    this.ProfileEmail = string.Empty;
                    this.ProfileUsername = string.Empty;
                    this.ProfileStatus = UserStatus.Offline;
                    this.ProfileLastSeenAt = null;
                    this.OnPropertyChanged(nameof(this.ProfileStatusText));

                    // Load chat avatar
                    _ = this.LoadProfileAvatarAsync(chat.AvatarUrl);

                    // Load and update participants list
                    this.logger.Debug("Loading participants for group chat {ChatId}, Participants from server: {Count}", chat.Id, chat.Participants?.Count ?? 0);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        this.GroupChatParticipants.Clear();
                        if (chat.Participants != null && chat.Participants.Any())
                        {
                            foreach (var participant in chat.Participants)
                            {
                                this.GroupChatParticipants.Add(participant);
                            }

                            this.logger.Debug("Loaded {Count} participants for group chat {ChatId} into UI", chat.Participants.Count, chat.Id);
                            this.OnPropertyChanged(nameof(this.GroupChatParticipants));
                        }
                        else
                        {
                            this.logger.Warning("No participants found for group chat {ChatId}", chat.Id);
                        }
                    });

                    // Also update the conversation's participants list
                    if (chat.Participants != null && chat.Participants.Any())
                    {
                        this.SelectedConversation.UpdateParticipants(chat.Participants);
                    }
                }
                else
                {
                    // For direct messages, display the other user's information
                    var otherUser = chat.Participants?.FirstOrDefault(p => p.Id != currentUserId);
                    if (otherUser != null)
                    {
                        this.ProfileEmail = otherUser.Email;
                        this.ProfileUsername = otherUser.Username;
                        this.ProfileDisplayName = !string.IsNullOrWhiteSpace(otherUser.DisplayName) ? otherUser.DisplayName : otherUser.Username;
                        this.ProfileStatus = otherUser.Status;
                        this.ProfileLastSeenAt = otherUser.LastSeenAt;
                        this.OnPropertyChanged(nameof(this.ProfileStatusText));

                        // Load avatar
                        _ = this.LoadProfileAvatarAsync(otherUser.AvatarUrl);
                    }
                    else
                    {
                        this.ProfileEmail = string.Empty;
                        this.ProfileUsername = string.Empty;
                        this.ProfileDisplayName = string.Empty;
                        this.ProfileStatus = UserStatus.Offline;
                        this.ProfileLastSeenAt = null;
                        this.ProfileAvatarImage = null;
                        this.OnPropertyChanged(nameof(this.ProfileStatusText));
                    }
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load profile data");
                this.ProfileEmail = string.Empty;
                this.ProfileUsername = string.Empty;
                this.ProfileAvatarImage = null;
            }
        }

        private async Task LoadProfileAvatarAsync(string? avatarUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    this.ProfileAvatarImage = this.avatarService.GetDefaultAvatar();
                    return;
                }

                var separator = avatarUrl.Contains('?') ? "&" : "?";
                var cacheBustingUrl = $"{avatarUrl}{separator}t={DateTime.UtcNow.Ticks}";
                this.ProfileAvatarImage = await this.avatarService.LoadAvatarAsync(cacheBustingUrl);
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load profile avatar");
                this.ProfileAvatarImage = this.avatarService.GetDefaultAvatar();
            }
        }

        /// <summary>
        /// Gets all message view models from the messages collection.
        /// </summary>
        /// <returns>Collection of message view models.</returns>
        private IEnumerable<MessageViewModel> GetMessageViewModels()
        {
            return this.Messages.OfType<MessageViewModel>();
        }

        /// <summary>
        /// Loads all messages for the selected conversation to ensure all attachments are available.
        /// </summary>
        private async Task LoadAllMessagesForAttachmentsAsync()
        {
            if (this.SelectedConversation == null)
            {
                return;
            }

            try
            {
                this.logger.Debug("Loading all messages for attachments in chat {ChatId}", this.SelectedConversation.Id);

                // Load all messages with pagination to ensure we get everything
                var allMessages = new List<MessageDto>();
                const int pageSize = 1000;
                int offset = 0;
                bool hasMore = true;

                while (hasMore)
                {
                    var pageMessages = await this.messagingService.GetMessageHistoryAsync(
                        this.SelectedConversation.Id,
                        limit: pageSize,
                        offset: offset);

                    if (pageMessages == null || pageMessages.Count == 0)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        allMessages.AddRange(pageMessages);
                        offset += pageMessages.Count;

                        // If we got fewer messages than requested, we've reached the end
                        if (pageMessages.Count < pageSize)
                        {
                            hasMore = false;
                        }
                    }
                }

                this.logger.Debug("Received {Count} total messages from server", allMessages.Count);

                var currentUserId = this.authenticationService.CurrentUser?.Id ?? string.Empty;

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // Store existing message IDs to avoid duplicates
                    var existingMessageIds = new HashSet<string>(
                        this.GetMessageViewModels().Select(m => m.Id));

                    this.logger.Debug("Existing messages in collection: {Count}", existingMessageIds.Count);
                    var addedCount = 0;

                    // Add only new messages
                    foreach (var message in allMessages.OrderBy(m => m.CreatedAt))
                    {
                        if (!existingMessageIds.Contains(message.Id))
                        {
                            // Get sender info from selected conversation if available
                            string? senderName = null;
                            string? senderAvatarUrl = null;
                            var isGroupChat = this.SelectedConversation.Type == ChatType.Group || this.SelectedConversation.Type == ChatType.Channel;

                            // For group chats, get from participants list for other users only
                            if (isGroupChat && message.SenderId != currentUserId)
                            {
                                senderName = this.SelectedConversation.GetParticipantName(message.SenderId);
                                senderAvatarUrl = this.SelectedConversation.GetParticipantAvatarUrl(message.SenderId);

                                // Fallback: if participant not found, try users cache
                                if (string.IsNullOrEmpty(senderName) && this.usersCache.TryGetValue(message.SenderId, out var cachedUser))
                                {
                                    senderName = !string.IsNullOrWhiteSpace(cachedUser.DisplayName) ? cachedUser.DisplayName : cachedUser.Username;
                                    senderAvatarUrl = cachedUser.AvatarUrl;
                                    this.logger.Debug("Found participant {SenderId} in users cache", message.SenderId);
                                }
                                else if (string.IsNullOrEmpty(senderName))
                                {
                                    this.logger.Debug("Participant {SenderId} not found in participants list (Count: {Count}) or cache for chat {ChatId}", message.SenderId, this.SelectedConversation.Participants.Count, this.SelectedConversation.Id);
                                }
                            }

                            // For direct messages and own messages in group chats, don't set name (won't be displayed)
                            this.Messages.Add(new MessageViewModel(
                                message,
                                currentUserId,
                                this.fileAttachmentService,
                                this.avatarService,
                                senderName,
                                senderAvatarUrl,
                                isGroupChat));

                            existingMessageIds.Add(message.Id);
                            addedCount++;
                        }
                    }

                    this.logger.Debug("Added {Count} new messages. Total messages in collection: {Total}", addedCount, this.GetMessageViewModels().Count());

                    // Count attachments for debugging
                    var totalAttachments = this.GetMessageViewModels()
                        .SelectMany(m => m.Attachments)
                        .Count();
                    var imageAttachments = this.GetMessageViewModels()
                        .SelectMany(m => m.Attachments)
                        .Count(a => a.AttachmentType == Shared.Enums.AttachmentType.Image);
                    var fileAttachments = this.GetMessageViewModels()
                        .SelectMany(m => m.Attachments)
                        .Count(a => a.AttachmentType == Shared.Enums.AttachmentType.Document ||
                                   a.AttachmentType == Shared.Enums.AttachmentType.Archive ||
                                   a.AttachmentType == Shared.Enums.AttachmentType.Code ||
                                   a.AttachmentType == Shared.Enums.AttachmentType.Audio);

                    this.logger.Debug("Total attachments: {Total}, Images: {Images}, Files: {Files}", totalAttachments, imageAttachments, fileAttachments);

                    // Re-insert date separators after adding new messages
                    this.InsertDateSeparators();
                });
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load all messages for attachments");
                this.errorHandlingService.ShowError("Failed to load all messages. Some attachments may be missing.");
            }
        }

        /// <summary>
        /// Finds a message view model by ID.
        /// </summary>
        /// <param name="messageId">The message ID to find.</param>
        /// <returns>The message view model if found, null otherwise.</returns>
        private MessageViewModel? FindMessageViewModel(string messageId)
        {
            return this.GetMessageViewModels().FirstOrDefault(m => m.Id == messageId);
        }
    }
}
