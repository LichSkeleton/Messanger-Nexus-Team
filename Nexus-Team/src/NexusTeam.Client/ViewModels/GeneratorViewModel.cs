namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Client.Services;
    using NexusTeam.Client.Views;
    using NexusTeam.Shared.Dtos;
    using Serilog;

    /// <summary>
    /// View model for the image generator view.
    /// </summary>
    public partial class GeneratorViewModel : ViewModelBase
    {
        private readonly INavigationService navigationService;
        private readonly IImageGeneratorService imageGeneratorService;
        private readonly IMessagingService messagingService;
        private readonly IFileAttachmentService fileAttachmentService;
        private readonly IErrorHandlingService errorHandlingService;
        private readonly ILogger logger;
        private readonly DispatcherTimer timeUpdateTimer;

        private string promptText = string.Empty;
        private string selectedModel = "flux";
        private BitmapImage? currentImage;
        private byte[]? currentImageData;
        private GeneratedImageDto? currentGeneratedImage;
        private bool isGenerating;
        private bool hasCurrentImage;
        private string generatedTimeAgo = string.Empty;
        private DateTime? generatedAt;
        private int characterCount;
        private bool isPromptsDropdownOpen;
        private bool isSendToChatDialogOpen;
        private bool isModelDropdownOpen;
        private string generationStatus = string.Empty;
        private int cooldownSeconds;
        private System.Windows.Threading.DispatcherTimer? cooldownTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratorViewModel"/> class.
        /// </summary>
        /// <param name="navigationService">The navigation service.</param>
        /// <param name="imageGeneratorService">The image generator service.</param>
        /// <param name="messagingService">The messaging service.</param>
        /// <param name="fileAttachmentService">The file attachment service.</param>
        /// <param name="errorHandlingService">The error handling service.</param>
        /// <param name="logger">The logger.</param>
        public GeneratorViewModel(
            INavigationService navigationService,
            IImageGeneratorService imageGeneratorService,
            IMessagingService messagingService,
            IFileAttachmentService fileAttachmentService,
            IErrorHandlingService errorHandlingService,
            ILogger logger)
        {
            this.navigationService = navigationService;
            this.imageGeneratorService = imageGeneratorService;
            this.messagingService = messagingService;
            this.fileAttachmentService = fileAttachmentService;
            this.errorHandlingService = errorHandlingService;
            this.logger = logger;

            this.RecentPrompts = new ObservableCollection<string>();
            this.AvailableModels = new ObservableCollection<ModelOption>
            {
                new ModelOption { Id = "flux", Name = "Flux", Description = "High quality, default" },
                new ModelOption { Id = "turbo", Name = "Turbo", Description = "Fast generation" },
            };
            this.Conversations = new ObservableCollection<SelectableConversationViewModel>();

            // Timer for updating "time ago" display
            this.timeUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1),
            };
            this.timeUpdateTimer.Tick += (s, e) => this.UpdateTimeAgo();
        }

        /// <summary>
        /// Gets the available models.
        /// </summary>
        public ObservableCollection<ModelOption> AvailableModels { get; }

        /// <summary>
        /// Gets the recent prompts.
        /// </summary>
        public ObservableCollection<string> RecentPrompts { get; }

        /// <summary>
        /// Gets the conversations for send-to dialog.
        /// </summary>
        public ObservableCollection<SelectableConversationViewModel> Conversations { get; }

        /// <summary>
        /// Gets or sets the prompt text.
        /// </summary>
        public string PromptText
        {
            get => this.promptText;
            set
            {
                if (this.SetProperty(ref this.promptText, value))
                {
                    this.CharacterCount = value?.Length ?? 0;
                    this.GenerateCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected model.
        /// </summary>
        public string SelectedModel
        {
            get => this.selectedModel;
            set => this.SetProperty(ref this.selectedModel, value);
        }

        /// <summary>
        /// Gets the selected model display name.
        /// </summary>
        public string SelectedModelName => this.AvailableModels.FirstOrDefault(m => m.Id == this.SelectedModel)?.Name ?? "Flux";

        /// <summary>
        /// Gets or sets the current generated image.
        /// </summary>
        public BitmapImage? CurrentImage
        {
            get => this.currentImage;
            set => this.SetProperty(ref this.currentImage, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether generation is in progress.
        /// </summary>
        public bool IsGenerating
        {
            get => this.isGenerating;
            set
            {
                if (this.SetProperty(ref this.isGenerating, value))
                {
                    this.GenerateCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether there is a current image.
        /// </summary>
        public bool HasCurrentImage
        {
            get => this.hasCurrentImage;
            set => this.SetProperty(ref this.hasCurrentImage, value);
        }

        /// <summary>
        /// Gets or sets the generated time ago string.
        /// </summary>
        public string GeneratedTimeAgo
        {
            get => this.generatedTimeAgo;
            set => this.SetProperty(ref this.generatedTimeAgo, value);
        }

        /// <summary>
        /// Gets or sets the character count.
        /// </summary>
        public int CharacterCount
        {
            get => this.characterCount;
            set => this.SetProperty(ref this.characterCount, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether prompts dropdown is open.
        /// </summary>
        public bool IsPromptsDropdownOpen
        {
            get => this.isPromptsDropdownOpen;
            set => this.SetProperty(ref this.isPromptsDropdownOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether send-to-chat dialog is open.
        /// </summary>
        public bool IsSendToChatDialogOpen
        {
            get => this.isSendToChatDialogOpen;
            set => this.SetProperty(ref this.isSendToChatDialogOpen, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether model dropdown is open.
        /// </summary>
        public bool IsModelDropdownOpen
        {
            get => this.isModelDropdownOpen;
            set => this.SetProperty(ref this.isModelDropdownOpen, value);
        }

        /// <summary>
        /// Gets or sets the generation status message.
        /// </summary>
        public string GenerationStatus
        {
            get => this.generationStatus;
            set => this.SetProperty(ref this.generationStatus, value);
        }

        /// <summary>
        /// Gets or sets the cooldown seconds remaining.
        /// </summary>
        public int CooldownSeconds
        {
            get => this.cooldownSeconds;
            set
            {
                if (this.SetProperty(ref this.cooldownSeconds, value))
                {
                    this.OnPropertyChanged(nameof(this.IsOnCooldown));
                    this.OnPropertyChanged(nameof(this.CooldownText));
                    this.GenerateCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether generation is on cooldown.
        /// </summary>
        public bool IsOnCooldown => this.CooldownSeconds > 0;

        /// <summary>
        /// Gets the cooldown text to display.
        /// </summary>
        public string CooldownText => this.IsOnCooldown ? $"Wait {this.CooldownSeconds}s" : "GENERATE";

        /// <inheritdoc/>
        public override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
            _ = this.LoadRecentPromptsAsync();
            this.timeUpdateTimer.Start();
        }

        /// <inheritdoc/>
        public override void OnNavigatedFrom()
        {
            this.timeUpdateTimer.Stop();
            base.OnNavigatedFrom();
        }

        /// <summary>
        /// Command to navigate back.
        /// </summary>
        [RelayCommand]
        private void NavigateBack()
        {
            this.navigationService.NavigateBack();
        }

        /// <summary>
        /// Command to generate an image.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanGenerate))]
        private async Task GenerateAsync()
        {
            if (string.IsNullOrWhiteSpace(this.PromptText))
            {
                return;
            }

            // Remove previous generated image locally and on server before a new generation
            await this.DeleteCurrentImageAsync(refreshPrompts: false);

            this.IsGenerating = true;
            this.GenerationStatus = $"Generating with {this.SelectedModelName}...";

            try
            {
                var (image, imageData, imageUrl) = await this.imageGeneratorService.GenerateImageAsync(
                    this.PromptText,
                    this.SelectedModel,
                    1024,
                    1024);

                this.GenerationStatus = "Saving image...";

                this.CurrentImage = image;
                this.currentImageData = imageData;
                this.HasCurrentImage = true;
                this.generatedAt = DateTime.UtcNow;
                this.UpdateTimeAgo();

                // Save to server
                this.currentGeneratedImage = await this.imageGeneratorService.SaveGeneratedImageAsync(
                    this.PromptText,
                    this.SelectedModel,
                    imageUrl,
                    imageData,
                    1024,
                    1024);

                // Refresh recent prompts
                await this.LoadRecentPromptsAsync();

                this.GenerationStatus = string.Empty;
                this.logger.Information("Image generated successfully: {Prompt}", this.PromptText);

                // Start 15 second cooldown after successful generation
                this.StartCooldown();
            }
            catch (Exception ex)
            {
                this.GenerationStatus = string.Empty;
                this.logger.Error(ex, "Failed to generate image");

                // User-friendly error messages
                var message = ex.Message;
                if (message.Contains("429") || message.Contains("Rate limit"))
                {
                    message = "Rate limit reached. Please wait 15 seconds between generations.";
                }
                else if (message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    message = "Generation timed out. Try a simpler prompt or try again later.";
                }

                this.errorHandlingService.ShowError(message);
            }
            finally
            {
                this.IsGenerating = false;
            }
        }

        private bool CanGenerate() => !this.IsGenerating && !this.IsOnCooldown && !string.IsNullOrWhiteSpace(this.PromptText);

        /// <summary>
        /// Starts the cooldown timer after generation.
        /// </summary>
        private void StartCooldown()
        {
            this.CooldownSeconds = 15;

            this.cooldownTimer?.Stop();
            this.cooldownTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1),
            };
            this.cooldownTimer.Tick += (s, e) =>
            {
                this.CooldownSeconds--;
                if (this.CooldownSeconds <= 0)
                {
                    this.cooldownTimer?.Stop();
                }
            };
            this.cooldownTimer.Start();
        }

        /// <summary>
        /// Command to save the current image.
        /// </summary>
        [RelayCommand]
        private async Task SaveImageAsync()
        {
            if (this.currentImageData == null)
            {
                return;
            }

            try
            {
                var fileName = $"generated_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                var savedPath = await this.imageGeneratorService.SaveImageToFileAsync(this.currentImageData, fileName);

                if (!string.IsNullOrEmpty(savedPath))
                {
                    this.errorHandlingService.ShowInfo($"Image saved to: {savedPath}");
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to save image");
                this.errorHandlingService.ShowError($"Failed to save image: {ex.Message}");
            }
        }

        /// <summary>
        /// Command to open send-to-chat dialog.
        /// </summary>
        [RelayCommand]
        private async Task OpenSendToDialogAsync()
        {
            if (!this.HasCurrentImage)
            {
                return;
            }

            try
            {
                // Load conversations
                var chats = await this.messagingService.GetChatsAsync();
                this.Conversations.Clear();

                foreach (var chat in chats)
                {
                    this.Conversations.Add(new SelectableConversationViewModel(chat));
                }

                this.IsSendToChatDialogOpen = true;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load chats");
                this.errorHandlingService.ShowError("Failed to load chats");
            }
        }

        /// <summary>
        /// Command to close send-to-chat dialog.
        /// </summary>
        [RelayCommand]
        private void CloseSendToDialog()
        {
            this.IsSendToChatDialogOpen = false;
        }

        /// <summary>
        /// Command to send image to selected chats.
        /// </summary>
        [RelayCommand]
        private async Task SendToChatsAsync()
        {
            if (this.currentImageData == null)
            {
                return;
            }

            var selectedChats = this.Conversations.Where(c => c.IsSelected).ToList();
            if (!selectedChats.Any())
            {
                this.errorHandlingService.ShowWarning("Please select at least one chat");
                return;
            }

            try
            {
                // Save image temporarily
                var tempPath = System.IO.Path.Combine(
                    System.IO.Path.GetTempPath(),
                    $"generated_{Guid.NewGuid()}.png");
                System.IO.File.WriteAllBytes(tempPath, this.currentImageData);

                foreach (var chat in selectedChats)
                {
                    try
                    {
                        // 1. First send message to get real message ID
                        var messageDto = await this.messagingService.SendMessageViaHttpAsync(
                            chat.Id,
                            "🎨 Generated Image",
                            null,
                            new List<string>());

                        // 2. Then upload attachment with the real message ID
                        await this.fileAttachmentService.UploadAttachmentAsync(
                            tempPath,
                            messageDto.Id);

                        this.logger.Information("Image sent to chat: {ChatId}, messageId: {MessageId}", chat.Id, messageDto.Id);
                    }
                    catch (Exception ex)
                    {
                        this.logger.Error(ex, "Failed to send image to chat: {ChatId}", chat.Id);
                    }
                }

                // Cleanup temp file
                try
                {
                    System.IO.File.Delete(tempPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                this.IsSendToChatDialogOpen = false;
                this.errorHandlingService.ShowInfo($"Image sent to {selectedChats.Count} chat(s)");
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to send image");
                this.errorHandlingService.ShowError("Failed to send image");
            }
        }

        /// <summary>
        /// Command to delete the current image.
        /// </summary>
        [RelayCommand]
        private async Task DeleteImageAsync()
        {
            await this.DeleteCurrentImageAsync(refreshPrompts: true);
        }

        /// <summary>
        /// Command to view the current image in full screen.
        /// </summary>
        [RelayCommand]
        private void ViewImage()
        {
            if (this.CurrentImage == null)
            {
                return;
            }

            try
            {
                var dialog = new ImageViewerDialog(this.CurrentImage)
                {
                    Owner = Application.Current.MainWindow,
                };
                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to view image");
            }
        }

        private async Task DeleteCurrentImageAsync(bool refreshPrompts)
        {
            // Clear local UI state immediately
            this.CurrentImage = null;
            this.currentImageData = null;
            this.HasCurrentImage = false;
            this.GeneratedTimeAgo = string.Empty;
            this.generatedAt = null;

            // Remember id to try delete on server after local clear
            var previousId = this.currentGeneratedImage?.Id;
            this.currentGeneratedImage = null;

            if (!string.IsNullOrEmpty(previousId))
            {
                try
                {
                    await this.imageGeneratorService.DeleteGeneratedImageAsync(previousId);
                }
                catch (Exception ex)
                {
                    this.logger.Warning(ex, "Failed to delete previous generated image");
                }
            }

            if (refreshPrompts)
            {
                await this.LoadRecentPromptsAsync();
            }
        }

        /// <summary>
        /// Command to select a recent prompt.
        /// </summary>
        [RelayCommand]
        private void SelectPrompt(string prompt)
        {
            if (!string.IsNullOrEmpty(prompt))
            {
                // Add prompt to existing text, separated by space if there's existing text
                if (string.IsNullOrWhiteSpace(this.PromptText))
                {
                    this.PromptText = prompt;
                }
                else
                {
                    this.PromptText = this.PromptText.TrimEnd() + " " + prompt;
                }

                this.IsPromptsDropdownOpen = false;
            }
        }

        /// <summary>
        /// Command to toggle prompts dropdown.
        /// </summary>
        [RelayCommand]
        private void TogglePromptsDropdown()
        {
            this.IsPromptsDropdownOpen = !this.IsPromptsDropdownOpen;
        }

        /// <summary>
        /// Command to select a model.
        /// </summary>
        [RelayCommand]
        private void SelectModel(string modelId)
        {
            this.SelectedModel = modelId;
            this.IsModelDropdownOpen = false;
            this.OnPropertyChanged(nameof(this.SelectedModelName));
        }

        /// <summary>
        /// Command to toggle model dropdown.
        /// </summary>
        [RelayCommand]
        private void ToggleModelDropdown()
        {
            this.IsModelDropdownOpen = !this.IsModelDropdownOpen;
        }

        private async Task LoadRecentPromptsAsync()
        {
            try
            {
                var prompts = await this.imageGeneratorService.GetRecentPromptsAsync(limit: 100);
                this.RecentPrompts.Clear();
                foreach (var prompt in prompts)
                {
                    this.RecentPrompts.Add(prompt);
                }
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Failed to load recent prompts");
            }
        }

        private void UpdateTimeAgo()
        {
            if (!this.generatedAt.HasValue)
            {
                this.GeneratedTimeAgo = string.Empty;
                return;
            }

            var diff = DateTime.UtcNow - this.generatedAt.Value;
            var minutes = (int)diff.TotalMinutes;

            this.GeneratedTimeAgo = minutes switch
            {
                0 => "Just now",
                1 => "1 min ago",
                _ when minutes < 60 => $"{minutes} min ago",
                _ when minutes < 1440 => $"{minutes / 60} h ago",
                _ => $"{minutes / 1440} d ago",
            };
        }
    }

    /// <summary>
    /// Model option for dropdown.
    /// </summary>
    public class ModelOption
    {
        /// <summary>
        /// Gets or sets the model ID.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Selectable conversation for send-to dialog.
    /// </summary>
    public class SelectableConversationViewModel : ObservableObject
    {
        private bool isSelected;

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectableConversationViewModel"/> class.
        /// </summary>
        /// <param name="chat">The chat DTO.</param>
        public SelectableConversationViewModel(ChatDto chat)
        {
            this.Id = chat.Id;
            this.Name = chat.Name ?? "Chat";
        }

        /// <summary>
        /// Gets the chat ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the chat name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the chat is selected.
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }
    }
}
