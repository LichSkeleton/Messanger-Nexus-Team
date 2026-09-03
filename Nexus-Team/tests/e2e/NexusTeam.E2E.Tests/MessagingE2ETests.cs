namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class MessagingE2ETests
    {
        private readonly E2EFixture fixture;

        public MessagingE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "MSG-01 Sent message is persisted and returned")]
        public async Task Msg01_SendAndPersist()
        {
            var conversation = await this.ConversationAsync("msg01");
            var sent = await this.fixture.SendMessageAsync(conversation.Owner, conversation.ChatId, "persist me");
            using var client = this.fixture.Client(conversation.Owner.Token); using var response = await client.GetAsync($"/api/chats/{conversation.ChatId}/messages");
            using var json = await E2EFixture.ReadJsonAsync(response);
            Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetString() == sent.GetProperty("id").GetString());
        }

        [Fact(DisplayName = "MSG-02 WebSocket message is received by both participants")]
        public async Task Msg02_RealTimeReceipt()
        {
            var c = await this.ConversationAsync("msg02"); await using var owner = this.fixture.Socket(); await using var member = this.fixture.Socket();
            await owner.ConnectAsync(); await member.ConnectAsync(); await owner.AuthenticateAsync(c.Owner.Token); await member.AuthenticateAsync(c.Member.Token);
            await owner.SendAsync(NewMessage(c.ChatId, "real time"));
            using var ownerReceipt = await owner.ReceiveTypeAsync("newMessage"); using var memberReceipt = await member.ReceiveTypeAsync("newMessage");
            Assert.Equal("real time", ownerReceipt.RootElement.GetProperty("payload").GetProperty("content").GetString());
            Assert.Equal(ownerReceipt.RootElement.GetProperty("messageId").GetString(), memberReceipt.RootElement.GetProperty("messageId").GetString());
        }

        [Fact(DisplayName = "MSG-03 Unicode and emoji content round-trips unchanged")]
        public async Task Msg03_EmojiSupport()
        {
            var c = await this.ConversationAsync("msg03"); const string content = "Merhaba 👋🏽🚀 café 日本語";
            var sent = await this.fixture.SendMessageAsync(c.Owner, c.ChatId, content);
            Assert.Equal(content, sent.GetProperty("content").GetString());
        }

        [Fact(DisplayName = "MSG-04 Sender can edit own message")]
        public async Task Msg04_OwnerCanEdit()
        {
            var c = await this.ConversationAsync("msg04"); var sent = await this.fixture.SendMessageAsync(c.Owner, c.ChatId, "before");
            await using var socket = this.fixture.Socket(); await socket.ConnectAsync(); await socket.AuthenticateAsync(c.Owner.Token);
            await socket.SendAsync(new { type = "editMessage", payload = new { messageId = sent.GetProperty("id").GetString(), content = "after" } });
            using var edited = await socket.ReceiveTypeAsync("editMessage");
            Assert.Equal("after", edited.RootElement.GetProperty("payload").GetProperty("content").GetString());
            Assert.NotEqual(JsonValueKind.Null, edited.RootElement.GetProperty("payload").GetProperty("editedAt").ValueKind);
        }

        [Fact(DisplayName = "MSG-05 Another participant cannot edit message ownership")]
        public async Task Msg05_NonOwnerCannotEdit()
        {
            var c = await this.ConversationAsync("msg05"); var sent = await this.fixture.SendMessageAsync(c.Owner, c.ChatId, "owned");
            await using var socket = this.fixture.Socket(); await socket.ConnectAsync(); await socket.AuthenticateAsync(c.Member.Token);
            await socket.SendAsync(new { type = "editMessage", payload = new { messageId = sent.GetProperty("id").GetString(), content = "hijacked" } });
            using var error = await socket.ReceiveTypeAsync("error");
            Assert.True(error.RootElement.TryGetProperty("error", out _));
        }

        [Fact(DisplayName = "MSG-06 Sender can delete own message and participants receive notification")]
        public async Task Msg06_OwnerCanDelete()
        {
            var c = await this.ConversationAsync("msg06"); var sent = await this.fixture.SendMessageAsync(c.Owner, c.ChatId, "delete me");
            await using var owner = this.fixture.Socket(); await using var member = this.fixture.Socket(); await owner.ConnectAsync(); await member.ConnectAsync(); await owner.AuthenticateAsync(c.Owner.Token); await member.AuthenticateAsync(c.Member.Token);
            await owner.SendAsync(new { type = "deleteMessage", payload = new { messageId = sent.GetProperty("id").GetString() } });
            using var notification = await member.ReceiveTypeAsync("deleteMessage");
            Assert.Equal(sent.GetProperty("id").GetString(), notification.RootElement.GetProperty("messageId").GetString());
        }

        [Fact(DisplayName = "MSG-07 Another participant cannot delete message ownership")]
        public async Task Msg07_NonOwnerCannotDelete()
        {
            var c = await this.ConversationAsync("msg07"); var sent = await this.fixture.SendMessageAsync(c.Owner, c.ChatId, "owned");
            await using var member = this.fixture.Socket(); await member.ConnectAsync(); await member.AuthenticateAsync(c.Member.Token);
            await member.SendAsync(new { type = "deleteMessage", payload = new { messageId = sent.GetProperty("id").GetString() } });
            using var error = await member.ReceiveTypeAsync("error");
            Assert.Contains("sender", error.RootElement.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
        }

        [Fact(DisplayName = "MSG-08 Reaction can be added and is persisted")]
        public async Task Msg08_AddReaction()
        {
            var c = await this.ConversationAsync("msg08"); var sent = await this.fixture.SendMessageAsync(c.Owner, c.ChatId, "react"); var id = sent.GetProperty("id").GetString();
            using var client = this.fixture.Client(c.Member.Token); using var response = await client.PostAsJsonAsync($"/api/chats/{c.ChatId}/messages/{id}/reactions", new { emoji = "🔥" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = await E2EFixture.ReadJsonAsync(response);
            Assert.Contains(c.Member.Id, json.RootElement.GetProperty("reactions").GetProperty("🔥").EnumerateArray().Select(x => x.GetString()));
        }

        [Fact(DisplayName = "MSG-09 Reaction can be removed")]
        public async Task Msg09_RemoveReaction()
        {
            var c = await this.ConversationAsync("msg09"); var sent = await this.fixture.SendMessageAsync(c.Owner, c.ChatId, "react"); var id = sent.GetProperty("id").GetString();
            using var client = this.fixture.Client(c.Member.Token); using var add = await client.PostAsJsonAsync($"/api/chats/{c.ChatId}/messages/{id}/reactions", new { emoji = "👍" }); add.EnsureSuccessStatusCode();
            using var response = await client.DeleteAsync($"/api/chats/{c.ChatId}/messages/{id}/reactions/{Uri.EscapeDataString("👍")}"); Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = await E2EFixture.ReadJsonAsync(response); Assert.False(json.RootElement.GetProperty("reactions").TryGetProperty("👍", out _));
        }

        [Fact(DisplayName = "MSG-10 Search and pagination return the expected subset")]
        public async Task Msg10_SearchAndPagination()
        {
            var c = await this.ConversationAsync("msg10"); var marker = "needle_" + Guid.NewGuid().ToString("N");
            await this.fixture.SendMessageAsync(c.Owner, c.ChatId, "ordinary"); await this.fixture.SendMessageAsync(c.Owner, c.ChatId, marker);
            using var client = this.fixture.Client(c.Owner.Token); using var search = await client.GetAsync($"/api/messages/{c.ChatId}/search?query={marker}"); using var searchJson = await E2EFixture.ReadJsonAsync(search);
            Assert.Single(searchJson.RootElement.EnumerateArray());
            using var page = await client.GetAsync($"/api/chats/{c.ChatId}/messages?limit=1&offset=0"); using var pageJson = await E2EFixture.ReadJsonAsync(page);
            Assert.Single(pageJson.RootElement.EnumerateArray());
        }

        private static object NewMessage(string chatId, string content) => new { type = "newMessage", payload = new { chatId, content, attachmentIds = Array.Empty<string>() } };

        private async Task<Conversation> ConversationAsync(string prefix)
        {
            var owner = await this.fixture.RegisterAndLoginAsync(prefix + "_owner"); var member = await this.fixture.RegisterAndLoginAsync(prefix + "_member");
            return new Conversation(owner, member, await this.fixture.CreateChatAsync(owner, member));
        }

        private sealed record Conversation(TestUser Owner, TestUser Member, string ChatId);
    }
}
