namespace NexusTeam.Shared.Helpers
{
    /// <summary>
    /// Static helper class for generating consistent Redis cache keys.
    /// Provides centralized key naming conventions across the application.
    /// </summary>
    public static class RedisKeys
    {
        private const string Prefix = "NexusTeam";

        /// <summary>
        /// Generates a Redis key for user session data.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The Redis key for the user session.</returns>
        public static string UserSession(string userId) => $"{Prefix}:user:session:{userId}";

        /// <summary>
        /// Generates a Redis key for user online status.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The Redis key for the user status.</returns>
        public static string UserStatus(string userId) => $"{Prefix}:user:status:{userId}";

        /// <summary>
        /// Generates a Redis key for the user's invisible-mode preference (survives disconnect).
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The Redis key for the invisible preference.</returns>
        public static string UserInvisiblePreference(string userId) => $"{Prefix}:user:invisible:{userId}";

        /// <summary>
        /// Generates a Redis key for chat messages cache.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <returns>The Redis key for the chat messages.</returns>
        public static string ChatMessages(string chatId) => $"{Prefix}:chat:messages:{chatId}";

        /// <summary>
        /// Generates a Redis key for user's active chats list.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>The Redis key for the user's chats.</returns>
        public static string UserChats(string userId) => $"{Prefix}:user:chats:{userId}";

        /// <summary>
        /// Generates a Redis key for unread message count per chat.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="chatId">The chat identifier.</param>
        /// <returns>The Redis key for the unread count.</returns>
        public static string UnreadCount(string userId, string chatId) => $"{Prefix}:unread:{userId}:{chatId}";

        /// <summary>
        /// Generates a Redis key for typing indicator in a chat.
        /// </summary>
        /// <param name="chatId">The chat identifier.</param>
        /// <returns>The Redis key for the typing indicator.</returns>
        public static string TypingIndicator(string chatId) => $"{Prefix}:typing:{chatId}";

        /// <summary>
        /// Generates a Redis key for refresh token storage.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="tokenId">The token identifier.</param>
        /// <returns>The Redis key for the refresh token.</returns>
        public static string RefreshToken(string userId, string tokenId) => $"{Prefix}:token:refresh:{userId}:{tokenId}";
    }
}
