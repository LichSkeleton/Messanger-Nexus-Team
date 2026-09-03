namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;

    /// <summary>
    /// View model for the create folder dialog.
    /// </summary>
    public class CreateFolderDialogViewModel : ObservableObject
    {
        private string folderName;
        private string? validationError;
        private bool canConfirm;
        private string? folderId;
        private string searchText;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateFolderDialogViewModel"/> class.
        /// </summary>
        public CreateFolderDialogViewModel()
        {
            this.folderName = string.Empty;
            this.searchText = string.Empty;
            this.AvailableChats = new ObservableCollection<SelectableChatViewModel>();
            this.ConfirmCommand = new RelayCommand(this.OnConfirm, this.CanExecuteConfirm);
            this.CancelCommand = new RelayCommand(this.OnCancel);
            this.canConfirm = false;
            this.folderId = null;

            this.AvailableChats.CollectionChanged += (s, e) => this.UpdateCanConfirm();
        }

        /// <summary>
        /// Gets or sets the folder ID (for edit mode).
        /// </summary>
        public string? FolderId
        {
            get => this.folderId;
            set => this.SetProperty(ref this.folderId, value);
        }

        /// <summary>
        /// Gets a value indicating whether this is edit mode.
        /// </summary>
        public bool IsEditMode => !string.IsNullOrEmpty(this.folderId);

        /// <summary>
        /// Gets or sets the folder name input.
        /// </summary>
        public string FolderName
        {
            get => this.folderName;
            set
            {
                if (this.SetProperty(ref this.folderName, value))
                {
                    this.UpdateCanConfirm();
                }
            }
        }

        /// <summary>
        /// Gets or sets the chat search filter.
        /// </summary>
        public string SearchText
        {
            get => this.searchText;
            set
            {
                if (this.SetProperty(ref this.searchText, value))
                {
                    this.ApplySearchFilter();
                }
            }
        }

        /// <summary>
        /// Gets the collection of available chats for selection.
        /// </summary>
        public ObservableCollection<SelectableChatViewModel> AvailableChats { get; }

        /// <summary>
        /// Gets or sets the validation error message.
        /// </summary>
        public string? ValidationError
        {
            get => this.validationError;
            set => this.SetProperty(ref this.validationError, value);
        }

        /// <summary>
        /// Gets a value indicating whether the confirm button can be executed.
        /// </summary>
        public bool CanConfirm
        {
            get => this.canConfirm;
            private set => this.SetProperty(ref this.canConfirm, value);
        }

        /// <summary>
        /// Gets the confirm command.
        /// </summary>
        public RelayCommand ConfirmCommand { get; }

        /// <summary>
        /// Gets the cancel command.
        /// </summary>
        public RelayCommand CancelCommand { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the dialog was confirmed.
        /// </summary>
        public bool DialogResult { get; set; }

        /// <summary>
        /// Gets the selected chat IDs.
        /// </summary>
        /// <returns>List of selected chat IDs.</returns>
        public List<string> GetSelectedChatIds()
        {
            return this.AvailableChats
                .Where(c => c.IsSelected && c.Chat != null)
                .Select(c => c.Chat!.Id)
                .ToList();
        }

        /// <summary>
        /// Populates available chats from the provided list.
        /// </summary>
        /// <param name="chats">The list of chats to display.</param>
        /// <param name="selectedChatIds">Optional list of chat IDs that should be pre-selected (for edit mode).</param>
        public void PopulateChats(List<ConversationViewModel> chats, List<string>? selectedChatIds = null)
        {
            this.AvailableChats.Clear();

            if (chats != null)
            {
                foreach (var chat in chats)
                {
                    var isSelected = selectedChatIds != null && selectedChatIds.Contains(chat.Id);
                    var selectableChat = new SelectableChatViewModel { Chat = chat, IsSelected = isSelected };
                    selectableChat.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectableChatViewModel.IsSelected))
                        {
                            this.UpdateCanConfirm();
                        }
                    };
                    this.AvailableChats.Add(selectableChat);
                }
            }

            this.ApplySearchFilter();
            this.UpdateCanConfirm();
        }

        private void ApplySearchFilter()
        {
            var filter = (this.searchText ?? string.Empty).Trim();
            foreach (var item in this.AvailableChats)
            {
                if (string.IsNullOrEmpty(filter) || item.Chat == null)
                {
                    item.IsVisible = true;
                    continue;
                }

                item.IsVisible = (item.Chat.Name ?? string.Empty)
                    .Contains(filter, StringComparison.OrdinalIgnoreCase);
            }
        }

        private void UpdateCanConfirm()
        {
            var selectedCount = this.AvailableChats.Count(c => c.IsSelected);
            var hasName = !string.IsNullOrWhiteSpace(this.folderName);

            this.CanConfirm = hasName && selectedCount >= 1;
            this.ValidationError = hasName && selectedCount < 1
                ? "Select at least one chat."
                : null;

            this.ConfirmCommand.NotifyCanExecuteChanged();
        }

        private bool CanExecuteConfirm() => this.CanConfirm;

        private void OnConfirm()
        {
            this.DialogResult = true;
        }

        private void OnCancel()
        {
            this.DialogResult = false;
        }
    }

    /// <summary>
    /// View model for a selectable chat in the folder creation dialog.
    /// </summary>
    public class SelectableChatViewModel : ObservableObject
    {
        private ConversationViewModel? chat;
        private bool isSelected;
        private bool isVisible = true;

        /// <summary>
        /// Gets or sets the chat.
        /// </summary>
        public ConversationViewModel? Chat
        {
            get => this.chat;
            set => this.SetProperty(ref this.chat, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the chat is selected.
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this chat matches the current search filter.
        /// </summary>
        public bool IsVisible
        {
            get => this.isVisible;
            set => this.SetProperty(ref this.isVisible, value);
        }
    }
}
