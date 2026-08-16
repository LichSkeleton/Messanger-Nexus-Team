namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class ChatAndFolderE2ETests
    {
        private readonly E2EFixture fixture;
        public ChatAndFolderE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "CHAT-01 Group creation includes creator and participants")]
        public async Task Chat01_CreateGroup()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat01_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat01_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(owner.Token); using var response = await client.GetAsync($"/api/chats/{chatId}"); using var json = await E2EFixture.ReadJsonAsync(response);
            var ids = json.RootElement.GetProperty("participantIds").EnumerateArray().Select(x => x.GetString()).ToArray(); Assert.Contains(owner.Id, ids); Assert.Contains(member.Id, ids);
        }

        [Fact(DisplayName = "CHAT-02 Duplicate chat name for one user is rejected")]
        public async Task Chat02_DuplicateName()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat02_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat02_member"); var name = "Duplicate " + Guid.NewGuid().ToString("N");
            using var client = this.fixture.Client(owner.Token); var body = new { name, type = 1, participantIds = new[] { member.Id } };
            using var first = await client.PostAsJsonAsync("/api/chats", body); Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            using var second = await client.PostAsJsonAsync("/api/chats", body); Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        }

        [Fact(DisplayName = "CHAT-03 Missing participant ID is rejected")]
        public async Task Chat03_MissingParticipant()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat03"); using var client = this.fixture.Client(owner.Token);
            using var response = await client.PostAsJsonAsync("/api/chats", new { name = "Missing", type = 1, participantIds = new[] { "does-not-exist" } });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "CHAT-04 User chat list includes newly created group")]
        public async Task Chat04_ListIncludesCreated()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat04_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat04_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(member.Token); using var response = await client.GetAsync("/api/chats"); using var json = await E2EFixture.ReadJsonAsync(response);
            Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetString() == chatId);
        }

        [Fact(DisplayName = "CHAT-05 Owner can update group metadata")]
        public async Task Chat05_OwnerUpdatesGroup()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat05_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat05_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(owner.Token); using var response = await client.PutAsJsonAsync($"/api/chats/{chatId}", new { name = "Updated Group", description = "Updated description" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = await E2EFixture.ReadJsonAsync(response); Assert.Equal("Updated Group", json.RootElement.GetProperty("name").GetString());
        }

        [Fact(DisplayName = "CHAT-06 Member can leave a group")]
        public async Task Chat06_MemberLeavesGroup()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat06_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat06_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var client = this.fixture.Client(member.Token); using var leave = await client.PostAsync($"/api/chats/{chatId}/leave", null); Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
            using var list = await client.GetAsync("/api/chats"); using var json = await E2EFixture.ReadJsonAsync(list); Assert.DoesNotContain(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetString() == chatId);
        }

        [Fact(DisplayName = "CHAT-07 Owner leave transfers group ownership")]
        public async Task Chat07_OwnerLeaveTransfersOwnership()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat07_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat07_member"); var chatId = await this.fixture.CreateChatAsync(owner, member);
            using var ownerClient = this.fixture.Client(owner.Token); using var leave = await ownerClient.PostAsync($"/api/chats/{chatId}/leave", null); Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
            using var memberClient = this.fixture.Client(member.Token); using var update = await memberClient.PutAsJsonAsync($"/api/chats/{chatId}", new { name = "New Owner Group" }); Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        }

        [Fact(DisplayName = "CHAT-08 Direct message cannot be left like a group")]
        public async Task Chat08_DirectMessageCannotBeLeft()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat08_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat08_member"); using var client = this.fixture.Client(owner.Token);
            using var create = await client.PostAsJsonAsync("/api/chats", new { name = "Direct " + Guid.NewGuid().ToString("N"), type = 0, participantIds = new[] { member.Id } }); Assert.Equal(HttpStatusCode.Created, create.StatusCode); using var json = await E2EFixture.ReadJsonAsync(create); var chatId = json.RootElement.GetProperty("id").GetString();
            using var leave = await client.PostAsync($"/api/chats/{chatId}/leave", null); Assert.Equal(HttpStatusCode.BadRequest, leave.StatusCode);
        }

        [Fact(DisplayName = "CHAT-09 Deleting chat removes it from subsequent reads")]
        public async Task Chat09_DeleteChat()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("chat09_owner"); var member = await this.fixture.RegisterAndLoginAsync("chat09_member"); var chatId = await this.fixture.CreateChatAsync(owner, member); await this.fixture.SendMessageAsync(owner, chatId, "cascade");
            using var client = this.fixture.Client(owner.Token); using var delete = await client.DeleteAsync($"/api/chats/{chatId}"); Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            using var get = await client.GetAsync($"/api/chats/{chatId}"); Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact(DisplayName = "FOLDER-01 Folder create, update and delete lifecycle")]
        public async Task Folder01_FullLifecycle()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("folder01_owner"); var member = await this.fixture.RegisterAndLoginAsync("folder01_member"); var chatId = await this.fixture.CreateChatAsync(owner, member); using var client = this.fixture.Client(owner.Token);
            using var create = await client.PostAsJsonAsync("/api/folders", new { name = "Important", chatIds = new[] { chatId } }); Assert.Equal(HttpStatusCode.Created, create.StatusCode); using var created = await E2EFixture.ReadJsonAsync(create); var folderId = created.RootElement.GetProperty("id").GetString();
            using var update = await client.PutAsJsonAsync($"/api/folders/{folderId}", new { name = "Renamed", chatIds = new[] { chatId } }); Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            using var delete = await client.DeleteAsync($"/api/folders/{folderId}"); Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            using var get = await client.GetAsync($"/api/folders/{folderId}"); Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact(DisplayName = "FOLDER-02 User cannot read another user's folder")]
        [Trait("Category", "Regression")]
        public async Task Folder02_OwnershipIsolation()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("folder02_owner"); var member = await this.fixture.RegisterAndLoginAsync("folder02_member"); var outsider = await this.fixture.RegisterAndLoginAsync("folder02_out"); var chatId = await this.fixture.CreateChatAsync(owner, member); using var ownerClient = this.fixture.Client(owner.Token);
            using var create = await ownerClient.PostAsJsonAsync("/api/folders", new { name = "Private", chatIds = new[] { chatId } }); using var json = await E2EFixture.ReadJsonAsync(create); var folderId = json.RootElement.GetProperty("id").GetString();
            using var outsiderClient = this.fixture.Client(outsider.Token); using var get = await outsiderClient.GetAsync($"/api/folders/{folderId}"); Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact(DisplayName = "FOLDER-03 Folder list includes the authenticated user's folder")]
        public async Task Folder03_ListIncludesCreated()
        {
            var user = await this.fixture.RegisterAndLoginAsync("folder03"); var member = await this.fixture.RegisterAndLoginAsync("folder03_member"); var chatId = await this.fixture.CreateChatAsync(user, member); using var client = this.fixture.Client(user.Token);
            using var create = await client.PostAsJsonAsync("/api/folders", new { name = "Listed", chatIds = new[] { chatId } }); Assert.Equal(HttpStatusCode.Created, create.StatusCode); using var created = await E2EFixture.ReadJsonAsync(create); var folderId = created.RootElement.GetProperty("id").GetString();
            using var list = await client.GetAsync("/api/folders"); Assert.Equal(HttpStatusCode.OK, list.StatusCode); using var json = await E2EFixture.ReadJsonAsync(list); Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetString() == folderId);
        }

        [Fact(DisplayName = "FOLDER-04 User cannot update another user's folder")]
        public async Task Folder04_UpdateOwnershipIsolation()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("folder04_owner"); var member = await this.fixture.RegisterAndLoginAsync("folder04_member"); var outsider = await this.fixture.RegisterAndLoginAsync("folder04_out"); var chatId = await this.fixture.CreateChatAsync(owner, member); using var ownerClient = this.fixture.Client(owner.Token);
            using var create = await ownerClient.PostAsJsonAsync("/api/folders", new { name = "Private", chatIds = new[] { chatId } }); using var created = await E2EFixture.ReadJsonAsync(create); var folderId = created.RootElement.GetProperty("id").GetString();
            using var outsiderClient = this.fixture.Client(outsider.Token); using var update = await outsiderClient.PutAsJsonAsync($"/api/folders/{folderId}", new { name = "Stolen", chatIds = new[] { chatId } }); Assert.True(update.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized);
        }

        [Fact(DisplayName = "FOLDER-05 User cannot delete another user's folder")]
        public async Task Folder05_DeleteOwnershipIsolation()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("folder05_owner"); var member = await this.fixture.RegisterAndLoginAsync("folder05_member"); var outsider = await this.fixture.RegisterAndLoginAsync("folder05_out"); var chatId = await this.fixture.CreateChatAsync(owner, member); using var ownerClient = this.fixture.Client(owner.Token);
            using var create = await ownerClient.PostAsJsonAsync("/api/folders", new { name = "Private", chatIds = new[] { chatId } }); using var created = await E2EFixture.ReadJsonAsync(create); var folderId = created.RootElement.GetProperty("id").GetString();
            using var outsiderClient = this.fixture.Client(outsider.Token); using var delete = await outsiderClient.DeleteAsync($"/api/folders/{folderId}"); Assert.True(delete.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized);
            using var verify = await ownerClient.GetAsync($"/api/folders/{folderId}"); Assert.Equal(HttpStatusCode.OK, verify.StatusCode);
        }
    }
}
