namespace NexusTeam.Client.ViewModels
{
    using System.Collections.Generic;
    using CommunityToolkit.Mvvm.ComponentModel;
    using NexusTeam.Shared.Dtos;

    /// <summary>
    /// View model for a chat folder in the folder list.
    /// </summary>
    public partial class ChatFolderViewModel : ObservableObject
    {
        private string id;
        private string name;
        private List<string> chatIds;
        private bool isSelected;
        private int unreadCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChatFolderViewModel"/> class.
        /// </summary>
        /// <param name="folderDto">The folder DTO.</param>
        public ChatFolderViewModel(ChatFolderDto folderDto)
        {
            this.id = folderDto.Id;
            this.name = folderDto.Name;
            this.chatIds = folderDto.ChatIds ?? new List<string>();
        }

        /// <summary>
        /// Gets or sets the folder ID.
        /// </summary>
        public string Id
        {
            get => this.id;
            set => this.SetProperty(ref this.id, value);
        }

        /// <summary>
        /// Gets or sets the folder name.
        /// </summary>
        public string Name
        {
            get => this.name;
            set => this.SetProperty(ref this.name, value);
        }

        /// <summary>
        /// Gets or sets the list of chat IDs in this folder.
        /// </summary>
        public List<string> ChatIds
        {
            get => this.chatIds;
            set => this.SetProperty(ref this.chatIds, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether this folder is selected.
        /// </summary>
        public bool IsSelected
        {
            get => this.isSelected;
            set => this.SetProperty(ref this.isSelected, value);
        }

        /// <summary>
        /// Gets or sets the unread count for this folder.
        /// </summary>
        public int UnreadCount
        {
            get => this.unreadCount;
            set => this.SetProperty(ref this.unreadCount, value);
        }

        /// <summary>
        /// Gets a value indicating whether this is the "All Chats" folder.
        /// </summary>
        public bool IsAllChatsFolder => this.id == "all";

        /// <summary>
        /// Updates the folder from a DTO.
        /// </summary>
        /// <param name="folderDto">The folder DTO.</param>
        public void UpdateFromDto(ChatFolderDto folderDto)
        {
            this.Name = folderDto.Name;
            this.ChatIds = folderDto.ChatIds ?? new List<string>();
        }
    }
}
