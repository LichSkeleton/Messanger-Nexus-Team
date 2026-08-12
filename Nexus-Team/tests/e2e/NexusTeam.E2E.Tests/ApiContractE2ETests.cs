namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class ApiContractE2ETests
    {
        private readonly E2EFixture fixture;
        public ApiContractE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "API-01 Health endpoint reports healthy")]
        public async Task Api01_Health()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/health"); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = await E2EFixture.ReadJsonAsync(response); Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        }

        [Fact(DisplayName = "API-02 Unknown chat returns not found")]
        public async Task Api02_UnknownChat()
        {
            var user = await this.fixture.RegisterAndLoginAsync("api02"); using var client = this.fixture.Client(user.Token); using var response = await client.GetAsync("/api/chats/does-not-exist"); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(DisplayName = "API-03 Null chat body is rejected")]
        public async Task Api03_NullChatBody()
        {
            var user = await this.fixture.RegisterAndLoginAsync("api03"); using var client = this.fixture.Client(user.Token); using var response = await client.PostAsJsonAsync<object?>("/api/chats", null); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "API-04 Null participant list is a validation error, not server error")]
        public async Task Api04_NullParticipants()
        {
            var user = await this.fixture.RegisterAndLoginAsync("api04"); using var client = this.fixture.Client(user.Token); using var response = await client.PostAsJsonAsync("/api/chats", new { name = "Null participants", type = 1, participantIds = (string[]?)null }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "API-05 Oversized message content is rejected")]
        public async Task Api05_OversizedMessage()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("api05_owner"); var member = await this.fixture.RegisterAndLoginAsync("api05_member"); var chatId = await this.fixture.CreateChatAsync(owner, member); using var client = this.fixture.Client(owner.Token); using var response = await client.PostAsJsonAsync($"/api/chats/{chatId}/messages", new { chatId, content = new string('x', 10001), attachmentIds = Array.Empty<string>() }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "API-06 Blank message search query is rejected")]
        public async Task Api06_BlankSearch()
        {
            var user = await this.fixture.RegisterAndLoginAsync("api06"); using var client = this.fixture.Client(user.Token); using var response = await client.GetAsync("/api/messages/chat/search?query=%20%20"); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "API-07 Unknown generated-image download returns not found")]
        public async Task Api07_UnknownImageDownload()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/generated-images/does-not-exist/download"); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(DisplayName = "API-08 Unknown attachment thumbnail returns not found")]
        public async Task Api08_UnknownThumbnail()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/attachments/thumbnail/does-not-exist"); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
