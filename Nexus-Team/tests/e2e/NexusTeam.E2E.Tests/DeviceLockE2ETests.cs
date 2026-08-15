namespace NexusTeam.E2E.Tests
{
    using System.Net;
    using System.Net.Http.Json;
    using System.Threading.Tasks;
    using Xunit;

    [Collection(E2ECollection.Name)]
    public sealed class DeviceLockE2ETests
    {
        private readonly E2EFixture fixture;

        public DeviceLockE2ETests(E2EFixture fixture) => this.fixture = fixture;

        [Fact(DisplayName = "LOCK-01 Locked device rejects protected API until correct PIN")]
        public async Task Lock01_ProtectedApiRequiresPinAfterManualLock()
        {
            var user = await this.fixture.RegisterAndLoginAsync("lock01");
            using var client = this.fixture.Client(user.Token);

            using var enabled = await client.PostAsJsonAsync("/api/device-lock/enable", new
            {
                accountPassword = user.Password,
                pin = "1234",
                confirmPin = "1234",
                timeoutSeconds = 60,
            });
            Assert.Equal(HttpStatusCode.NoContent, enabled.StatusCode);

            using var locked = await client.PostAsync("/api/device-lock/lock", null);
            Assert.Equal(HttpStatusCode.NoContent, locked.StatusCode);

            using var rejected = await client.GetAsync("/api/chats");
            Assert.Equal((HttpStatusCode)423, rejected.StatusCode);

            using var unlocked = await client.PostAsJsonAsync("/api/device-lock/unlock", new { pin = "1234" });
            Assert.Equal(HttpStatusCode.OK, unlocked.StatusCode);

            using var allowed = await client.GetAsync("/api/chats");
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }

        [Fact(DisplayName = "LOCK-02 Five invalid PINs revoke the device session")]
        public async Task Lock02_FiveInvalidPinsRequireAccountSignIn()
        {
            var user = await this.fixture.RegisterAndLoginAsync("lock02");
            using var client = this.fixture.Client(user.Token);
            using var enabled = await client.PostAsJsonAsync("/api/device-lock/enable", new
            {
                accountPassword = user.Password,
                pin = "1234",
                confirmPin = "1234",
                timeoutSeconds = 60,
            });
            Assert.Equal(HttpStatusCode.NoContent, enabled.StatusCode);
            using var locked = await client.PostAsync("/api/device-lock/lock", null);
            Assert.Equal(HttpStatusCode.NoContent, locked.StatusCode);

            HttpStatusCode lastStatus = HttpStatusCode.OK;
            for (var attempt = 0; attempt < 5; attempt++)
            {
                using var response = await client.PostAsJsonAsync("/api/device-lock/unlock", new { pin = "9999" });
                lastStatus = response.StatusCode;
            }

            Assert.Equal(HttpStatusCode.Unauthorized, lastStatus);
            using var rejected = await client.GetAsync("/api/chats");
            Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        }
    }
}
