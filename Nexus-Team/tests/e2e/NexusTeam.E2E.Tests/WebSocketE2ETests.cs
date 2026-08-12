namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Net.WebSockets;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class WebSocketE2ETests
    {
        private readonly E2EFixture fixture;

        public WebSocketE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "WS-01 Valid token authenticates a WebSocket connection")]
        public async Task Ws01_ValidAuthentication()
        {
            var user = await this.fixture.RegisterAndLoginAsync("ws01"); await using var socket = this.fixture.Socket();
            await socket.ConnectAsync(); await socket.AuthenticateAsync(user.Token);
            Assert.Equal(WebSocketState.Open, socket.State);
        }

        [Fact(DisplayName = "WS-02 Invalid token receives authentication error")]
        public async Task Ws02_InvalidToken()
        {
            await using var socket = this.fixture.Socket(); await socket.ConnectAsync();
            await socket.SendAsync(new { type = "authenticate", payload = new { token = "invalid-token" } });
            using var error = await socket.ReceiveTypeAsync("error");
            Assert.Contains("Authentication", error.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact(DisplayName = "WS-03 Message before authentication is rejected")]
        public async Task Ws03_MessageBeforeAuthentication()
        {
            await using var socket = this.fixture.Socket(); await socket.ConnectAsync();
            await socket.SendAsync(new { type = "heartbeat" });
            using var error = await socket.ReceiveTypeAsync("error");
            Assert.Contains("Not authenticated", error.RootElement.GetProperty("error").GetString());
        }

        [Fact(DisplayName = "WS-04 Invalid JSON receives protocol error")]
        public async Task Ws04_InvalidJson()
        {
            await using var socket = this.fixture.Socket(); await socket.ConnectAsync(); await socket.SendTextAsync("{not-json");
            using var error = await socket.ReceiveTypeAsync("error");
            Assert.Contains("Invalid message format", error.RootElement.GetProperty("error").GetString());
        }

        [Fact(DisplayName = "WS-05 Authentication timeout closes idle socket")]
        public async Task Ws05_AuthenticationTimeout()
        {
            await using var socket = this.fixture.Socket(); await socket.ConnectAsync();
            var status = await socket.WaitForCloseAsync(TimeSpan.FromSeconds(15));
            Assert.True(status is WebSocketCloseStatus.PolicyViolation or WebSocketCloseStatus.Empty);
        }

        [Fact(DisplayName = "WS-06 Heartbeat keeps authenticated connection open")]
        public async Task Ws06_Heartbeat()
        {
            var user = await this.fixture.RegisterAndLoginAsync("ws06"); await using var socket = this.fixture.Socket(); await socket.ConnectAsync(); await socket.AuthenticateAsync(user.Token);
            await socket.SendAsync(new { type = "heartbeat" }); await Task.Delay(250);
            Assert.Equal(WebSocketState.Open, socket.State);
        }

        [Fact(DisplayName = "WS-07 Multiple connections for one user receive broadcasts")]
        public async Task Ws07_MultipleConnections()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("ws07_owner"); var member = await this.fixture.RegisterAndLoginAsync("ws07_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            await using var first = this.fixture.Socket(); await using var second = this.fixture.Socket(); await first.ConnectAsync(); await second.ConnectAsync(); await first.AuthenticateAsync(member.Token); await second.AuthenticateAsync(member.Token);
            await this.fixture.SendMessageAsync(owner, chatId, "fan out");
            using var one = await first.ReceiveTypeAsync("newMessage"); using var two = await second.ReceiveTypeAsync("newMessage");
            Assert.Equal(one.RootElement.GetProperty("messageId").GetString(), two.RootElement.GetProperty("messageId").GetString());
        }

        [Fact(DisplayName = "WS-08 Disconnected user can reconnect and authenticate again")]
        public async Task Ws08_ReconnectionRecovery()
        {
            var user = await this.fixture.RegisterAndLoginAsync("ws08");
            await using (var first = this.fixture.Socket()) { await first.ConnectAsync(); await first.AuthenticateAsync(user.Token); }
            await using var second = this.fixture.Socket(); await second.ConnectAsync(); await second.AuthenticateAsync(user.Token);
            Assert.Equal(WebSocketState.Open, second.State);
        }

        [Fact(DisplayName = "WS-09 Presence change is broadcast to chat partner")]
        public async Task Ws09_PresenceBroadcast()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("ws09_owner"); var member = await this.fixture.RegisterAndLoginAsync("ws09_member"); await this.fixture.CreateChatAsync(owner, member);
            await using var memberSocket = this.fixture.Socket(); await memberSocket.ConnectAsync(); await memberSocket.AuthenticateAsync(member.Token);
            await using var ownerSocket = this.fixture.Socket(); await ownerSocket.ConnectAsync(); await ownerSocket.AuthenticateAsync(owner.Token);
            using var update = await memberSocket.ReceiveTypeAsync("statusUpdate");
            Assert.Equal(owner.Id, update.RootElement.GetProperty("payload").GetProperty("userId").GetString());
        }

        [Fact(DisplayName = "WS-10 Call request is forwarded to online recipient")]
        public async Task Ws10_CallForwarding()
        {
            var caller = await this.fixture.RegisterAndLoginAsync("ws10_caller"); var callee = await this.fixture.RegisterAndLoginAsync("ws10_callee");
            await using var callerSocket = this.fixture.Socket(); await using var calleeSocket = this.fixture.Socket(); await callerSocket.ConnectAsync(); await calleeSocket.ConnectAsync(); await callerSocket.AuthenticateAsync(caller.Token); await calleeSocket.AuthenticateAsync(callee.Token);
            var callId = Guid.NewGuid().ToString();
            await callerSocket.SendAsync(new { type = "callRequest", payload = new { callId, fromUserId = caller.Id, toUserId = callee.Id, timestamp = DateTime.UtcNow } });
            using var forwarded = await calleeSocket.ReceiveTypeAsync("callRequest");
            Assert.Equal(callId, forwarded.RootElement.GetProperty("payload").GetProperty("callId").GetString());
        }

        [Fact(DisplayName = "WS-11 Message rate limiting returns an explicit error")]
        [Trait("Category", "Regression")]
        public async Task Ws11_MessageRateLimit()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("ws11_owner"); var member = await this.fixture.RegisterAndLoginAsync("ws11_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            await using var socket = this.fixture.Socket(); await socket.ConnectAsync(); await socket.AuthenticateAsync(owner.Token);
            for (var i = 0; i < 60; i++)
            {
                await socket.SendAsync(new { type = "newMessage", payload = new { chatId, content = $"rate-{i}", attachmentIds = Array.Empty<string>() } });
                using var receipt = await socket.ReceiveTypeAsync("newMessage");
            }
            await socket.SendAsync(new { type = "newMessage", payload = new { chatId, content = "rate-overflow", attachmentIds = Array.Empty<string>() } });
            using var error = await socket.ReceiveTypeAsync("error");
            Assert.Contains("rate limit", error.RootElement.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact(DisplayName = "WS-12 Last connection disconnect broadcasts offline presence")]
        public async Task Ws12_DisconnectBroadcastsOffline()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("ws12_owner"); var member = await this.fixture.RegisterAndLoginAsync("ws12_member"); await this.fixture.CreateChatAsync(owner, member);
            await using var memberSocket = this.fixture.Socket(); await memberSocket.ConnectAsync(); await memberSocket.AuthenticateAsync(member.Token);
            await using var ownerSocket = this.fixture.Socket(); await ownerSocket.ConnectAsync(); await ownerSocket.AuthenticateAsync(owner.Token);
            using (var online = await memberSocket.ReceiveTypeAsync("statusUpdate")) { Assert.Equal(owner.Id, online.RootElement.GetProperty("payload").GetProperty("userId").GetString()); }
            await ownerSocket.CloseAsync();
            using var offline = await memberSocket.ReceiveTypeAsync("statusUpdate");
            Assert.Equal("offline", offline.RootElement.GetProperty("payload").GetProperty("status").GetString());
        }
    }
}
