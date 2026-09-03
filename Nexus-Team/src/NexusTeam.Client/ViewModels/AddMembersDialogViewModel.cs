namespace NexusTeam.Client.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// View model for adding members to an existing group.
    /// </summary>
    public class AddMembersDialogViewModel : ObservableObject
    {
        private string searchText = string.Empty;
        private string? validationError;
        private bool canConfirm;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMembersDialogViewModel"/> class.
        /// </summary>
        public AddMembersDialogViewModel()
        {
            this.AvailableUsers = new ObservableCollection<SelectableUserViewModel>();
            this.FilteredUsers = new ObservableCollection<SelectableUserViewModel>();
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
        /// Gets all available users.
        /// </summary>
        public ObservableCollection<SelectableUserViewModel> AvailableUsers { get; }

        /// <summary>
        /// Gets the filtered user list.
        /// </summary>
        public ObservableCollection<SelectableUserViewModel> FilteredUsers { get; }

        /// <summary>
        /// Gets or sets the validation error.
        /// </summary>
        public string? ValidationError
        {
            get => this.validationError;
            set => this.SetProperty(ref this.validationError, value);
        }

        /// <summary>
        /// Gets a value indicating whether confirm is enabled.
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
        /// Gets selected user IDs.
        /// </summary>
        /// <returns>Selected IDs.</returns>
        public List<string> GetSelectedUserIds()
        {
            return this.AvailableUsers
                .Where(u => u.IsSelected)
                .Select(u => u.User.Id)
                .ToList();
        }

        /// <summary>
        /// Populates users that are not already in the group.
        /// </summary>
        /// <param name="users">Available users.</param>
        public void PopulateUsers(List<UserDto> users)
        {
            this.AvailableUsers.Clear();
            if (users != null)
            {
                foreach (var user in users)
                {
                    var selectable = new SelectableUserViewModel { User = user, IsSelected = false };
                    selectable.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(SelectableUserViewModel.IsSelected))
                        {
                            this.UpdateCanConfirm();
                        }
                    };
                    this.AvailableUsers.Add(selectable);
                }
            }

            this.ApplyFilter();
            this.UpdateCanConfirm();
        }

        private void ApplyFilter()
        {
            this.FilteredUsers.Clear();
            var query = (this.searchText ?? string.Empty).Trim();
            foreach (var user in this.AvailableUsers)
            {
                if (string.IsNullOrEmpty(query)
                    || user.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(user.User.Username)
                        && user.User.Username.Contains(query, StringComparison.OrdinalIgnoreCase)))
                {
                    this.FilteredUsers.Add(user);
                }
            }
        }

        private void UpdateCanConfirm()
        {
            var selectedCount = this.AvailableUsers.Count(u => u.IsSelected);
            this.CanConfirm = selectedCount >= 1;
            this.ValidationError = selectedCount == 0 ? "Select at least one person" : null;
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
