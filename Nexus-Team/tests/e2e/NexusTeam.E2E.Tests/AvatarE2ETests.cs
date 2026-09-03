namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class AvatarE2ETests
    {
        private static readonly byte[] TinyPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        private readonly E2EFixture fixture;

        public AvatarE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "AVATAR-01 Default avatar is publicly downloadable")]
        public async Task Avatar01_DefaultAvatar()
        {
            using var client = this.fixture.Client();
            using var response = await client.GetAsync("/api/users/avatar/default");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
            Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
        }

        [Fact(DisplayName = "AVATAR-02 User avatar upload is persisted and downloadable")]
        public async Task Avatar02_UserUploadRoundTrip()
        {
            var user = await this.fixture.RegisterAndLoginAsync("avatar02");
            using var client = this.fixture.Client(user.Token);
            using var form = Form("avatar.png", "image/png", TinyPng);
            using var upload = await client.PostAsync("/api/users/avatar/upload", form);
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            using var json = await E2EFixture.ReadJsonAsync(upload);
            Assert.Equal($"/api/users/avatar/{user.Id}", json.RootElement.GetProperty("avatarUrl").GetString());

            using var download = await client.GetAsync($"/api/users/avatar/{user.Id}");
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("image/jpeg", download.Content.Headers.ContentType?.MediaType);
            Assert.NotEmpty(await download.Content.ReadAsByteArrayAsync());
        }

        [Fact(DisplayName = "AVATAR-03 Unsupported user avatar extension is rejected")]
        public async Task Avatar03_InvalidExtension()
        {
            var user = await this.fixture.RegisterAndLoginAsync("avatar03");
            using var client = this.fixture.Client(user.Token);
            using var form = Form("avatar.exe", "application/octet-stream", new byte[] { 1 });
            using var response = await client.PostAsync("/api/users/avatar/upload", form);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "AVATAR-04 Anonymous user avatar upload is rejected")]
        public async Task Avatar04_AnonymousUpload()
        {
            using var client = this.fixture.Client();
            using var form = Form("avatar.png", "image/png", TinyPng);
            using var response = await client.PostAsync("/api/users/avatar/upload", form);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "AVATAR-05 Unknown user avatar falls back to the default image")]
        public async Task Avatar05_UnknownUserFallback()
        {
            using var client = this.fixture.Client();
            using var response = await client.GetAsync("/api/users/avatar/unknown-user");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
            Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
        }

        [Fact(DisplayName = "AVATAR-06 Group owner can upload and download a chat avatar")]
        public async Task Avatar06_GroupOwnerUpload()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("avatar06_owner");
            var member = await this.fixture.RegisterAndLoginAsync("avatar06_member");
            var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(owner.Token);
            using var form = Form("group.png", "image/png", TinyPng);
            using var upload = await client.PostAsync($"/api/chats/{chatId}/avatar", form);
            Assert.Equal(HttpStatusCode.OK, upload.StatusCode);
            using var json = await E2EFixture.ReadJsonAsync(upload);
            var avatarUrl = json.RootElement.GetProperty("avatarUrl").GetString();
            Assert.Equal($"/api/users/avatar/chat_{chatId}", avatarUrl);

            using var download = await client.GetAsync(avatarUrl);
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.NotEmpty(await download.Content.ReadAsByteArrayAsync());
        }

        [Fact(DisplayName = "AVATAR-07 Group member cannot replace the owner's chat avatar")]
        public async Task Avatar07_GroupMemberRejected()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("avatar07_owner");
            var member = await this.fixture.RegisterAndLoginAsync("avatar07_member");
            var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(member.Token);
            using var form = Form("group.png", "image/png", TinyPng);
            using var response = await client.PostAsync($"/api/chats/{chatId}/avatar", form);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "AVATAR-08 Direct messages cannot have a group avatar")]
        public async Task Avatar08_DirectMessageRejected()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("avatar08_owner");
            var member = await this.fixture.RegisterAndLoginAsync("avatar08_member");
            using var client = this.fixture.Client(owner.Token);
            using var create = await client.PostAsJsonAsync("/api/chats", new
            {
                name = "Direct " + Guid.NewGuid().ToString("N"),
                type = 0,
                participantIds = new[] { member.Id },
            });
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            using var created = await E2EFixture.ReadJsonAsync(create);
            var chatId = created.RootElement.GetProperty("id").GetString();
            using var form = Form("direct.png", "image/png", TinyPng);
            using var response = await client.PostAsync($"/api/chats/{chatId}/avatar", form);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "AVATAR-09 Anonymous chat avatar upload is rejected")]
        public async Task Avatar09_AnonymousChatUpload()
        {
            using var client = this.fixture.Client();
            using var form = Form("group.png", "image/png", TinyPng);
            using var response = await client.PostAsync("/api/chats/unknown/avatar", form);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static MultipartFormDataContent Form(string fileName, string contentType, byte[] bytes)
        {
            var form = new MultipartFormDataContent();
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            form.Add(content, "file", fileName);
            return form;
        }
    }
}
