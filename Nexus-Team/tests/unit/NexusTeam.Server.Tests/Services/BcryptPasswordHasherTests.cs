namespace NexusTeam.Server.Tests.Services
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services;
    using Xunit;

    public class BcryptPasswordHasherTests
    {
        [Fact]
        public async Task HashPasswordAsync_UsesConfiguredWorkFactorAndVerifiesPassword()
        {
            var hasher = CreateHasher(workFactor: 10);

            var hash = await hasher.HashPasswordAsync("SecurePassword123");
            var isValid = await hasher.VerifyPasswordAsync("SecurePassword123", hash);

            Assert.StartsWith("$2", hash);
            Assert.Contains("$10$", hash);
            Assert.True(isValid);
        }

        [Fact]
        public async Task VerifyPasswordAsync_WithWrongPassword_ReturnsFalse()
        {
            var hasher = CreateHasher();
            var hash = await hasher.HashPasswordAsync("correct-password");

            var result = await hasher.VerifyPasswordAsync("wrong-password", hash);

            Assert.False(result);
        }

        [Fact]
        public async Task HashPasswordAsync_ForSamePassword_UsesUniqueSalt()
        {
            var hasher = CreateHasher();

            var first = await hasher.HashPasswordAsync("same-password");
            var second = await hasher.HashPasswordAsync("same-password");

            Assert.NotEqual(first, second);
            Assert.True(await hasher.VerifyPasswordAsync("same-password", first));
            Assert.True(await hasher.VerifyPasswordAsync("same-password", second));
        }

        [Fact]
        [Trait("Category", "Regression")]
        public async Task VerifyPasswordAsync_WithMalformedHash_ReturnsFalse()
        {
            var hasher = CreateHasher();

            var result = await hasher.VerifyPasswordAsync(
                "valid-password",
                "not-a-bcrypt-hash");

            Assert.False(result);
        }

        private static BcryptPasswordHasher CreateHasher(int workFactor = 10)
        {
            return new BcryptPasswordHasher(
                Options.Create(new BcryptOptions { WorkFactor = workFactor }));
        }
    }
}
