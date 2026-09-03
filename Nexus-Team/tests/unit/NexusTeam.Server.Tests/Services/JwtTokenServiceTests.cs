namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using Microsoft.IdentityModel.Tokens;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Models;
    using Serilog;
    using Xunit;

    public class JwtTokenServiceTests
    {
        private const string Secret = "0123456789abcdef0123456789abcdef";

        [Fact]
        public async Task GenerateAccessTokenAsync_CreatesSignedTokenWithExpectedMetadata()
        {
            var now = new DateTime(2026, 8, 12, 12, 0, 0, DateTimeKind.Utc);
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(now, logger: logger);
            var user = CreateUser();

            var tokenString = await service.GenerateAccessTokenAsync(user);
            var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            Assert.Equal(SecurityAlgorithms.HmacSha256, token.Header.Alg);
            Assert.Equal("NexusTeam", token.Issuer);
            Assert.Contains("NexusTeam.Client", token.Audiences);
            Assert.Equal(now, token.ValidFrom);
            Assert.Equal(now.AddMinutes(60), token.ValidTo);
            Assert.Equal(user.Id, token.Subject);
            Assert.Contains(token.Claims, claim => claim.Value == user.Username);
            Assert.Contains(token.Claims, claim => claim.Value == user.Email);
            Assert.Contains(token.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti);
        }

        [Fact]
        public async Task GenerateAccessTokenAsync_WithDeviceId_IncludesDeviceClaim()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(DateTime.UtcNow, logger: logger);

            var tokenString = await service.GenerateAccessTokenAsync(CreateUser(), "device-123");
            var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenString);

            Assert.Contains(token.Claims, claim => claim.Type == "device_id" && claim.Value == "device-123");
        }

        [Fact]
        public async Task ValidateIdentityAsync_WithDeviceToken_ReturnsUserAndDeviceIds()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(DateTime.UtcNow, logger: logger);
            var token = await service.GenerateAccessTokenAsync(CreateUser(), "device-123");

            var identity = await service.ValidateIdentityAsync(token);

            Assert.NotNull(identity);
            Assert.Equal("user-123", identity.UserId);
            Assert.Equal("device-123", identity.DeviceId);
        }

        [Fact]
        public async Task GenerateAccessTokenAsync_ForSameUser_CreatesUniqueTokens()
        {
            var now = DateTime.UtcNow;
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(now, logger: logger);
            var user = CreateUser();

            var first = await service.GenerateAccessTokenAsync(user);
            var second = await service.GenerateAccessTokenAsync(user);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithGeneratedToken_ReturnsUserId()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(DateTime.UtcNow, logger: logger);
            var token = await service.GenerateAccessTokenAsync(CreateUser());

            var userId = await service.ValidateTokenAsync(token);

            Assert.Equal("user-123", userId);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithTamperedSignature_ReturnsNull()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(DateTime.UtcNow, logger: logger);
            var token = await service.GenerateAccessTokenAsync(CreateUser());
            var tampered = TamperSignature(token);

            var userId = await service.ValidateTokenAsync(tampered);

            Assert.Null(userId);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithDifferentSigningKey_ReturnsNull()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var issuer = CreateService(
                DateTime.UtcNow,
                secret: "abcdef0123456789abcdef0123456789",
                logger: logger);
            var validator = CreateService(DateTime.UtcNow, logger: logger);
            var token = await issuer.GenerateAccessTokenAsync(CreateUser());

            var userId = await validator.ValidateTokenAsync(token);

            Assert.Null(userId);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithDifferentIssuer_ReturnsNull()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var issuer = CreateService(DateTime.UtcNow, issuer: "OtherIssuer", logger: logger);
            var validator = CreateService(DateTime.UtcNow, logger: logger);
            var token = await issuer.GenerateAccessTokenAsync(CreateUser());

            var userId = await validator.ValidateTokenAsync(token);

            Assert.Null(userId);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithExpiredTokenBeyondClockSkew_ReturnsNull()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(
                DateTime.UtcNow.AddMinutes(-10),
                expirationMinutes: 1,
                logger: logger);
            var token = await service.GenerateAccessTokenAsync(CreateUser());

            var userId = await service.ValidateTokenAsync(token);

            Assert.Null(userId);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithRecentlyExpiredToken_AcceptsConfiguredClockSkew()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(
                DateTime.UtcNow.AddMinutes(-3),
                expirationMinutes: 1,
                logger: logger);
            var token = await service.GenerateAccessTokenAsync(CreateUser());

            var userId = await service.ValidateTokenAsync(token);

            Assert.Equal("user-123", userId);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithNoSubjectClaim_ReturnsNull()
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(DateTime.UtcNow, logger: logger);
            var token = CreateTokenWithoutSubject();

            var userId = await service.ValidateTokenAsync(token);

            Assert.Null(userId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not-a-jwt")]
        public async Task ValidateTokenAsync_WithMalformedToken_ReturnsNull(string? token)
        {
            using var logger = new LoggerConfiguration().CreateLogger();
            var service = CreateService(DateTime.UtcNow, logger: logger);

            var userId = await service.ValidateTokenAsync(token!);

            Assert.Null(userId);
        }

        private static JwtTokenService CreateService(
            DateTime now,
            string secret = Secret,
            string issuer = "NexusTeam",
            int expirationMinutes = 60,
            ILogger? logger = null)
        {
            var options = Options.Create(new JwtOptions
            {
                SecretKey = secret,
                Issuer = issuer,
                Audience = "NexusTeam.Client",
                ExpirationMinutes = expirationMinutes,
                RefreshTokenExpirationDays = 7,
            });

            return new JwtTokenService(
                options,
                new FixedClock(now),
                logger ?? new LoggerConfiguration().CreateLogger());
        }

        private static User CreateUser()
        {
            return new User
            {
                Id = "user-123",
                Username = "alice",
                Email = "alice@example.com",
            };
        }

        private static string TamperSignature(string token)
        {
            var segments = token.Split('.');
            var signature = segments[2].ToCharArray();
            signature[0] = signature[0] == 'A' ? 'B' : 'A';
            segments[2] = new string(signature);
            return string.Join('.', segments);
        }

        private static string CreateTokenWithoutSubject()
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
            var token = new JwtSecurityToken(
                issuer: "NexusTeam",
                audience: "NexusTeam.Client",
                claims: new[] { new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) },
                notBefore: DateTime.UtcNow.AddMinutes(-1),
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private sealed class FixedClock : IClock
        {
            public FixedClock(DateTime utcNow)
            {
                this.UtcNow = utcNow;
            }

            public DateTime UtcNow { get; }
        }
    }
}
