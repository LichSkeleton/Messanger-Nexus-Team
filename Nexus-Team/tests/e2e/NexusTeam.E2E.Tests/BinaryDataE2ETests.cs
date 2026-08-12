namespace NexusTeam.E2E.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class BinaryDataE2ETests
    {
        private static readonly byte[] TinyPng = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
        private readonly E2EFixture fixture;
        public BinaryDataE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "ATT-01 Attachment upload and download preserve bytes")]
        public async Task Att01_UploadDownloadRoundTrip()
        {
            var context = await this.MessageAsync("att01"); var bytes = Encoding.UTF8.GetBytes("attachment payload");
            var attachment = await this.UploadAsync(context.User, context.MessageId, "notes.txt", "text/plain", bytes);
            using var client = this.fixture.Client(context.User.Token); using var response = await client.GetAsync($"/api/attachments/download/{attachment.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(bytes, await response.Content.ReadAsByteArrayAsync());
        }

        [Fact(DisplayName = "ATT-02 Image upload generates downloadable thumbnail")]
        public async Task Att02_ImageThumbnail()
        {
            var context = await this.MessageAsync("att02"); var attachment = await this.UploadAsync(context.User, context.MessageId, "tiny.png", "image/png", TinyPng);
            using var client = this.fixture.Client(context.User.Token); using var response = await client.GetAsync($"/api/attachments/thumbnail/{attachment.Id}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType); Assert.NotEmpty(await response.Content.ReadAsByteArrayAsync());
        }

        [Fact(DisplayName = "ATT-03 Disallowed executable upload is rejected")]
        public async Task Att03_DisallowedExtension()
        {
            var context = await this.MessageAsync("att03"); using var client = this.fixture.Client(context.User.Token); using var form = Form("malware.exe", "application/octet-stream", new byte[] { 1 });
            using var response = await client.PostAsync($"/api/attachments/upload?messageId={context.MessageId}", form); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "ATT-04 Attachment update replaces content")]
        public async Task Att04_UpdateContent()
        {
            var context = await this.MessageAsync("att04"); var attachment = await this.UploadAsync(context.User, context.MessageId, "notes.txt", "text/plain", Encoding.UTF8.GetBytes("before"));
            using var client = this.fixture.Client(context.User.Token); using var form = Form("notes.txt", "text/plain", Encoding.UTF8.GetBytes("after")); using var update = await client.PutAsync($"/api/attachments/{attachment.Id}", form); Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            using var download = await client.GetAsync($"/api/attachments/download/{attachment.Id}"); Assert.Equal("after", await download.Content.ReadAsStringAsync());
        }

        [Fact(DisplayName = "ATT-05 Attachment delete removes metadata and file")]
        public async Task Att05_Delete()
        {
            var context = await this.MessageAsync("att05"); var attachment = await this.UploadAsync(context.User, context.MessageId, "notes.txt", "text/plain", new byte[] { 1 }); using var client = this.fixture.Client(context.User.Token);
            using var delete = await client.DeleteAsync($"/api/attachments/{attachment.Id}"); Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode); using var get = await client.GetAsync($"/api/attachments/download/{attachment.Id}"); Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact(DisplayName = "ATT-06 Message attachment listing includes uploaded file")]
        public async Task Att06_ListByMessage()
        {
            var context = await this.MessageAsync("att06"); var attachment = await this.UploadAsync(context.User, context.MessageId, "notes.txt", "text/plain", new byte[] { 1 }); using var client = this.fixture.Client(context.User.Token);
            using var response = await client.GetAsync($"/api/attachments/message/{context.MessageId}"); using var json = await E2EFixture.ReadJsonAsync(response); Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetString() == attachment.Id);
        }

        [Fact(DisplayName = "ATT-07 Unknown attachment returns not found")]
        public async Task Att07_UnknownAttachment()
        {
            using var client = this.fixture.Client(); using var response = await client.GetAsync("/api/attachments/download/does-not-exist"); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact(DisplayName = "IMG-01 Generated image metadata can be created and listed")]
        public async Task Img01_CreateAndList()
        {
            var user = await this.fixture.RegisterAndLoginAsync("img01"); var image = await this.CreateImageAsync(user, "orange robot"); using var client = this.fixture.Client(user.Token); using var list = await client.GetAsync("/api/generated-images"); using var json = await E2EFixture.ReadJsonAsync(list); Assert.Contains(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetString() == image.Id);
        }

        [Fact(DisplayName = "IMG-02 Base64 image data can be saved and downloaded")]
        public async Task Img02_SaveAndDownload()
        {
            var user = await this.fixture.RegisterAndLoginAsync("img02"); var image = await this.CreateImageAsync(user, "download robot"); using var client = this.fixture.Client(user.Token);
            using var save = await client.PostAsJsonAsync($"/api/generated-images/{image.Id}/data", new { imageDataBase64 = Convert.ToBase64String(TinyPng) }); Assert.Equal(HttpStatusCode.OK, save.StatusCode);
            using var download = await client.GetAsync($"/api/generated-images/{image.Id}/download"); Assert.Equal(HttpStatusCode.OK, download.StatusCode); Assert.Equal(TinyPng, await download.Content.ReadAsByteArrayAsync());
        }

        [Fact(DisplayName = "IMG-03 Invalid base64 image data is rejected")]
        public async Task Img03_InvalidBase64()
        {
            var user = await this.fixture.RegisterAndLoginAsync("img03"); var image = await this.CreateImageAsync(user, "bad data"); using var client = this.fixture.Client(user.Token); using var response = await client.PostAsJsonAsync($"/api/generated-images/{image.Id}/data", new { imageDataBase64 = "!invalid!" }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "IMG-04 Image owner can delete generated image")]
        public async Task Img04_OwnerDelete()
        {
            var user = await this.fixture.RegisterAndLoginAsync("img04"); var image = await this.CreateImageAsync(user, "delete robot"); using var client = this.fixture.Client(user.Token); using var delete = await client.DeleteAsync($"/api/generated-images/{image.Id}"); Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode); using var get = await client.GetAsync($"/api/generated-images/{image.Id}"); Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        }

        [Fact(DisplayName = "IMG-05 Non-owner cannot delete generated image")]
        public async Task Img05_NonOwnerDelete()
        {
            var owner = await this.fixture.RegisterAndLoginAsync("img05_owner"); var outsider = await this.fixture.RegisterAndLoginAsync("img05_out"); var image = await this.CreateImageAsync(owner, "private robot"); using var client = this.fixture.Client(outsider.Token); using var delete = await client.DeleteAsync($"/api/generated-images/{image.Id}"); Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        }

        [Fact(DisplayName = "IMG-06 Recent prompt history includes created prompt")]
        public async Task Img06_RecentPrompts()
        {
            var user = await this.fixture.RegisterAndLoginAsync("img06"); var prompt = "prompt_" + Guid.NewGuid().ToString("N"); await this.CreateImageAsync(user, prompt); using var client = this.fixture.Client(user.Token); using var response = await client.GetAsync("/api/generated-images/prompts?limit=10"); using var json = await E2EFixture.ReadJsonAsync(response); Assert.Contains(prompt, json.RootElement.EnumerateArray().Select(x => x.GetString()));
        }

        private static MultipartFormDataContent Form(string fileName, string contentType, byte[] bytes)
        {
            var form = new MultipartFormDataContent(); var content = new ByteArrayContent(bytes); content.Headers.ContentType = new MediaTypeHeaderValue(contentType); form.Add(content, "file", fileName); return form;
        }

        private async Task<Attachment> UploadAsync(TestUser user, string messageId, string fileName, string contentType, byte[] bytes)
        {
            using var client = this.fixture.Client(user.Token); using var form = Form(fileName, contentType, bytes); form.Add(new StringContent(messageId), "messageId"); using var response = await client.PostAsync("/api/attachments/upload", form); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = await E2EFixture.ReadJsonAsync(response); return new Attachment(json.RootElement.GetProperty("id").GetString()!);
        }

        private async Task<MessageContext> MessageAsync(string prefix)
        {
            var owner = await this.fixture.RegisterAndLoginAsync(prefix + "_owner"); var member = await this.fixture.RegisterAndLoginAsync(prefix + "_member"); var chatId = await this.fixture.CreateChatAsync(owner, member); var message = await this.fixture.SendMessageAsync(owner, chatId, "attachment host"); return new MessageContext(owner, message.GetProperty("id").GetString()!);
        }

        private async Task<ImageRecord> CreateImageAsync(TestUser user, string prompt)
        {
            using var client = this.fixture.Client(user.Token); using var response = await client.PostAsJsonAsync("/api/generated-images", new { prompt, model = "e2e", imageUrl = "https://example.test/image.png", width = 1, height = 1 }); Assert.Equal(HttpStatusCode.Created, response.StatusCode); using var json = await E2EFixture.ReadJsonAsync(response); return new ImageRecord(json.RootElement.GetProperty("id").GetString()!);
        }

        private sealed record Attachment(string Id);
        private sealed record MessageContext(TestUser User, string MessageId);
        private sealed record ImageRecord(string Id);
    }
}
