namespace NexusTeam.Shared.Tests.Helpers
{
    using NexusTeam.Shared.Helpers;
    using Xunit;

    public class RedisKeysTests
    {
        [Fact]
        public void UserSession_WithUserId_ReturnsNamespacedKey()
        {
            var result = RedisKeys.UserSession("user-123");

            Assert.Equal("NexusTeam:user:session:user-123", result);
        }

        [Theory]
        [InlineData("user-status", "NexusTeam:user:status:user-123")]
        [InlineData("user-invisible", "NexusTeam:user:invisible:user-123")]
        [InlineData("chat-messages", "NexusTeam:chat:messages:chat-456")]
        [InlineData("user-chats", "NexusTeam:user:chats:user-123")]
        [InlineData("unread-count", "NexusTeam:unread:user-123:chat-456")]
        [InlineData("typing", "NexusTeam:typing:chat-456")]
        [InlineData("refresh-token", "NexusTeam:token:refresh:user-123:token-789")]
        public void KeyFactory_ReturnsExpectedNamespaceAndSegments(
            string keyType,
            string expected)
        {
            var result = keyType switch
            {
                "user-status" => RedisKeys.UserStatus("user-123"),
                "user-invisible" => RedisKeys.UserInvisiblePreference("user-123"),
                "chat-messages" => RedisKeys.ChatMessages("chat-456"),
                "user-chats" => RedisKeys.UserChats("user-123"),
                "unread-count" => RedisKeys.UnreadCount("user-123", "chat-456"),
                "typing" => RedisKeys.TypingIndicator("chat-456"),
                "refresh-token" => RedisKeys.RefreshToken("user-123", "token-789"),
                _ => throw new Xunit.Sdk.XunitException($"Unknown key type: {keyType}"),
            };

            Assert.Equal(expected, result);
        }
    }
}
