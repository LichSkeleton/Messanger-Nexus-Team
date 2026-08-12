namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class SecurityE2ETests
    {
        private readonly E2EFixture fixture;

        public SecurityE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "SEC-01 Anonymous chat listing is rejected")]
        public async Task Sec01_AnonymousChatsRejected()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/chats");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-02 Anonymous user listing is rejected")]
        public async Task Sec02_AnonymousUsersRejected()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-03 Anonymous preferences access is rejected")]
        public async Task Sec03_AnonymousPreferencesRejected()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/preferences");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-04 Anonymous folder access is rejected")]
        public async Task Sec04_AnonymousFoldersRejected()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/folders");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-05 Malformed bearer token is rejected")]
        public async Task Sec05_MalformedTokenRejected()
        {
            using var client = this.fixture.Client("not-a-jwt"); using var response = await client.GetAsync("/api/chats");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-06 Non-participant cannot read a private chat")]
        [Trait("Category", "Regression")]
        public async Task Sec06_NonParticipantCannotReadChat()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec06_owner"); var member = await this.fixture.RegisterAndLoginAsync("sec06_member"); var outsider = await this.fixture.RegisterAndLoginAsync("sec06_out");
            var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(outsider.Token); using var response = await client.GetAsync($"/api/chats/{chatId}");
            Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "SEC-07 Non-participant cannot read private messages")]
        [Trait("Category", "Regression")]
        public async Task Sec07_NonParticipantCannotReadMessages()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec07_owner"); var member = await this.fixture.RegisterAndLoginAsync("sec07_member"); var outsider = await this.fixture.RegisterAndLoginAsync("sec07_out");
            var chatId = await this.fixture.CreateChatAsync(owner, member); await this.fixture.SendMessageAsync(owner, chatId, "private");
            using var client = this.fixture.Client(outsider.Token); using var response = await client.GetAsync($"/api/chats/{chatId}/messages");
            Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "SEC-08 Non-owner cannot update group metadata")]
        public async Task Sec08_NonOwnerCannotUpdateGroup()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec08_owner"); var member = await this.fixture.RegisterAndLoginAsync("sec08_member");
            var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(member.Token); using var response = await client.PutAsJsonAsync($"/api/chats/{chatId}", new { name = "Hijacked" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-09 Login rate limiting returns 429")]
        public async Task Sec09_LoginRateLimit()
        {
            var identifier = "missing_" + Guid.NewGuid().ToString("N");
            using var client = this.fixture.Client();
            HttpStatusCode last = HttpStatusCode.OK;
            for (var i = 0; i < 6; i++) { using var response = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = identifier, password = "WrongPass123" }); last = response.StatusCode; }
            Assert.Equal((HttpStatusCode)429, last);
        }

        [Fact(DisplayName = "SEC-10 Security headers are present on API responses")]
        [Trait("Category", "Regression")]
        public async Task Sec10_SecurityHeaders()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/health");
            Assert.True(response.Headers.Contains("X-Content-Type-Options"));
            Assert.True(response.Headers.Contains("X-Frame-Options"));
            Assert.True(response.Headers.Contains("Referrer-Policy"));
            Assert.DoesNotContain(response.Headers, x => x.Key.Equals("Server", StringComparison.OrdinalIgnoreCase));
        }

        [Fact(DisplayName = "SEC-11 Anonymous message history access is rejected")]
        [Trait("Category", "Regression")]
        public async Task Sec11_AnonymousMessageHistoryRejected()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec11_owner"); var member = await this.fixture.RegisterAndLoginAsync("sec11_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(); using var response = await client.GetAsync($"/api/chats/{chatId}/messages");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-12 Anonymous attachment listing is rejected")]
        [Trait("Category", "Regression")]
        public async Task Sec12_AnonymousAttachmentListingRejected()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/attachments/message/nonexistent");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-13 Group member cannot delete owner chat")]
        [Trait("Category", "Regression")]
        public async Task Sec13_NonOwnerCannotDeleteGroup()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec13_owner"); var member = await this.fixture.RegisterAndLoginAsync("sec13_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(member.Token); using var response = await client.DeleteAsync($"/api/chats/{chatId}");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-14 Invalid route does not expose internal exception details")]
        public async Task Sec14_UnknownRouteIsSafe404()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/does-not-exist");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("exception", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact(DisplayName = "SEC-15 Anonymous generated-image metadata access is rejected")]
        [Trait("Category", "Regression")]
        public async Task Sec15_AnonymousGeneratedImageRejected()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec15"); using var ownerClient = this.fixture.Client(owner.Token); using var create = await ownerClient.PostAsJsonAsync("/api/generated-images", new { prompt = "private", model = "e2e", imageUrl = "url", width = 1, height = 1 }); using var json = await E2EFixture.ReadJsonAsync(create); var id = json.RootElement.GetProperty("id").GetString();
            using var anonymous = this.fixture.Client(); using var response = await anonymous.GetAsync($"/api/generated-images/{id}"); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-16 Another user cannot read generated-image metadata")]
        [Trait("Category", "Regression")]
        public async Task Sec16_GeneratedImageOwnership()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec16_owner"); var outsider = await this.fixture.RegisterAndLoginAsync("sec16_out"); using var ownerClient = this.fixture.Client(owner.Token); using var create = await ownerClient.PostAsJsonAsync("/api/generated-images", new { prompt = "private", model = "e2e", imageUrl = "url", width = 1, height = 1 }); using var json = await E2EFixture.ReadJsonAsync(create); var id = json.RootElement.GetProperty("id").GetString();
            using var outsiderClient = this.fixture.Client(outsider.Token); using var response = await outsiderClient.GetAsync($"/api/generated-images/{id}"); Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "SEC-17 Anonymous attachment upload is rejected")]
        [Trait("Category", "Regression")]
        public async Task Sec17_AnonymousAttachmentUpload()
        {
            using var client = this.fixture.Client(); using var form = new MultipartFormDataContent(); var bytes = new ByteArrayContent(new byte[] { 1 }); bytes.Headers.ContentType = new MediaTypeHeaderValue("text/plain"); form.Add(bytes, "file", "private.txt"); form.Add(new StringContent("message-id"), "messageId");
            using var response = await client.PostAsync("/api/attachments/upload", form); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-18 Anonymous attachment delete is rejected")]
        [Trait("Category", "Regression")]
        public async Task Sec18_AnonymousAttachmentDelete()
        {
            using var client = this.fixture.Client(); using var response = await client.DeleteAsync("/api/attachments/any-id"); Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "SEC-19 Non-participant cannot send to private chat")]
        public async Task Sec19_OutsiderCannotSendMessage()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec19_owner"); var member = await this.fixture.RegisterAndLoginAsync("sec19_member"); var outsider = await this.fixture.RegisterAndLoginAsync("sec19_out"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(outsider.Token); using var response = await client.PostAsJsonAsync($"/api/chats/{chatId}/messages", new { chatId, content = "intrusion", attachmentIds = Array.Empty<string>() }); Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
        }

        [Fact(DisplayName = "SEC-20 Non-participant cannot react to private message")]
        [Trait("Category", "Regression")]
        public async Task Sec20_OutsiderCannotReact()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("sec20_owner"); var member = await this.fixture.RegisterAndLoginAsync("sec20_member"); var outsider = await this.fixture.RegisterAndLoginAsync("sec20_out"); var chatId = await this.fixture.CreateChatAsync(owner, member); var message = await this.fixture.SendMessageAsync(owner, chatId, "private");
            using var client = this.fixture.Client(outsider.Token); using var response = await client.PostAsJsonAsync($"/api/chats/{chatId}/messages/{message.GetProperty("id").GetString()}/reactions", new { emoji = "👀" }); Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden);
        }
    }
}
