namespace NexusTeam.Shared.Tests.Helpers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Shared.Configuration;
    using NexusTeam.Shared.Helpers;
    using Xunit;

    public class PasswordHasherTests
    {
        [Fact]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new PasswordHasher(null!));

            Assert.Equal("options", exception.ParamName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9)]
        public void Constructor_WithWorkFactorBelowTen_ThrowsArgumentException(int workFactor)
        {
            var options = CreateOptions(workFactor);

            var exception = Assert.Throws<ArgumentException>(() =>
                new PasswordHasher(options));

            Assert.Equal("options", exception.ParamName);
        }

        [Fact]
        public async Task HashPasswordAsync_WithValidPassword_ProducesVerifiableBcryptHash()
        {
            var hasher = CreateHasher();

            var hash = await hasher.HashPasswordAsync("Correct-Horse-Battery-Staple");
            var isValid = await hasher.VerifyPasswordAsync(
                "Correct-Horse-Battery-Staple",
                hash);

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
        public async Task VerifyPasswordAsync_WithMalformedHash_ReturnsFalse()
        {
            var hasher = CreateHasher();

            var result = await hasher.VerifyPasswordAsync("valid-password", "not-a-bcrypt-hash");

            Assert.False(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task HashPasswordAsync_WithMissingPassword_ThrowsArgumentException(string? password)
        {
            var hasher = CreateHasher();

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                hasher.HashPasswordAsync(password!));

            Assert.Equal("password", exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VerifyPasswordAsync_WithMissingPassword_ThrowsArgumentException(string? password)
        {
            var hasher = CreateHasher();

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                hasher.VerifyPasswordAsync(password!, "not-used"));

            Assert.Equal("password", exception.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task VerifyPasswordAsync_WithMissingHash_ThrowsArgumentException(string? hash)
        {
            var hasher = CreateHasher();

            var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
                hasher.VerifyPasswordAsync("valid-password", hash!));

            Assert.Equal("hash", exception.ParamName);
        }

        private static PasswordHasher CreateHasher()
        {
            return new PasswordHasher(CreateOptions(workFactor: 10));
        }

        private static IOptions<PasswordHashingOptions> CreateOptions(int workFactor)
        {
            return Options.Create(new PasswordHashingOptions { WorkFactor = workFactor });
        }
    }
}
