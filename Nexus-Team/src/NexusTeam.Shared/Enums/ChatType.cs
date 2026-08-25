namespace NexusTeam.Shared.Enums
{
    /// <summary>
    /// Represents the type of chat conversation.
    /// </summary>
    public enum ChatType
    {
        /// <summary>
        /// Direct message between two users.
        /// </summary>
        DirectMessage = 0,

        /// <summary>
        /// Group chat with multiple participants.
        /// </summary>
        Group = 1,

        /// <summary>
        /// Public channel that users can join.
        /// </summary>
        Channel = 2,
    }
}
