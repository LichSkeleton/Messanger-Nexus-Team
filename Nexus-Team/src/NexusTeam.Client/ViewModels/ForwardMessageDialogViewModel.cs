namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Shared.Enums;

    /// <summary>
    /// View model for choosing a chat to forward a message into.
    /// </summary>
    public class ForwardMessageDialogViewModel : ObservableObject
    {
        private string searchText = string.Empty;
        private ConversationViewModel? selectedChat;
        private string? validationError;

        /// <summary>
        /// Initializes a new instance of the <see cref="ForwardMessageDialogViewModel"/> class.
        /// </summary>
        public ForwardMessageDialogViewModel()
        {
            this.Chats = new ObservableCollection<ConversationViewModel>();
            this.FilteredChats = new ObservableCollection<ConversationViewModel>();
            this.ConfirmCommand = new RelayCommand(this.OnConfirm, this.CanExecuteConfirm);
            this.CancelCommand = new RelayCommand(this.OnCancel);
        }

        /// <summary>
        /// Gets or sets the search text.
        /// </summary>
        public string SearchText
        {
            get => this.searchText;
            set
            {
                if (this.SetProperty(ref this.searchText, value))
                {
                    this.ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Gets all chats the user can forward to.
        /// </summary>
        public ObservableCollection<ConversationViewModel> Chats { get; }

        /// <summary>
        /// Gets the filtered chat list.
        /// </summary>
        public ObservableCollection<ConversationViewModel> FilteredChats { get; }

        /// <summary>
        /// Gets or sets the selected target chat.
        /// </summary>
        public ConversationViewModel? SelectedChat
        {
            get => this.selectedChat;
            set
            {
                if (this.SetProperty(ref this.selectedChat, value))
                {
                    this.ValidationError = value == null ? "Select a chat" : null;
                    this.ConfirmCommand.NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the validation error.
        /// </summary>
        public string? ValidationError
        {
            get => this.validationError;
            set => this.SetProperty(ref this.validationError, value);
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
        /// Populates chats the user can forward into.
        /// </summary>
        /// <param name="conversations">Available conversations, including Saved Messages.</param>
        public void PopulateChats(System.Collections.Generic.IEnumerable<ConversationViewModel> conversations)
        {
            this.Chats.Clear();
            if (conversations != null)
            {
                foreach (var chat in conversations.OrderByDescending(c => c.Type == ChatType.SavedMessages)
                             .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
                {
                    this.Chats.Add(chat);
                }
            }

            this.ApplyFilter();
            this.ValidationError = "Select a chat";
        }

        private void ApplyFilter()
        {
            this.FilteredChats.Clear();
            var query = (this.searchText ?? string.Empty).Trim();
            foreach (var chat in this.Chats)
            {
                if (string.IsNullOrEmpty(query)
                    || chat.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                {
                    this.FilteredChats.Add(chat);
                }
            }
        }

        private bool CanExecuteConfirm() => this.selectedChat != null;

        private void OnConfirm()
        {
            if (this.selectedChat == null)
            {
                this.ValidationError = "Select a chat";
                return;
            }

            this.DialogResult = true;
        }

        private void OnCancel()
        {
            this.DialogResult = false;
        }
    }
}
