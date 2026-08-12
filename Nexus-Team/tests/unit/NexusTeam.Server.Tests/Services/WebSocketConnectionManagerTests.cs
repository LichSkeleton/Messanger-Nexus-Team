namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services;
    using Serilog;
    using Xunit;

    public class WebSocketConnectionManagerTests
    {
        [Fact]
        public void AddConnection_MapsSocketConnectionAndUser()
        {
            var manager = CreateManager();
            var socket = new RecordingWebSocket();

            manager.AddConnection("user-1", socket, "connection-1");

            Assert.Same(socket, manager.GetSocketByConnectionId("connection-1"));
            Assert.Equal("user-1", manager.GetUserIdByConnectionId("connection-1"));
            Assert.Equal(new[] { "connection-1" }, manager.GetConnectionIdsByUserId("user-1"));
        }

        [Fact]
        public void AddConnection_WithMultipleConnections_PreservesAllUserConnections()
        {
            var manager = CreateManager();

            manager.AddConnection("user-1", new RecordingWebSocket(), "connection-1");
            manager.AddConnection("user-1", new RecordingWebSocket(), "connection-2");

            Assert.Equal(
                new[] { "connection-1", "connection-2" },
                manager.GetConnectionIdsByUserId("user-1").OrderBy(value => value));
        }

        [Fact]
        public void RemoveConnection_RemovesOnlyTargetConnection()
        {
            var manager = CreateManager();
            manager.AddConnection("user-1", new RecordingWebSocket(), "connection-1");
            manager.AddConnection("user-1", new RecordingWebSocket(), "connection-2");

            manager.RemoveConnection("connection-1");

            Assert.Null(manager.GetSocketByConnectionId("connection-1"));
            Assert.Null(manager.GetUserIdByConnectionId("connection-1"));
            Assert.Equal(new[] { "connection-2" }, manager.GetConnectionIdsByUserId("user-1"));
        }

        [Fact]
        public void RemoveConnection_WhenLastConnection_RemovesUserMapping()
        {
            var manager = CreateManager();
            manager.AddConnection("user-1", new RecordingWebSocket(), "connection-1");

            manager.RemoveConnection("connection-1");

            Assert.Empty(manager.GetConnectionIdsByUserId("user-1"));
        }

        [Fact]
        public void RemoveConnection_WhenUnknown_IsIdempotent()
        {
            var manager = CreateManager();

            var exception = Record.Exception(() => manager.RemoveConnection("missing"));

            Assert.Null(exception);
        }

        [Fact]
        public async Task SendMessageAsync_WithOpenSocket_SendsUtf8TextMessage()
        {
            var manager = CreateManager();
            var socket = new RecordingWebSocket(WebSocketState.Open);
            manager.AddConnection("user-1", socket, "connection-1");

            await manager.SendMessageAsync("connection-1", "Merhaba 👋");

            var sent = Assert.Single(socket.Messages);
            Assert.Equal("Merhaba 👋", sent.Text);
            Assert.Equal(WebSocketMessageType.Text, sent.MessageType);
            Assert.True(sent.EndOfMessage);
        }

        [Theory]
        [InlineData(WebSocketState.Closed)]
        [InlineData(WebSocketState.Aborted)]
        public async Task SendMessageAsync_WithNonOpenSocket_DoesNotSend(WebSocketState state)
        {
            var manager = CreateManager();
            var socket = new RecordingWebSocket(state);
            manager.AddConnection("user-1", socket, "connection-1");

            await manager.SendMessageAsync("connection-1", "ignored");

            Assert.Empty(socket.Messages);
        }

        [Fact]
        public async Task BroadcastToUserAsync_SendsToEveryOpenConnectionOnly()
        {
            var manager = CreateManager();
            var first = new RecordingWebSocket(WebSocketState.Open);
            var second = new RecordingWebSocket(WebSocketState.Open);
            var closed = new RecordingWebSocket(WebSocketState.Closed);
            manager.AddConnection("user-1", first, "connection-1");
            manager.AddConnection("user-1", second, "connection-2");
            manager.AddConnection("user-1", closed, "connection-3");

            await manager.BroadcastToUserAsync("user-1", "event");

            Assert.Single(first.Messages);
            Assert.Single(second.Messages);
            Assert.Empty(closed.Messages);
        }

        [Fact]
        public async Task ConcurrentAddAndRemove_KeepsConnectionMappingsConsistent()
        {
            var manager = CreateManager();
            var additions = Enumerable.Range(0, 100)
                .Select(index => Task.Run(() => manager.AddConnection(
                    "user-1",
                    new RecordingWebSocket(),
                    $"connection-{index}")));
            await Task.WhenAll(additions);

            var removals = Enumerable.Range(0, 50)
                .Select(index => Task.Run(() => manager.RemoveConnection($"connection-{index}")));
            await Task.WhenAll(removals);

            var remaining = manager.GetConnectionIdsByUserId("user-1").ToList();
            Assert.Equal(50, remaining.Count);
            Assert.All(remaining, id => Assert.Equal("user-1", manager.GetUserIdByConnectionId(id)));
            Assert.All(remaining, id => Assert.NotNull(manager.GetSocketByConnectionId(id)));
        }

        [Fact]
        public void AddConnection_WithDuplicateId_DoesNotAssociateItWithSecondUser()
        {
            var manager = CreateManager();
            manager.AddConnection("user-1", new RecordingWebSocket(), "connection-1");

            manager.AddConnection("user-2", new RecordingWebSocket(), "connection-1");

            Assert.Equal("user-1", manager.GetUserIdByConnectionId("connection-1"));
            Assert.Empty(manager.GetConnectionIdsByUserId("user-2"));
        }

        private static WebSocketConnectionManager CreateManager()
        {
            return new WebSocketConnectionManager(new LoggerConfiguration().CreateLogger());
        }

        private sealed class RecordingWebSocket : WebSocket
        {
            private WebSocketState state;

            public RecordingWebSocket(WebSocketState state = WebSocketState.Open)
            {
                this.state = state;
            }

            public ConcurrentQueue<SentMessage> Messages { get; } = new ConcurrentQueue<SentMessage>();

            public override WebSocketCloseStatus? CloseStatus => null;

            public override string? CloseStatusDescription => null;

            public override WebSocketState State => this.state;

            public override string? SubProtocol => null;

            public override void Abort()
            {
                this.state = WebSocketState.Aborted;
            }

            public override Task CloseAsync(
                WebSocketCloseStatus closeStatus,
                string? statusDescription,
                CancellationToken cancellationToken)
            {
                this.state = WebSocketState.Closed;
                return Task.CompletedTask;
            }

            public override Task CloseOutputAsync(
                WebSocketCloseStatus closeStatus,
                string? statusDescription,
                CancellationToken cancellationToken)
            {
                this.state = WebSocketState.CloseSent;
                return Task.CompletedTask;
            }

            public override void Dispose()
            {
                this.state = WebSocketState.Closed;
            }

            public override Task<WebSocketReceiveResult> ReceiveAsync(
                ArraySegment<byte> buffer,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException();
            }

            public override Task SendAsync(
                ArraySegment<byte> buffer,
                WebSocketMessageType messageType,
                bool endOfMessage,
                CancellationToken cancellationToken)
            {
                var bytes = buffer.ToArray();
                this.Messages.Enqueue(new SentMessage(
                    Encoding.UTF8.GetString(bytes),
                    messageType,
                    endOfMessage));
                return Task.CompletedTask;
            }
        }

        private sealed record SentMessage(
            string Text,
            WebSocketMessageType MessageType,
            bool EndOfMessage);
    }
}
