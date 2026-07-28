namespace NexusTeam.Client.ViewModels
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// View model for the create chat dialog.
    /// </summary>
    public class CreateChatDialogViewModel : ObservableObject
    {
        private string chatName;
        private string? validationError;
        private bool canConfirm;
        private bool isChatNameVisible;
        private UserDto? currentUser;
        private bool isUpdatingChatName;
        private string? lastDefaultChatName;
        private bool isChatNameManuallyEdited;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateChatDialogViewModel"/> class.
        /// </summary>
        public CreateChatDialogViewModel()
        {
            this.chatName = string.Empty;
            this.AvailableUsers = new ObservableCollection<SelectableUserViewModel>();
            this.ConfirmCommand = new RelayCommand(this.OnConfirm, this.CanExecuteConfirm);
            this.CancelCommand = new RelayCommand(this.OnCancel);
            this.canConfirm = false;
            this.isChatNameVisible = false;
            this.isUpdatingChatName = false;
            this.isChatNameManuallyEdited = false;

            this.AvailableUsers.CollectionChanged += (s, e) => this.UpdateCanConfirm();
        }

        /// <summary>
        /// Gets or sets the chat name input.
        /// </summary>
        public string ChatName
        {
            get => this.chatName;
            set
            {
                if (this.SetProperty(ref this.chatName, value))
                {
                    // Если название изменяется не программно (не через UpdateChatNameBasedOnSelection),
                    // значит пользователь редактирует его вручную
                    if (!this.isUpdatingChatName)
                    {
                        var selectedCount = this.AvailableUsers.Count(u => u.IsSelected);

                        // Если выбрано 2+ пользователей, считаем что пользователь редактирует вручную
                        if (selectedCount >= 2)
                        {
                            this.isChatNameManuallyEdited = true;
                        }
                    }

                    this.UpdateCanConfirm();
                }
            }
        }

        /// <summary>
        /// Gets the collection of available users for selection.
        /// </summary>
        public ObservableCollection<SelectableUserViewModel> AvailableUsers { get; }

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
        /// Gets or sets the current user (creator of the chat).
        /// </summary>
        public UserDto? CurrentUser
        {
            get => this.currentUser;
            set => this.SetProperty(ref this.currentUser, value);
        }

        /// <summary>
        /// Gets a value indicating whether the chat name input field should be visible.
        /// </summary>
        public bool IsChatNameVisible
        {
            get => this.isChatNameVisible;
            private set => this.SetProperty(ref this.isChatNameVisible, value);
        }

        /// <summary>
        /// Gets the selected participant IDs.
        /// </summary>
        /// <returns>List of selected user IDs.</returns>
        public List<string> GetSelectedParticipants()
        {
            return this.AvailableUsers
                .Where(u => u.IsSelected)
                .Select(u => u.User.Id)
                .ToList();
        }

        /// <summary>
        /// Gets the final chat name to use when creating the chat.
        /// If name is empty and multiple users selected, generates default name.
        /// </summary>
        /// <returns>The chat name to use.</returns>
        public string GetFinalChatName()
        {
            var selectedUsers = this.AvailableUsers.Where(u => u.IsSelected).ToList();
            var selectedCount = selectedUsers.Count;

            // Если выбрано 2+ пользователей и название пустое, используем дефолтное
            if (selectedCount >= 2 && string.IsNullOrWhiteSpace(this.ChatName))
            {
                return this.GenerateDefaultChatName(selectedUsers);
            }

            return this.ChatName ?? string.Empty;
        }

        /// <summary>
        /// Populates available users from the provided list.
        /// </summary>
        /// <param name="users">The list of users to display.</param>
        public void PopulateUsers(List<UserDto> users)
        {
            this.AvailableUsers.Clear();

            if (users != null)
            {
                foreach (var user in users)
                {
                    var selectableUser = new SelectableUserViewModel { User = user, IsSelected = false };
                    selectableUser.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectableUserViewModel.IsSelected))
                        {
                            this.UpdateChatNameBasedOnSelection();
                            this.UpdateCanConfirm();
                        }
                    };
                    this.AvailableUsers.Add(selectableUser);
                }
            }

            this.UpdateCanConfirm();
        }

        private void UpdateChatNameBasedOnSelection()
        {
            if (this.isUpdatingChatName)
            {
                return;
            }

            this.isUpdatingChatName = true;

            try
            {
                var selectedUsers = this.AvailableUsers.Where(u => u.IsSelected).ToList();
                var selectedCount = selectedUsers.Count;

                if (selectedCount == 0)
                {
                    this.ChatName = string.Empty;
                    this.IsChatNameVisible = false;
                    this.lastDefaultChatName = null;
                    this.isChatNameManuallyEdited = false;
                }
                else if (selectedCount == 1)
                {
                    // При выборе одного пользователя - название становится его именем
                    var selectedUser = selectedUsers[0];
                    this.ChatName = selectedUser.DisplayName;
                    this.IsChatNameVisible = false;
                    this.lastDefaultChatName = null;
                    this.isChatNameManuallyEdited = false;
                }
                else
                {
                    // При выборе 2+ пользователей - показываем поле ввода
                    this.IsChatNameVisible = true;

                    // Генерируем новое дефолтное название со всеми выбранными пользователями
                    var defaultName = this.GenerateDefaultChatName(selectedUsers);

                    // Обновляем название автоматически если:
                    // 1. Поле пустое
                    // 2. Название не было изменено пользователем вручную (всегда обновляем автоматические названия)
                    if (string.IsNullOrWhiteSpace(this.ChatName) || !this.isChatNameManuallyEdited)
                    {
                        this.ChatName = defaultName;
                        this.lastDefaultChatName = defaultName;
                        this.isChatNameManuallyEdited = false;
                    }
                    else
                    {
                        // Пользователь ввел свое название, сохраняем его, но обновляем последнее дефолтное
                        this.lastDefaultChatName = defaultName;
                    }
                }
            }
            finally
            {
                this.isUpdatingChatName = false;
            }
        }

        private string GenerateDefaultChatName(List<SelectableUserViewModel> selectedUsers)
        {
            var names = new List<string>();

            // Добавляем текущего пользователя
            if (this.CurrentUser != null)
            {
                names.Add(!string.IsNullOrWhiteSpace(this.CurrentUser.DisplayName) ? this.CurrentUser.DisplayName : this.CurrentUser.Username);
            }

            // Добавляем выбранных пользователей
            foreach (var user in selectedUsers)
            {
                if (user.User != null)
                {
                    var name = !string.IsNullOrWhiteSpace(user.User.DisplayName) ? user.User.DisplayName : user.User.Username;
                    if (!names.Contains(name))
                    {
                        names.Add(name);
                    }
                }
            }

            return string.Join(", ", names);
        }

        private void UpdateCanConfirm()
        {
            var selectedCount = this.AvailableUsers.Count(u => u.IsSelected);
            var hasParticipants = selectedCount >= 1;

            // Если выбран один пользователь, название уже установлено автоматически
            // Если выбрано 2+, название может быть пустым, но мы установим дефолтное при подтверждении
            var hasName = !string.IsNullOrWhiteSpace(this.chatName) || selectedCount == 1;

            this.CanConfirm = hasName && hasParticipants;
            this.ValidationError = !hasParticipants ? "Select at least one participant" : null;

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
}
