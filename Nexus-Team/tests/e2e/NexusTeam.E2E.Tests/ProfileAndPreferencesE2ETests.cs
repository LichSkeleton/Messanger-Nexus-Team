namespace NexusTeam.E2E.Tests
{
    using System.Linq;
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class ProfileAndPreferencesE2ETests
    {
        private readonly E2EFixture fixture;
        public ProfileAndPreferencesE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "PREF-01 First preference read creates documented defaults")]
        public async Task Pref01_Defaults()
        {
            var user = await this.fixture.RegisterAndLoginAsync("pref01"); using var client = this.fixture.Client(user.Token); using var response = await client.GetAsync("/api/preferences"); using var json = await E2EFixture.ReadJsonAsync(response);
            Assert.Equal("light", json.RootElement.GetProperty("theme").GetString()); Assert.Equal("en", json.RootElement.GetProperty("language").GetString()); Assert.True(json.RootElement.GetProperty("notificationsEnabled").GetBoolean());
        }

        [Fact(DisplayName = "PREF-02 Preference update persists across reads")]
        public async Task Pref02_UpdatePersists()
        {
            var user = await this.fixture.RegisterAndLoginAsync("pref02"); using var client = this.fixture.Client(user.Token); using var update = await client.PutAsJsonAsync("/api/preferences", new { userId = "ignored", notificationsEnabled = false, soundEnabled = false, theme = "dark", language = "tr", mutedChats = new[] { "chat-1" } }); Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            using var get = await client.GetAsync("/api/preferences"); using var json = await E2EFixture.ReadJsonAsync(get); Assert.Equal("dark", json.RootElement.GetProperty("theme").GetString()); Assert.False(json.RootElement.GetProperty("soundEnabled").GetBoolean());
        }

        [Fact(DisplayName = "PREF-03 Invalid preference theme is rejected")]
        public async Task Pref03_InvalidTheme()
        {
            var user = await this.fixture.RegisterAndLoginAsync("pref03"); using var client = this.fixture.Client(user.Token); using var response = await client.PutAsJsonAsync("/api/preferences", new { notificationsEnabled = true, soundEnabled = true, theme = "neon", language = "en", mutedChats = new string[0] }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "PROFILE-01 Display name update persists")]
        public async Task Profile01_UpdateDisplayName()
        {
            var user = await this.fixture.RegisterAndLoginAsync("profile01"); using var client = this.fixture.Client(user.Token); using var response = await client.PutAsJsonAsync("/api/users/profile", new { displayName = "Updated E2E Name" }); Assert.Equal(HttpStatusCode.OK, response.StatusCode); using var json = await E2EFixture.ReadJsonAsync(response); Assert.Equal("Updated E2E Name", json.RootElement.GetProperty("displayName").GetString());
        }

        [Fact(DisplayName = "PROFILE-02 Empty display name is rejected")]
        public async Task Profile02_EmptyDisplayName()
        {
            var user = await this.fixture.RegisterAndLoginAsync("profile02"); using var client = this.fixture.Client(user.Token); using var response = await client.PutAsJsonAsync("/api/users/profile", new { displayName = " " }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "PROFILE-03 User listing excludes current user")]
        public async Task Profile03_UserListExcludesSelf()
        {
            var user = await this.fixture.RegisterAndLoginAsync("profile03"); using var client = this.fixture.Client(user.Token); using var response = await client.GetAsync("/api/users"); using var json = await E2EFixture.ReadJsonAsync(response); Assert.DoesNotContain(json.RootElement.EnumerateArray(), x => x.GetProperty("id").GetString() == user.Id);
        }

        [Fact(DisplayName = "PRESENCE-01 Invisible status is retained for own status read")]
        public async Task Presence01_InvisibleStatus()
        {
            var user = await this.fixture.RegisterAndLoginAsync("presence01"); using var client = this.fixture.Client(user.Token); using var update = await client.PutAsJsonAsync("/api/users/status", new { status = 4 }); Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            using var get = await client.GetAsync("/api/users/status"); using var json = await E2EFixture.ReadJsonAsync(get); Assert.Equal(4, json.RootElement.GetProperty("status").GetInt32());
        }

        [Fact(DisplayName = "PRESENCE-02 Unsupported status transition is rejected")]
        public async Task Presence02_InvalidStatus()
        {
            var user = await this.fixture.RegisterAndLoginAsync("presence02"); using var client = this.fixture.Client(user.Token); using var response = await client.PutAsJsonAsync("/api/users/status", new { status = 2 }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}
