namespace NexusTeam.Client.Views
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Effects;
    using System.Windows.Threading;
    using Emoji.Wpf;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Logic usability with ChatView.xaml.
    /// </summary>
    public partial class ChatView : UserControl
    {
        private const double ScrollThreshold = 50.0;
        private const int SmoothScrollDurationMs = 300;
        private DispatcherTimer? smoothScrollTimer;
        private double smoothScrollTarget;
        private double smoothScrollStart;
        private DateTime smoothScrollStartTime;
        private string? currentAudioTempPath;
        private AttachmentViewModel? currentPlayingAttachment;
        private System.Windows.Controls.Primitives.Popup? currentReactionPopup;
        private System.Windows.Controls.Primitives.Popup? currentTranslatePopup;
        private bool isUpdatingFromViewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatView"/> class.
        /// </summary>
        public ChatView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.OnDataContextChanged;
            this.Loaded += this.ChatView_Loaded;
        }

        private void ChatView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.SetupEmojiPickerBinding();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ChatView_Loaded: {ex.Message}");
            }
        }

        private void EditFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is ChatFolderViewModel folder)
            {
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    viewModel.EditFolderCommand.Execute(folder);
                }
            }
        }

        private void DeleteFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is ChatFolderViewModel folder)
            {
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    viewModel.DeleteFolderCommand.Execute(folder);
                }
            }
        }

        private void MainContentGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.FolderRailColumn == null || this.ChatListColumn == null || this.MessagesColumn == null)
            {
                return;
            }

            var width = e.NewSize.Width;
            if (width < 780)
            {
                this.FolderRailColumn.Width = new GridLength(68);
                this.ChatListColumn.MinWidth = 140;
                this.MessagesColumn.MinWidth = 200;
            }
            else
            {
                this.FolderRailColumn.Width = new GridLength(80);
                this.ChatListColumn.MinWidth = 160;
                this.MessagesColumn.MinWidth = 240;
            }
        }

        private void ConversationContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is not ContextMenu menu)
            {
                return;
            }

            var conversation = menu.PlacementTarget is FrameworkElement fe
                ? fe.DataContext as ConversationViewModel
                : null;
            var chatVm = this.DataContext as ChatViewModel;
            if (conversation == null || chatVm == null)
            {
                return;
            }

            MenuItem? editItem = null;
            MenuItem? leaveItem = null;
            MenuItem? addToFolderItem = null;
            MenuItem? deleteItem = null;

            foreach (var item in menu.Items)
            {
                if (item is MenuItem mi)
                {
                    if (mi.Header as string == "Edit Group")
                    {
                        editItem = mi;
                    }
                    else if (mi.Header as string == "Leave Group")
                    {
                        leaveItem = mi;
                    }
                    else if (mi.Header as string == "Add to Folder")
                    {
                        addToFolderItem = mi;
                    }
                    else if (mi.Tag as string == "DeleteChat")
                    {
                        deleteItem = mi;
                    }
                }
            }

            if (editItem != null)
            {
                editItem.Visibility = conversation.IsOwner ? Visibility.Visible : Visibility.Collapsed;
                editItem.Tag = conversation;
            }

            if (leaveItem != null)
            {
                leaveItem.Visibility = conversation.IsGroup ? Visibility.Visible : Visibility.Collapsed;
                leaveItem.Tag = conversation;
            }

            if (deleteItem != null)
            {
                deleteItem.Visibility = conversation.CanDeleteChat ? Visibility.Visible : Visibility.Collapsed;
            }

            if (addToFolderItem != null)
            {
                var personalFolders = chatVm.Folders.Where(f => !f.IsAllChatsFolder).ToList();
                addToFolderItem.Items.Clear();
                addToFolderItem.Visibility = personalFolders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

                foreach (var folder in personalFolders)
                {
                    var inFolder = folder.ChatIds.Contains(conversation.Id);
                    var folderItem = new MenuItem
                    {
                        Header = inFolder ? $"✓ {folder.Name}" : folder.Name,
                        Tag = new object[] { conversation, folder },
                        Foreground = System.Windows.Media.Brushes.White,
                    };
                    folderItem.Click += this.FolderAssignmentMenuItem_Click;
                    addToFolderItem.Items.Add(folderItem);
                }
            }
        }

        private void EditGroupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Tag: ConversationViewModel conversation }
                && this.DataContext is ChatViewModel viewModel)
            {
                viewModel.EditGroupCommand.Execute(conversation);
            }
        }

        private void LeaveGroupMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Tag: ConversationViewModel conversation }
                && this.DataContext is ChatViewModel viewModel)
            {
                viewModel.LeaveGroupCommand.Execute(conversation);
            }
        }

        private void FolderAssignmentMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem { Tag: object[] args } || args.Length < 2
                || this.DataContext is not ChatViewModel viewModel)
            {
                return;
            }

            if (args[0] is not ConversationViewModel conversation
                || args[1] is not ChatFolderViewModel folder)
            {
                return;
            }

            if (folder.ChatIds.Contains(conversation.Id))
            {
                viewModel.RemoveChatFromFolderCommand.Execute(args);
            }
            else
            {
                viewModel.AddChatToFolderCommand.Execute(args);
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is ChatViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= this.ViewModel_PropertyChanged;
                oldViewModel.Messages.CollectionChanged -= this.Messages_CollectionChanged;
                oldViewModel.OnAudioPlayRequested -= this.ViewModel_OnAudioPlayRequested;
                oldViewModel.OnAudioPlayPauseRequested -= this.ViewModel_OnAudioPlayPauseRequested;
                oldViewModel.OnAudioStopRequested -= this.ViewModel_OnAudioStopRequested;
            }

            if (e.NewValue is ChatViewModel newViewModel)
            {
                newViewModel.PropertyChanged += this.ViewModel_PropertyChanged;
                newViewModel.Messages.CollectionChanged += this.Messages_CollectionChanged;
                newViewModel.OnAudioPlayRequested += this.ViewModel_OnAudioPlayRequested;
                newViewModel.OnAudioPlayPauseRequested += this.ViewModel_OnAudioPlayPauseRequested;
                newViewModel.OnAudioStopRequested += this.ViewModel_OnAudioStopRequested;

                // Subscribe to MessageText changes to update RichTextBox
                newViewModel.PropertyChanged += (s, args) =>
                {
                    if (args.PropertyName == nameof(ChatViewModel.MessageText))
                    {
                        this.UpdateRichTextBoxFromViewModel();
                        this.UpdateRichTextBox2FromViewModel();
                    }
                };
            }
        }

        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChatViewModel.IsLoadingMessages))
            {
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null && !viewModel.IsLoadingMessages)
                {
                    // Messages just finished loading, scroll to bottom
                    this.ScrollToBottom();
                }
            }
            else if (e.PropertyName == nameof(ChatViewModel.IsEmojiPickerOpen))
            {
                try
                {
                    var viewModel = this.DataContext as ChatViewModel;
                    if (this.EmojiPickerPopup != null && viewModel != null)
                    {
                        // Ensure PlacementTarget is set before opening
                        if (viewModel.IsEmojiPickerOpen)
                        {
                            var emojiButtonContainer = this.FindName("EmojiButtonContainer") as FrameworkElement;
                            if (emojiButtonContainer != null)
                            {
                                this.EmojiPickerPopup.PlacementTarget = emojiButtonContainer;
                            }
                        }

                        // Only update if different to avoid circular updates
                        if (this.EmojiPickerPopup.IsOpen != viewModel.IsEmojiPickerOpen)
                        {
                            this.EmojiPickerPopup.IsOpen = viewModel.IsEmojiPickerOpen;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in ViewModel_PropertyChanged for IsEmojiPickerOpen: {ex.Message}");
                }
            }
        }

        private void Messages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var viewModel = this.DataContext as ChatViewModel;
            if (viewModel != null && viewModel.IsLoadingMessages)
            {
                return;
            }

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                // New message added, scroll to bottom
                this.ScrollToBottom();
            }
        }

        private void ScrollToBottom()
        {
            Application.Current.Dispatcher.InvokeAsync(
                () =>
                {
                    this.MessagesScrollViewer.ScrollToBottom();

                    this.UpdateScrollButtonVisibility();
                },
                System.Windows.Threading.DispatcherPriority.Background);
        }

        private void MessagesScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            this.UpdateScrollButtonVisibility();
        }

        private void UpdateScrollButtonVisibility()
        {
            if (this.MessagesScrollViewer == null)
            {
                return;
            }

            var verticalOffset = this.MessagesScrollViewer.VerticalOffset;
            var scrollableHeight = this.MessagesScrollViewer.ScrollableHeight;
            var isAtBottom = scrollableHeight - verticalOffset <= ScrollThreshold;

            this.ScrollToBottomButton.Visibility = (!isAtBottom && scrollableHeight > 0)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ScrollToBottomButton_Click(object sender, RoutedEventArgs e)
        {
            this.SmoothScrollToBottom();
        }

        private void SmoothScrollToBottom()
        {
            if (this.MessagesScrollViewer == null)
            {
                return;
            }

            if (this.smoothScrollTimer != null)
            {
                this.smoothScrollTimer.Stop();
                this.smoothScrollTimer = null;
            }

            this.smoothScrollTarget = this.MessagesScrollViewer.ScrollableHeight;
            this.smoothScrollStart = this.MessagesScrollViewer.VerticalOffset;
            this.smoothScrollStartTime = DateTime.Now;

            if (Math.Abs(this.smoothScrollTarget - this.smoothScrollStart) < 1.0)
            {
                this.UpdateScrollButtonVisibility();
                return;
            }

            this.smoothScrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16), // ~60 FPS
            };
            this.smoothScrollTimer.Tick += this.SmoothScrollTimer_Tick;
            this.smoothScrollTimer.Start();
        }

        private void SmoothScrollTimer_Tick(object? sender, EventArgs e)
        {
            if (this.MessagesScrollViewer == null || this.smoothScrollTimer == null)
            {
                return;
            }

            var elapsed = (DateTime.Now - this.smoothScrollStartTime).TotalMilliseconds;
            var progress = Math.Min(elapsed / SmoothScrollDurationMs, 1.0);

            var easedProgress = 1.0 - Math.Pow(1.0 - progress, 3);

            var currentOffset = this.smoothScrollStart + ((this.smoothScrollTarget - this.smoothScrollStart) * easedProgress);
            this.MessagesScrollViewer.ScrollToVerticalOffset(currentOffset);

            if (progress >= 1.0)
            {
                this.smoothScrollTimer.Stop();
                this.smoothScrollTimer = null;
                this.MessagesScrollViewer.ScrollToVerticalOffset(this.smoothScrollTarget);
                this.UpdateScrollButtonVisibility();
            }
        }

        /// <summary>
        /// Handles the DragEnter event for the chat area.
        /// </summary>
        private void ChatArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the DragOver event for the chat area.
        /// </summary>
        private void ChatArea_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles the Drop event for the chat area.
        /// </summary>
        private void ChatArea_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    var viewModel = this.DataContext as ChatViewModel;
                    if (viewModel != null)
                    {
                        // Use the same logic as AttachFileCommand but directly with file paths
                        viewModel.ProcessDroppedFilesAsync(files);
                    }
                }
            }

            e.Handled = true;
        }

        private async void ViewModel_OnAudioPlayRequested(AttachmentViewModel attachment, string audioUrl)
        {
            try
            {
                // MediaElement doesn't support HTTP with authentication headers
                // So we need to download the file to a temp location first
                if (attachment.AttachmentDto == null)
                {
                    attachment.IsPlaying = false;
                    return;
                }

                // Clean up previous temp file if exists
                if (!string.IsNullOrEmpty(this.currentAudioTempPath) && System.IO.File.Exists(this.currentAudioTempPath))
                {
                    try
                    {
                        System.IO.File.Delete(this.currentAudioTempPath);
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }

                // Download to temp file
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nexusteam_audio_{Guid.NewGuid()}{System.IO.Path.GetExtension(attachment.FileName)}");

                    // Download using the service
                    try
                    {
                        // Store reference to current attachment
                        this.currentPlayingAttachment = attachment;

                        // Use DownloadAttachmentAsync which handles authentication
                        var localPath = await viewModel.DownloadAttachmentForPlaybackAsync(attachment.AttachmentDto);
                        this.currentAudioTempPath = localPath;

                        // Now play from local file
                        this.AudioPlayer.Source = new Uri(localPath);

                        // Don't call Play() here - wait for MediaOpened event
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to download audio: {ex.Message}");
                        attachment.IsPlaying = false;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set audio source: {ex.Message}");
                attachment.IsPlaying = false;
            }
        }

        private void AudioPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            try
            {
                this.AudioPlayer.Play();

                // Update IsPlaying state for the current attachment
                if (this.currentPlayingAttachment != null)
                {
                    this.currentPlayingAttachment.IsPlaying = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play audio: {ex.Message}");
                if (this.currentPlayingAttachment != null)
                {
                    this.currentPlayingAttachment.IsPlaying = false;
                }
            }
        }

        private void AudioPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"Media failed: {e.ErrorException?.Message}");

            // Reset playing state
            if (this.currentPlayingAttachment != null)
            {
                this.currentPlayingAttachment.IsPlaying = false;
                this.currentPlayingAttachment = null;
            }
        }

        private void ViewModel_OnAudioPlayPauseRequested(AttachmentViewModel attachment)
        {
            try
            {
                // Check if this is the currently playing attachment
                if (this.currentPlayingAttachment == attachment && this.AudioPlayer.Source != null)
                {
                    // Toggle play/pause based on current state
                    if (attachment.IsPlaying)
                    {
                        this.AudioPlayer.Pause();
                        attachment.IsPlaying = false;
                    }
                    else
                    {
                        this.AudioPlayer.Play();
                        attachment.IsPlaying = true;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to toggle audio playback: {ex.Message}");
                attachment.IsPlaying = false;
            }
        }

        private void ViewModel_OnAudioStopRequested(AttachmentViewModel attachment)
        {
            try
            {
                // Stop the audio player
                this.AudioPlayer.Stop();
                attachment.IsPlaying = false;
                this.currentPlayingAttachment = null;

                // Clean up temp file
                if (!string.IsNullOrEmpty(this.currentAudioTempPath) && System.IO.File.Exists(this.currentAudioTempPath))
                {
                    try
                    {
                        System.IO.File.Delete(this.currentAudioTempPath);
                        this.currentAudioTempPath = null;
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to stop audio: {ex.Message}");
            }
        }

        private void AudioPlayer_MediaEnded(object sender, RoutedEventArgs e)
        {
            // Store references
            var attachment = this.currentPlayingAttachment;
            var tempPath = this.currentAudioTempPath;

            // Clear references immediately
            this.currentPlayingAttachment = null;
            this.currentAudioTempPath = null;

            // Update UI state asynchronously with high priority to avoid blocking
            // This ensures UI updates quickly without blocking the MediaEnded event handler
            if (attachment != null)
            {
                this.Dispatcher.BeginInvoke(
                    DispatcherPriority.Input,
                    new Action(() =>
                    {
                        attachment.IsPlaying = false;
                    }));
            }

            // Clean up temp file asynchronously in background thread (non-blocking)
            if (!string.IsNullOrEmpty(tempPath))
            {
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (System.IO.File.Exists(tempPath))
                        {
                            System.IO.File.Delete(tempPath);
                        }
                    }
                    catch
                    {
                        // Ignore cleanup errors
                    }
                });
            }
        }

        private MessageViewModel? FindMessageViewModelFromElement(DependencyObject element)
        {
            var current = element;
            while (current != null)
            {
                if (current is FrameworkElement fe && fe.DataContext is MessageViewModel msg)
                {
                    return msg;
                }

                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void ReactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string emoji)
            {
                var message = this.FindMessageViewModelFromElement(button);
                if (message != null)
                {
                    var viewModel = this.DataContext as ChatViewModel;
                    if (viewModel != null)
                    {
                        var tuple = new Tuple<MessageViewModel, string>(message, emoji);
                        viewModel.ToggleReactionCommand.Execute(tuple);
                    }
                }
            }
        }

        private void MessageBorder_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Don't open popup on double click (for text selection)
            if (e.ClickCount > 1)
            {
                return;
            }

            // Allow context menu to show - don't handle the event
            // The popup with reactions will be opened via "Add Reaction" in context menu
        }

        private async void SaveAsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                // Get MessageViewModel from Tag or DataContext
                MessageViewModel? message = null;
                if (menuItem.Tag is MessageViewModel msgFromTag)
                {
                    message = msgFromTag;
                }
                else if (menuItem.DataContext is MessageViewModel msgFromDataContext)
                {
                    message = msgFromDataContext;
                }
                else
                {
                    // Try to get from PlacementTarget
                    var contextMenu = menuItem.Parent as ContextMenu;
                    if (contextMenu?.PlacementTarget is FrameworkElement placementTarget)
                    {
                        message = this.FindMessageViewModelFromElement(placementTarget);
                    }
                }

                if (message != null && message.HasImageAttachments)
                {
                    // Find first image attachment
                    var imageAttachment = message.Attachments.FirstOrDefault(a => a.IsImage);
                    if (imageAttachment?.AttachmentDto != null)
                    {
                        var viewModel = this.DataContext as ChatViewModel;
                        if (viewModel != null)
                        {
                            // Call SaveAttachmentAsAsync directly to open file dialog
                            await viewModel.SaveImageAsAsync(imageAttachment.AttachmentDto);
                        }
                    }
                }
            }
        }

        private void CopyMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MessageViewModel message)
            {
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    viewModel.CopyMessageCommand.Execute(message);
                }
            }
        }

        private void ReplyMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MessageViewModel message)
            {
                var viewModel = this.DataContext as ChatViewModel;
                viewModel?.ReplyToMessageCommand.Execute(message);
            }
        }

        private void ForwardMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MessageViewModel message)
            {
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null && viewModel.ForwardMessageCommand.CanExecute(message))
                {
                    viewModel.ForwardMessageCommand.Execute(message);
                }
            }
        }

        private void ReplyQuote_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is MessageViewModel message
                && !string.IsNullOrEmpty(message.ReplyToId))
            {
                this.ScrollToMessage(message.ReplyToId);
            }
        }

        private void ScrollToMessage(string messageId)
        {
            if (this.MessagesItemsControl == null)
            {
                return;
            }

            foreach (var item in this.MessagesItemsControl.Items)
            {
                if (item is MessageViewModel message && message.Id == messageId)
                {
                    var container = this.MessagesItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                    container?.BringIntoView();
                    return;
                }
            }
        }

        private void EditMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MessageViewModel message)
            {
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    viewModel.EditMessageCommand.Execute(message);
                }
            }
        }

        private void DeleteMessageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Tag is MessageViewModel message)
            {
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    viewModel.DeleteMessageCommand.Execute(message);
                }
            }
        }

        private void AddReactionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                // Get the context menu
                var contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu == null)
                {
                    return;
                }

                // Get PlacementTarget (the Border that contains the message)
                var placementTarget = contextMenu.PlacementTarget as FrameworkElement;
                if (placementTarget == null)
                {
                    return;
                }

                // Try multiple methods to get MessageViewModel
                MessageViewModel? message = this.FindMessageViewModelFromElement(placementTarget);

                if (message != null && placementTarget != null)
                {
                    // Close context menu
                    contextMenu.IsOpen = false;

                    // Show reaction popup
                    this.ShowReactionPopup(message, placementTarget);
                }
            }
        }

        private void ShowReactionPopup(MessageViewModel message, FrameworkElement placementTarget)
        {
            if (message == null || placementTarget == null)
            {
                return;
            }

            // Close previous popup if open
            if (this.currentReactionPopup != null)
            {
                this.currentReactionPopup.IsOpen = false;
            }

            var horizontalOffset = 298;
            var verticalOffset = -35;

            var popup = new System.Windows.Controls.Primitives.Popup
            {
                Placement = System.Windows.Controls.Primitives.PlacementMode.Custom,
                PlacementTarget = placementTarget,
                CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                {
                    var x = targetSize.Width - horizontalOffset;
                    var y = verticalOffset;

                    return new System.Windows.Controls.Primitives.CustomPopupPlacement[]
                    {
                        new System.Windows.Controls.Primitives.CustomPopupPlacement(
                            new System.Windows.Point(x, y),
                            System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal),
                    };
                },
                StaysOpen = false,
                PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade,
                Tag = message,
                AllowsTransparency = true,
            };

            var popupBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 42, 42, 62)),
                CornerRadius = new CornerRadius(20),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(102, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(2, 1, 2, 1),
                ClipToBounds = true,
            };

            var shadowContainer = new Border
            {
                CornerRadius = new CornerRadius(20),
                ClipToBounds = false,
                Child = popupBorder,
            };

            shadowContainer.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                Direction = 315,
                ShadowDepth = 4,
                BlurRadius = 8,
                Opacity = 0.5,
            };

            var mainContainer = new StackPanel
            {
                Orientation = Orientation.Vertical,
            };

            var quickReactionsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
            };

            var quickEmojis = new[] { "👍", "❤️", "😂", "😮", "😢", "🙏", "🔥", "⭐" };
            foreach (var emoji in quickEmojis)
            {
                var emojiTextBlock = new Emoji.Wpf.TextBlock
                {
                    Text = emoji,
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    ClipToBounds = false,
                };

                var emojiButton = new Button
                {
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(4, 2, 4, 2),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = new Tuple<string, MessageViewModel>(emoji, message),
                    Width = 32,
                    Height = 32,
                    ClipToBounds = true,
                    Content = emojiTextBlock,
                    OverridesDefaultStyle = true,
                };

                var template = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
                borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
                borderFactory.SetValue(Border.TagProperty, new TemplateBindingExtension(Button.TagProperty));

                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(Button.ContentProperty));

                borderFactory.AppendChild(contentPresenterFactory);
                template.VisualTree = borderFactory;
                emojiButton.Template = template;

                var scaleTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
                emojiButton.RenderTransform = scaleTransform;
                emojiButton.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

                emojiButton.MouseEnter += (s, args) =>
                {
                    if (s is Button btn && btn.RenderTransform is System.Windows.Media.ScaleTransform transform)
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 1.3,
                            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut },
                        };
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
                    }
                };

                emojiButton.MouseLeave += (s, args) =>
                {
                    if (s is Button btn && btn.RenderTransform is System.Windows.Media.ScaleTransform transform)
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 1.0,
                            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut },
                        };
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
                    }
                };

                emojiButton.Click += (s, args) =>
                {
                    try
                    {
                        var viewModel = this.DataContext as ChatViewModel;
                        if (viewModel == null)
                        {
                            return;
                        }

                        if (message == null || string.IsNullOrEmpty(emoji))
                        {
                            return;
                        }

                        var tuple = new Tuple<MessageViewModel, string>(message, emoji);

                        if (viewModel.ToggleReactionCommand.CanExecute(tuple))
                        {
                            viewModel.ToggleReactionCommand.Execute(tuple);
                        }

                        if (this.currentReactionPopup != null)
                        {
                            this.currentReactionPopup.IsOpen = false;
                            this.currentReactionPopup = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"QuickReactionButton_Click: Error - {ex.Message}");
                    }
                };
                quickReactionsPanel.Children.Add(emojiButton);
            }

            var additionalEmojis = new[] { "😂", "😅", "🥰", "🙄", "😘", "😎", "🤓", "🥳", "😟", "😭", "😱", "😡", "🤷‍♂️", "🤷‍♀️", "🤦", "❌", "🤖", "💻", "🛠️", "🇺🇦" };

            var separatorLine = new Border
            {
                Height = 1,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(51, 255, 255, 255)),
                Margin = new Thickness(4, 2, 4, 2),
                Visibility = Visibility.Collapsed,
            };

            var expandableSection = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 0, 0),
            };

            var wrapPanel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                MaxWidth = 280,
            };

            foreach (var emoji in additionalEmojis)
            {
                var emojiTextBlock = new Emoji.Wpf.TextBlock
                {
                    Text = emoji,
                    FontSize = 20,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    ClipToBounds = false,
                };

                var emojiButton = new Button
                {
                    Background = System.Windows.Media.Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Padding = new Thickness(4, 2, 4, 2),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = new Tuple<string, MessageViewModel>(emoji, message),
                    Width = 32,
                    Height = 32,
                    ClipToBounds = true,
                    Content = emojiTextBlock,
                    OverridesDefaultStyle = true,
                };

                var template = new ControlTemplate(typeof(Button));
                var borderFactory = new FrameworkElementFactory(typeof(Border));
                borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
                borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
                borderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
                borderFactory.SetValue(Border.TagProperty, new TemplateBindingExtension(Button.TagProperty));

                var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                contentPresenterFactory.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(Button.ContentProperty));

                borderFactory.AppendChild(contentPresenterFactory);
                template.VisualTree = borderFactory;
                emojiButton.Template = template;

                var scaleTransform = new System.Windows.Media.ScaleTransform(1.0, 1.0);
                emojiButton.RenderTransform = scaleTransform;
                emojiButton.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

                emojiButton.MouseEnter += (s, args) =>
                {
                    if (s is Button btn && btn.RenderTransform is System.Windows.Media.ScaleTransform transform)
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 1.3,
                            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut },
                        };
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
                    }
                };

                emojiButton.MouseLeave += (s, args) =>
                {
                    if (s is Button btn && btn.RenderTransform is System.Windows.Media.ScaleTransform transform)
                    {
                        var animation = new System.Windows.Media.Animation.DoubleAnimation
                        {
                            To = 1.0,
                            Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(150)),
                            EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut },
                        };
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, animation);
                        transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, animation);
                    }
                };

                emojiButton.Click += (s, args) =>
                {
                    try
                    {
                        var viewModel = this.DataContext as ChatViewModel;
                        if (viewModel == null)
                        {
                            return;
                        }

                        if (message == null || string.IsNullOrEmpty(emoji))
                        {
                            return;
                        }

                        var tuple = new Tuple<MessageViewModel, string>(message, emoji);

                        if (viewModel.ToggleReactionCommand.CanExecute(tuple))
                        {
                            viewModel.ToggleReactionCommand.Execute(tuple);
                        }

                        if (this.currentReactionPopup != null)
                        {
                            this.currentReactionPopup.IsOpen = false;
                            this.currentReactionPopup = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"QuickReactionButton_Click (expanded): Error - {ex.Message}");
                    }
                };
                wrapPanel.Children.Add(emojiButton);
            }

            expandableSection.Children.Add(wrapPanel);

            var dropdownButton = new Button
            {
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(4, 2, 4, 2),
                Cursor = System.Windows.Input.Cursors.Hand,
                Width = 28,
                Height = 28,
                Margin = new Thickness(2, 0, 0, 0),
                OverridesDefaultStyle = true,
            };

            var arrowTextBlock = new System.Windows.Controls.TextBlock
            {
                Text = "▼",
                FontSize = 12,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 200, 200, 200)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            dropdownButton.Content = arrowTextBlock;

            var dropdownTemplate = new ControlTemplate(typeof(Button));
            var dropdownBorderFactory = new FrameworkElementFactory(typeof(Border));
            dropdownBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            dropdownBorderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));
            dropdownBorderFactory.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Button.PaddingProperty));
            dropdownBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));

            var dropdownContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            dropdownContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            dropdownContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            dropdownContentFactory.SetValue(ContentPresenter.ContentProperty, new TemplateBindingExtension(Button.ContentProperty));

            dropdownBorderFactory.AppendChild(dropdownContentFactory);
            dropdownTemplate.VisualTree = dropdownBorderFactory;
            dropdownButton.Template = dropdownTemplate;

            bool isExpanded = false;
            dropdownButton.Click += (s, args) =>
            {
                isExpanded = !isExpanded;
                expandableSection.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                separatorLine.Visibility = isExpanded ? Visibility.Visible : Visibility.Collapsed;
                arrowTextBlock.Text = isExpanded ? "▲" : "▼";
                args.Handled = true;
            };

            quickReactionsPanel.Children.Add(dropdownButton);

            mainContainer.Children.Add(quickReactionsPanel);
            mainContainer.Children.Add(separatorLine);
            mainContainer.Children.Add(expandableSection);

            popupBorder.Child = mainContainer;
            popup.Child = shadowContainer;

            popup.Tag = message;

            popup.IsOpen = true;
            this.currentReactionPopup = popup;
        }

        private void MessageContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            // Close reaction popup when context menu opens
            if (this.currentReactionPopup != null)
            {
                this.currentReactionPopup.IsOpen = false;
                this.currentReactionPopup = null;
            }
        }

        private void QuickReactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Tuple<string, MessageViewModel> tagData)
            {
                var emoji = tagData.Item1;
                var message = tagData.Item2;

                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    var tuple = new Tuple<MessageViewModel, string>(message, emoji);
                    viewModel.ToggleReactionCommand.Execute(tuple);
                }

                // Close the popup
                if (this.currentReactionPopup != null)
                {
                    this.currentReactionPopup.IsOpen = false;
                    this.currentReactionPopup = null;
                }
            }
        }

        private void EmojiPicker_EmojiSelected(object? sender, string emoji)
        {
            if (string.IsNullOrEmpty(emoji))
            {
                return;
            }

            var viewModel = this.DataContext as ChatViewModel;
            if (viewModel == null)
            {
                return;
            }

            try
            {
                viewModel.InsertEmojiCommand.Execute(emoji);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error inserting emoji: {ex.Message}");
            }
        }

        private void SetupEmojiPickerBinding()
        {
            try
            {
                if (this.EmojiPickerPopup != null && this.DataContext is ChatViewModel viewModel)
                {
                    // Sync initial state
                    this.EmojiPickerPopup.IsOpen = viewModel.IsEmojiPickerOpen;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in SetupEmojiPickerBinding: {ex.Message}");
            }
        }

        private void EmojiPickerPopup_Opened(object? sender, EventArgs e)
        {
            try
            {
                // Ensure PlacementTarget is set every time popup opens
                if (this.EmojiPickerPopup != null)
                {
                    var emojiButtonContainer = this.FindName("EmojiButtonContainer") as FrameworkElement;
                    if (emojiButtonContainer != null && this.EmojiPickerPopup.PlacementTarget != emojiButtonContainer)
                    {
                        this.EmojiPickerPopup.PlacementTarget = emojiButtonContainer;
                    }
                }

                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null && this.EmojiPickerPopup != null)
                {
                    if (viewModel.IsEmojiPickerOpen != this.EmojiPickerPopup.IsOpen)
                    {
                        viewModel.IsEmojiPickerOpen = this.EmojiPickerPopup.IsOpen;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EmojiPickerPopup_Opened: {ex.Message}");
            }
        }

        private void EmojiPickerPopup_Closed(object? sender, EventArgs e)
        {
            try
            {
                // Popup closed - sync with ViewModel
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null && this.EmojiPickerPopup != null)
                {
                    // Only update if different to avoid circular updates
                    if (viewModel.IsEmojiPickerOpen != this.EmojiPickerPopup.IsOpen)
                    {
                        viewModel.IsEmojiPickerOpen = this.EmojiPickerPopup.IsOpen;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EmojiPickerPopup_Closed: {ex.Message}");
            }
        }

        private System.Windows.Controls.Primitives.CustomPopupPlacement[]? EmojiPickerPopup_PlacementCallback(
            System.Windows.Size popupSize,
            System.Windows.Size targetSize,
            System.Windows.Point offset)
        {
            try
            {
                var emojiButtonContainer = this.FindName("EmojiButtonContainer") as FrameworkElement;
                if (emojiButtonContainer != null)
                {
                    // Get the position of the button container relative to the window
                    var point = emojiButtonContainer.PointToScreen(new System.Windows.Point(0, 0));
                    var windowPoint = this.PointToScreen(new System.Windows.Point(0, 0));

                    // Position popup above the button, slightly to the left
                    var x = targetSize.Width - 50;
                    var y = -popupSize.Height - 10;

                    return new System.Windows.Controls.Primitives.CustomPopupPlacement[]
                    {
                        new System.Windows.Controls.Primitives.CustomPopupPlacement(new System.Windows.Point(x, y), System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal),
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in EmojiPickerPopup_PlacementCallback: {ex.Message}");
            }

            // Fallback: position above the button
            return new System.Windows.Controls.Primitives.CustomPopupPlacement[]
            {
                new System.Windows.Controls.Primitives.CustomPopupPlacement(new System.Windows.Point(targetSize.Width - 50, -popupSize.Height - 10), System.Windows.Controls.Primitives.PopupPrimaryAxis.Horizontal),
            };
        }

        private void MessageInputRichTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize RichTextBox with current MessageText
            this.UpdateRichTextBoxFromViewModel();
        }

        private void MessageInputRichTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Sync RichTextBox content to ViewModel MessageText
            this.UpdateViewModelFromRichTextBox();
        }

        private void MessageInputRichTextBox2_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize RichTextBox with current MessageText
            this.UpdateRichTextBox2FromViewModel();
        }

        private void MessageInputRichTextBox2_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Sync RichTextBox content to ViewModel MessageText
            this.UpdateViewModelFromRichTextBox2();
        }

        private void UpdateRichTextBoxFromViewModel()
        {
            if (this.MessageInputRichTextBox == null || this.isUpdatingFromViewModel)
            {
                return;
            }

            var viewModel = this.DataContext as ChatViewModel;
            if (viewModel == null)
            {
                return;
            }

            try
            {
                this.isUpdatingFromViewModel = true;

                // Get current text from RichTextBox
                var currentText = new TextRange(
                    this.MessageInputRichTextBox.Document.ContentStart,
                    this.MessageInputRichTextBox.Document.ContentEnd).Text.TrimEnd('\r', '\n');

                // Only update if different to avoid circular updates
                if (currentText != viewModel.MessageText)
                {
                    // Save cursor position
                    var selection = this.MessageInputRichTextBox.Selection;
                    var start = selection.Start;
                    var offset = start.GetOffsetToPosition(this.MessageInputRichTextBox.Document.ContentStart);

                    // Update document
                    var paragraph = new Paragraph();
                    paragraph.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                    paragraph.Inlines.Add(new Run(viewModel.MessageText ?? string.Empty));
                    this.MessageInputRichTextBox.Document.Blocks.Clear();
                    this.MessageInputRichTextBox.Document.Blocks.Add(paragraph);

                    // Restore cursor position
                    try
                    {
                        var newStart = this.MessageInputRichTextBox.Document.ContentStart.GetPositionAtOffset(Math.Min(offset, viewModel.MessageText?.Length ?? 0));
                        if (newStart != null)
                        {
                            this.MessageInputRichTextBox.Selection.Select(newStart, newStart);
                            this.MessageInputRichTextBox.CaretPosition = newStart;
                        }
                    }
                    catch
                    {
                        // If position restoration fails, just move to end
                        this.MessageInputRichTextBox.CaretPosition = this.MessageInputRichTextBox.Document.ContentEnd;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating RichTextBox from ViewModel: {ex.Message}");
            }
            finally
            {
                this.isUpdatingFromViewModel = false;
            }
        }

        private void UpdateViewModelFromRichTextBox()
        {
            if (this.MessageInputRichTextBox == null || this.isUpdatingFromViewModel)
            {
                return;
            }

            var viewModel = this.DataContext as ChatViewModel;
            if (viewModel == null)
            {
                return;
            }

            try
            {
                // Get text from RichTextBox
                var textRange = new TextRange(
                    this.MessageInputRichTextBox.Document.ContentStart,
                    this.MessageInputRichTextBox.Document.ContentEnd);
                var text = textRange.Text.TrimEnd('\r', '\n');

                // Only update if different to avoid circular updates
                if (text != viewModel.MessageText)
                {
                    viewModel.MessageText = text;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ViewModel from RichTextBox: {ex.Message}");
            }
        }

        private void UpdateRichTextBox2FromViewModel()
        {
            if (this.MessageInputRichTextBox2 == null || this.isUpdatingFromViewModel)
            {
                return;
            }

            var viewModel = this.DataContext as ChatViewModel;
            if (viewModel == null)
            {
                return;
            }

            try
            {
                this.isUpdatingFromViewModel = true;

                // Get current text from RichTextBox
                var currentText = new TextRange(
                    this.MessageInputRichTextBox2.Document.ContentStart,
                    this.MessageInputRichTextBox2.Document.ContentEnd).Text.TrimEnd('\r', '\n');

                // Only update if different to avoid circular updates
                if (currentText != viewModel.MessageText)
                {
                    // Save cursor position
                    var selection = this.MessageInputRichTextBox2.Selection;
                    var start = selection.Start;
                    var offset = start.GetOffsetToPosition(this.MessageInputRichTextBox2.Document.ContentStart);

                    // Update document
                    var paragraph = new Paragraph();
                    paragraph.FlowDirection = System.Windows.FlowDirection.LeftToRight;
                    paragraph.Inlines.Add(new Run(viewModel.MessageText ?? string.Empty));
                    this.MessageInputRichTextBox2.Document.Blocks.Clear();
                    this.MessageInputRichTextBox2.Document.Blocks.Add(paragraph);

                    // Restore cursor position
                    try
                    {
                        var newStart = this.MessageInputRichTextBox2.Document.ContentStart.GetPositionAtOffset(Math.Min(offset, viewModel.MessageText?.Length ?? 0));
                        if (newStart != null)
                        {
                            this.MessageInputRichTextBox2.Selection.Select(newStart, newStart);
                            this.MessageInputRichTextBox2.CaretPosition = newStart;
                        }
                    }
                    catch
                    {
                        // If position restoration fails, just move to end
                        this.MessageInputRichTextBox2.CaretPosition = this.MessageInputRichTextBox2.Document.ContentEnd;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating RichTextBox2 from ViewModel: {ex.Message}");
            }
            finally
            {
                this.isUpdatingFromViewModel = false;
            }
        }

        private void UpdateViewModelFromRichTextBox2()
        {
            if (this.MessageInputRichTextBox2 == null || this.isUpdatingFromViewModel)
            {
                return;
            }

            var viewModel = this.DataContext as ChatViewModel;
            if (viewModel == null)
            {
                return;
            }

            try
            {
                // Get text from RichTextBox
                var textRange = new TextRange(
                    this.MessageInputRichTextBox2.Document.ContentStart,
                    this.MessageInputRichTextBox2.Document.ContentEnd);
                var text = textRange.Text.TrimEnd('\r', '\n');

                // Only update if different to avoid circular updates
                if (text != viewModel.MessageText)
                {
                    viewModel.MessageText = text;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating ViewModel from RichTextBox2: {ex.Message}");
            }
        }

        private void UserProfileOverlay_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (this.DataContext is ViewModels.ChatViewModel viewModel)
            {
                viewModel.CloseUserProfileCommand.Execute(null);
            }
        }

        private void UserProfileModal_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void MicrophoneButton_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                e.Handled = true; // Prevent default button click behavior
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine("MicrophoneButton_PreviewMouseDown: Starting voice recording");
                    if (viewModel.StartVoiceRecordingCommand.CanExecute(null))
                    {
                        viewModel.StartVoiceRecordingCommand.Execute(null);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MicrophoneButton_PreviewMouseDown: Cannot execute StartVoiceRecordingCommand");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MicrophoneButton_PreviewMouseDown: ViewModel is null");
                }
            }
        }

        private void MicrophoneButton_PreviewMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                e.Handled = true; // Prevent default button click behavior
                var viewModel = this.DataContext as ChatViewModel;
                if (viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine("MicrophoneButton_PreviewMouseUp: Stopping voice recording");
                    if (viewModel.StopVoiceRecordingCommand.CanExecute(null))
                    {
                        viewModel.StopVoiceRecordingCommand.Execute(null);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("MicrophoneButton_PreviewMouseUp: Cannot execute StopVoiceRecordingCommand");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("MicrophoneButton_PreviewMouseUp: ViewModel is null");
                }
            }
        }

        private void MicrophoneButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // If mouse leaves button while recording, stop recording
            var viewModel = this.DataContext as ChatViewModel;
            if (viewModel != null && System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                System.Diagnostics.Debug.WriteLine("MicrophoneButton_MouseLeave: Stopping voice recording");
                if (viewModel.StopVoiceRecordingCommand.CanExecute(null))
                {
                    viewModel.StopVoiceRecordingCommand.Execute(null);
                }
            }
        }

        private void MicrophoneButton_Click(object sender, RoutedEventArgs e)
        {
            // Prevent default click behavior - we handle it via PreviewMouseDown/Up
            e.Handled = true;
        }

        private void TranslateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                // Get MessageViewModel from Tag or DataContext
                MessageViewModel? message = null;
                if (menuItem.Tag is MessageViewModel msgFromTag)
                {
                    message = msgFromTag;
                }
                else if (menuItem.DataContext is MessageViewModel msgFromDataContext)
                {
                    message = msgFromDataContext;
                }
                else
                {
                    // Try to get from PlacementTarget
                    var contextMenu = menuItem.Parent as ContextMenu;
                    if (contextMenu?.PlacementTarget is FrameworkElement placementTarget)
                    {
                        message = this.FindMessageViewModelFromElement(placementTarget);
                    }
                }

                if (message != null && !string.IsNullOrWhiteSpace(message.Content))
                {
                    // Close previous translate popup if open
                    if (this.currentTranslatePopup != null)
                    {
                        this.currentTranslatePopup.IsOpen = false;
                        this.currentTranslatePopup = null;
                    }

                    // Get translation service from DI
                    var app = Application.Current as App;
                    var translationService = app?.Services?.GetService(typeof(NexusTeam.Client.Services.ITranslationService)) as NexusTeam.Client.Services.ITranslationService;

                    if (translationService != null)
                    {
                        // Get placement target (the Border that contains the message)
                        var contextMenu = menuItem.Parent as ContextMenu;
                        var placementTarget = contextMenu?.PlacementTarget as FrameworkElement;
                        if (placementTarget == null)
                        {
                            placementTarget = this.FindMessageElement(message);
                        }

                        if (placementTarget != null)
                        {
                            this.ShowTranslatePopup(message, placementTarget, translationService);
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "Translation service is not available.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
        }

        private FrameworkElement? FindMessageElement(MessageViewModel message)
        {
            // Try to find the Border element containing this message
            var itemsControl = this.FindName("MessagesScrollViewer") as ScrollViewer;
            if (itemsControl?.Content is ItemsControl messagesControl)
            {
                var container = messagesControl.ItemContainerGenerator.ContainerFromItem(message) as FrameworkElement;
                if (container != null)
                {
                    return this.FindVisualChild<Border>(container);
                }
            }

            return null;
        }

        private T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var childOfChild = this.FindVisualChild<T>(child);
                if (childOfChild != null)
                {
                    return childOfChild;
                }
            }

            return null;
        }

        private void ShowTranslatePopup(MessageViewModel message, FrameworkElement placementTarget, NexusTeam.Client.Services.ITranslationService translationService)
        {
            if (message == null || placementTarget == null)
            {
                return;
            }

            var translateControl = new TranslateWindow(translationService, message.Content);

            var popup = new System.Windows.Controls.Primitives.Popup
            {
                Placement = System.Windows.Controls.Primitives.PlacementMode.Custom,
                PlacementTarget = placementTarget,
                CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                {
                    // Position above the message
                    var x = (targetSize.Width - popupSize.Width) / 2;
                    var y = -popupSize.Height - 10;

                    return new System.Windows.Controls.Primitives.CustomPopupPlacement[]
                    {
                        new System.Windows.Controls.Primitives.CustomPopupPlacement(
                            new System.Windows.Point(x, y),
                            System.Windows.Controls.Primitives.PopupPrimaryAxis.Vertical),
                    };
                },
                StaysOpen = false, // Close when clicking outside
                PopupAnimation = System.Windows.Controls.Primitives.PopupAnimation.Fade,
                AllowsTransparency = true,
                Child = translateControl,
            };

            translateControl.SetParentPopup(popup);
            this.currentTranslatePopup = popup;
            popup.IsOpen = true;
        }
    }
}
