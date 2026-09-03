namespace NexusTeam.E2E.Tests
{
    using System;
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class AuthenticationE2ETests
    {
        private readonly E2EFixture fixture;

        public AuthenticationE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "AUTH-01 Valid registration creates a user")]
        public async Task Auth01_ValidRegistration()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth01");
            Assert.StartsWith("auth01_", user.Username);
            Assert.False(string.IsNullOrWhiteSpace(user.Id));
        }

        [Fact(DisplayName = "AUTH-02 Duplicate username is rejected")]
        public async Task Auth02_DuplicateUsername()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth02");
            using var client = this.fixture.Client();
            using var response = await client.PostAsJsonAsync("/api/auth/register", new { username = user.Username, email = $"other_{Guid.NewGuid():N}@example.test", password = user.Password, displayName = "Duplicate" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "AUTH-03 Duplicate email is rejected")]
        public async Task Auth03_DuplicateEmail()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth03");
            using var client = this.fixture.Client();
            using var response = await client.PostAsJsonAsync("/api/auth/register", new { username = $"other_{Guid.NewGuid():N}", email = user.Email, password = user.Password, displayName = "Duplicate" });
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "AUTH-04 Invalid email fails request validation")]
        public async Task Auth04_InvalidEmail()
        {
            using var client = this.fixture.Client();
            using var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegistration("auth04", email: "not-an-email"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "AUTH-05 Weak password fails request validation")]
        public async Task Auth05_WeakPassword()
        {
            using var client = this.fixture.Client();
            using var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegistration("auth05", password: "weak"));
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact(DisplayName = "AUTH-06 Registered user can log in by username")]
        public async Task Auth06_LoginByUsername()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth06");
            using var client = this.fixture.Client();
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = user.Username, password = user.Password, deviceId = Guid.NewGuid(), deviceName = "E2E Browser" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var json = await E2EFixture.ReadJsonAsync(response);
            Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("accessToken").GetString()));
        }

        [Fact(DisplayName = "AUTH-07 Registered user can log in by email")]
        public async Task Auth07_LoginByEmail()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth07");
            using var client = this.fixture.Client();
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = user.Email, password = user.Password, deviceId = Guid.NewGuid(), deviceName = "E2E Browser" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact(DisplayName = "AUTH-08 Invalid credentials return unauthorized")]
        public async Task Auth08_InvalidCredentials()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth08");
            using var client = this.fixture.Client();
            using var response = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = user.Username, password = "WrongPass123", deviceId = Guid.NewGuid(), deviceName = "E2E Browser" });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "AUTH-09 Expired token cannot access a protected endpoint")]
        public async Task Auth09_ExpiredToken()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth09");
            using var client = this.fixture.Client(E2EFixture.CreateExpiredToken(user.Id));
            using var response = await client.GetAsync("/api/chats");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(DisplayName = "AUTH-10 Logout removes the active session")]
        public async Task Auth10_Logout()
        {
            var user = await this.fixture.RegisterAndLoginAsync("auth10");
            using var client = this.fixture.Client(user.Token);
            using var response = await client.PostAsync("/api/auth/logout", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static object ValidRegistration(string prefix, string? email = null, string password = "ValidPass123")
        {
            var suffix = Guid.NewGuid().ToString("N");
            return new { username = $"{prefix}_{suffix}", email = email ?? $"{prefix}_{suffix}@example.test", password, displayName = "E2E User" };
        }
    }
}
