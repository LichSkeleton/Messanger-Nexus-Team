namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Net.WebSockets;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    [CollectionDefinition(Name)]
    public sealed class E2ECollection : ICollectionFixture<E2EFixture>
    {
        public const string Name = "NexusTeam E2E";
    }

    public sealed class E2EFixture : IAsyncLifetime, IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        private readonly HttpClient http;

        public E2EFixture()
        {
            this.BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5251";
            this.WebSocketUrl = Environment.GetEnvironmentVariable("E2E_WS_URL") ?? "ws://localhost:5251/ws";
            this.http = new HttpClient { BaseAddress = new Uri(this.BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
        }

        public string BaseUrl { get; }

        public string WebSocketUrl { get; }

        public async Task InitializeAsync()
        {
            using var response = await this.http.GetAsync("/health");
            response.EnsureSuccessStatusCode();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public void Dispose() => this.http.Dispose();

        public HttpClient Client(string? token = null)
        {
            var client = new HttpClient { BaseAddress = new Uri(this.BaseUrl), Timeout = TimeSpan.FromSeconds(30) };
            if (!string.IsNullOrEmpty(token)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        public async Task<TestUser> RegisterAndLoginAsync(string prefix = "e2e")
        {
            var suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            var username = $"{prefix}_{suffix}";
            var email = $"{username}@example.test";
            const string password = "ValidPass123";
            using var client = this.Client();
            using var registration = await client.PostAsJsonAsync("/api/auth/register", new
            {
                username, email, password, displayName = $"E2E {suffix}",
            });
            Assert.Equal(HttpStatusCode.OK, registration.StatusCode);
            using var registerJson = await ReadJsonAsync(registration);
            var id = registerJson.RootElement.GetProperty("user").GetProperty("id").GetString()!;
            using var login = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = username, password });
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            using var loginJson = await ReadJsonAsync(login);
            return new TestUser(id, username, email, password, loginJson.RootElement.GetProperty("accessToken").GetString()!);
        }

        public async Task<string> CreateChatAsync(TestUser owner, params TestUser[] participants)
        {
            using var client = this.Client(owner.Token);
            using var response = await client.PostAsJsonAsync("/api/chats", new
            {
                name = "E2E Chat " + Guid.NewGuid().ToString("N"),
                description = "Created by end-to-end tests",
                type = 1,
                participantIds = participants.Select(x => x.Id).ToArray(),
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var json = await ReadJsonAsync(response);
            return json.RootElement.GetProperty("id").GetString()!;
        }

        public async Task<JsonElement> SendMessageAsync(TestUser sender, string chatId, string content)
        {
            using var client = this.Client(sender.Token);
            using var response = await client.PostAsJsonAsync($"/api/chats/{chatId}/messages", new
            {
                chatId, content, attachmentIds = Array.Empty<string>(),
            });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            using var json = await ReadJsonAsync(response);
            return json.RootElement.Clone();
        }

        public E2ESocket Socket() => new E2ESocket(this.WebSocketUrl);

        public static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            return JsonDocument.Parse(string.IsNullOrWhiteSpace(text) ? "{}" : text);
        }

        public static string CreateExpiredToken(string userId)
        {
            var secret = Environment.GetEnvironmentVariable("E2E_JWT_SECRET") ?? "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6q7r8s9t0u1v2w3x4y5z6";
            var issuer = Environment.GetEnvironmentVariable("E2E_JWT_ISSUER") ?? "NexusTeamServer";
            var audience = Environment.GetEnvironmentVariable("E2E_JWT_AUDIENCE") ?? "NexusTeamClient";
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { alg = "HS256", typ = "JWT" }, JsonOptions));
            var payload = Base64Url(JsonSerializer.SerializeToUtf8Bytes(new { sub = userId, iss = issuer, aud = audience, nbf = now - 7200, iat = now - 7200, exp = now - 3600 }, JsonOptions));
            var unsigned = header + "." + payload;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return unsigned + "." + Base64Url(hmac.ComputeHash(Encoding.UTF8.GetBytes(unsigned)));
        }

        private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public sealed record TestUser(string Id, string Username, string Email, string Password, string Token);

    public sealed class E2ESocket : IAsyncDisposable
    {
        private readonly ClientWebSocket socket = new ClientWebSocket();
        private readonly Uri uri;

        public E2ESocket(string url) => this.uri = new Uri(url);

        public WebSocketState State => this.socket.State;

        public Task ConnectAsync() => this.socket.ConnectAsync(this.uri, CancellationToken.None);

        public async Task AuthenticateAsync(string token)
        {
            await this.SendAsync(new { type = "authenticate", payload = new { token } });
            using var response = await this.ReceiveTypeAsync("authenticate");
            Assert.True(response.RootElement.GetProperty("payload").GetProperty("success").GetBoolean());
        }

        public async Task SendAsync(object value)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            await this.socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public Task SendTextAsync(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            return this.socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task<WebSocketCloseStatus?> WaitForCloseAsync(TimeSpan timeout)
        {
            using var cancellation = new CancellationTokenSource(timeout);
            var buffer = new byte[1024];
            try
            {
                while (true)
                {
                    var result = await this.socket.ReceiveAsync(buffer, cancellation.Token);
                    if (result.MessageType == WebSocketMessageType.Close) return result.CloseStatus;
                }
            }
            catch (WebSocketException) when (this.socket.State is WebSocketState.Aborted or WebSocketState.Closed)
            {
                return WebSocketCloseStatus.Empty;
            }
        }

        public async Task<JsonDocument> ReceiveAsync(TimeSpan? timeout = null)
        {
            using var cancellation = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(8));
            using var output = new MemoryStream();
            var buffer = new byte[4096];
            WebSocketReceiveResult result;
            do
            {
                result = await this.socket.ReceiveAsync(buffer, cancellation.Token);
                if (result.MessageType == WebSocketMessageType.Close) throw new WebSocketException("Socket closed before a message was received.");
                output.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);
            return JsonDocument.Parse(output.ToArray());
        }

        public async Task<JsonDocument> ReceiveTypeAsync(string expectedType, TimeSpan? timeout = null)
        {
            var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(8));
            while (DateTime.UtcNow < deadline)
            {
                var remaining = deadline - DateTime.UtcNow;
                var message = await this.ReceiveAsync(remaining);
                if (message.RootElement.TryGetProperty("type", out var type) && type.GetString() == expectedType) return message;
                message.Dispose();
            }

            throw new TimeoutException($"WebSocket message '{expectedType}' was not received.");
        }

        public async Task CloseAsync()
        {
            if (this.socket.State == WebSocketState.Open)
            {
                using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await this.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test disconnect", cancellation.Token);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.socket.State == WebSocketState.Open)
            {
                using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                try { await this.socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "test complete", cancellation.Token); } catch { }
            }
            this.socket.Dispose();
        }
    }
}
