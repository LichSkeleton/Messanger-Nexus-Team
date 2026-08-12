namespace NexusTeam.Shared.Tests.Serialization
{
    using System;
    using System.Text.Json;
    using NexusTeam.Shared.Contracts;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Serialization;
    using Xunit;

    public class JsonSerializerOptionsFactoryTests
    {
        [Fact]
        public void Default_WhenReadRepeatedly_ReturnsSameOptionsInstance()
        {
            var first = JsonSerializerOptionsFactory.Default;
            var second = JsonSerializerOptionsFactory.Default;

            Assert.Same(first, second);
        }

        [Fact]
        public void WebSocket_WhenReadRepeatedly_ReturnsSameOptionsInstance()
        {
            var first = JsonSerializerOptionsFactory.WebSocket;
            var second = JsonSerializerOptionsFactory.WebSocket;

            Assert.Same(first, second);
        }

        [Fact]
        public void Default_WhenSerializingDto_UsesCamelCasePropertyNames()
        {
            var request = new LoginRequest
            {
                UsernameOrEmail = "alice@example.com",
                Password = "secret",
            };

            var json = JsonSerializer.Serialize(request, JsonSerializerOptionsFactory.Default);
            using var document = JsonDocument.Parse(json);

            Assert.Equal(
                "alice@example.com",
                document.RootElement.GetProperty("usernameOrEmail").GetString());
            Assert.Equal("secret", document.RootElement.GetProperty("password").GetString());
            Assert.False(document.RootElement.TryGetProperty("UsernameOrEmail", out _));
        }

        [Fact]
        public void Default_WhenDeserializingPropertyNames_IsCaseInsensitive()
        {
            const string Json = """
                { "USERNAMEOREMAIL": "alice", "PASSWORD": "secret" }
                """;

            var result = JsonSerializer.Deserialize<LoginRequest>(
                Json,
                JsonSerializerOptionsFactory.Default);

            Assert.NotNull(result);
            Assert.Equal("alice", result.UsernameOrEmail);
            Assert.Equal("secret", result.Password);
        }

        [Fact]
        public void WebSocket_WhenSerializingEnvelope_WritesEnumAsCamelCaseString()
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Type = WebSocketMessageType.CallIceCandidate,
            };

            var json = JsonSerializer.Serialize(envelope, JsonSerializerOptionsFactory.WebSocket);
            using var document = JsonDocument.Parse(json);

            Assert.Equal("callIceCandidate", document.RootElement.GetProperty("type").GetString());
        }

        [Fact]
        public void WebSocket_WhenSerializingEnvelope_OmitsNullProperties()
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Type = WebSocketMessageType.Heartbeat,
                Payload = null,
                MessageId = null,
                Error = null,
            };

            var json = JsonSerializer.Serialize(envelope, JsonSerializerOptionsFactory.WebSocket);
            using var document = JsonDocument.Parse(json);

            Assert.Single(document.RootElement.EnumerateObject());
            Assert.True(document.RootElement.TryGetProperty("type", out _));
            Assert.False(document.RootElement.TryGetProperty("payload", out _));
            Assert.False(document.RootElement.TryGetProperty("messageId", out _));
            Assert.False(document.RootElement.TryGetProperty("error", out _));
        }

        [Fact]
        public void WebSocket_WhenSerializingEnvelope_ProducesCompactJson()
        {
            var envelope = new WebSocketMessageEnvelope
            {
                Type = WebSocketMessageType.Error,
                Error = "Something went wrong",
            };

            var json = JsonSerializer.Serialize(envelope, JsonSerializerOptionsFactory.WebSocket);

            Assert.DoesNotContain('\n', json);
            Assert.DoesNotContain("  ", json);
        }

        [Fact]
        [Trait("Category", "Regression")]
        public void WebSocket_WhenSerializingAnonymousPayload_SupportsControllerPayloads()
        {
            var payload = new { ChatId = "chat-1" };

            var element = JsonSerializer.SerializeToElement(
                payload,
                JsonSerializerOptionsFactory.WebSocket);

            Assert.Equal("chat-1", element.GetProperty("chatId").GetString());
        }

        [Fact]
        public void WebSocketEnvelope_WithTypingPayload_RoundTripsWithoutDataLoss()
        {
            var payload = new TypingIndicatorContract
            {
                UserId = "user-1",
                ChatId = "chat-2",
                Username = "Alice",
                IsTyping = true,
            };
            var options = JsonSerializerOptionsFactory.WebSocket;
            var envelope = new WebSocketMessageEnvelope
            {
                Type = WebSocketMessageType.Typing,
                MessageId = "event-3",
                Payload = JsonSerializer.SerializeToElement(payload, options),
            };

            var json = JsonSerializer.Serialize(envelope, options);
            var restoredEnvelope = JsonSerializer.Deserialize<WebSocketMessageEnvelope>(json, options);

            Assert.NotNull(restoredEnvelope);
            Assert.Equal(WebSocketMessageType.Typing, restoredEnvelope.Type);
            Assert.Equal("event-3", restoredEnvelope.MessageId);
            var payloadElement = Assert.IsType<JsonElement>(restoredEnvelope.Payload);
            var restoredPayload = payloadElement.Deserialize<TypingIndicatorContract>(options);
            Assert.NotNull(restoredPayload);
            Assert.Equal(MessageTypes.TypingIndicator, restoredPayload.Type);
            Assert.Equal("user-1", restoredPayload.UserId);
            Assert.Equal("chat-2", restoredPayload.ChatId);
            Assert.Equal("Alice", restoredPayload.Username);
            Assert.True(restoredPayload.IsTyping);
        }

        [Fact]
        public void ChatMessageContract_WhenRoundTripped_PreservesTimestampAndOptionalReply()
        {
            var timestamp = new DateTime(2026, 8, 12, 14, 30, 15, DateTimeKind.Utc);
            var message = new ChatMessageContract
            {
                MessageId = "message-1",
                ChatId = "chat-2",
                SenderId = "user-3",
                Content = "Hello",
                Timestamp = timestamp,
                ReplyToId = "message-0",
            };
            var options = JsonSerializerOptionsFactory.WebSocket;

            var json = JsonSerializer.Serialize(message, options);
            var restored = JsonSerializer.Deserialize<ChatMessageContract>(json, options);

            Assert.NotNull(restored);
            Assert.Equal(MessageTypes.ChatMessage, restored.Type);
            Assert.Equal(message.MessageId, restored.MessageId);
            Assert.Equal(message.ChatId, restored.ChatId);
            Assert.Equal(message.SenderId, restored.SenderId);
            Assert.Equal(message.Content, restored.Content);
            Assert.Equal(timestamp, restored.Timestamp);
            Assert.Equal(message.ReplyToId, restored.ReplyToId);
        }
    }
}
