namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using NexusTeam.Shared.Dtos;
    using NexusTeam.Shared.Enums;
    using NexusTeam.Shared.Exceptions;
    using NexusTeam.Shared.Models;
    using Serilog;
    using Xunit;

    public class AuthServiceTests
    {
        private static readonly DateTime FixedNow = new DateTime(
            2026,
            8,
            12,
            12,
            30,
            0,
            DateTimeKind.Utc);

        [Fact]
        public async Task RegisterAsync_WithNewUser_CreatesExpectedUserAndResponse()
        {
            var fixture = new AuthFixture();
            fixture.StatusService.Status = UserStatus.Online;

            var response = await fixture.Service.RegisterAsync(CreateRegisterRequest());

            Assert.True(response.Success);
            Assert.Null(response.ErrorMessage);
            var created = Assert.IsType<User>(fixture.Repository.CreatedUser);
            Assert.Equal("generated-user-id", created.Id);
            Assert.Equal("alice", created.Username);
            Assert.Equal("alice@example.com", created.Email);
            Assert.Equal("hashed:Secure123", created.PasswordHash);
            Assert.Equal("Alice", created.DisplayName);
            Assert.Equal("/api/users/avatar/generated-user-id", created.AvatarUrl);
            Assert.Equal(UserStatus.Offline, created.Status);
            Assert.Equal(FixedNow, created.CreatedAt);
            Assert.Equal(FixedNow, created.UpdatedAt);
            Assert.NotNull(response.User);
            Assert.Equal(UserStatus.Online, response.User.Status);
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateUsername_StopsBeforeEmailAndHashChecks()
        {
            var fixture = new AuthFixture();
            fixture.Repository.UserByUsername = CreateUser();

            var response = await fixture.Service.RegisterAsync(CreateRegisterRequest());

            Assert.False(response.Success);
            Assert.Equal("Username already exists", response.ErrorMessage);
            Assert.Equal(0, fixture.Repository.EmailLookupCalls);
            Assert.Equal(0, fixture.PasswordHasher.HashCalls);
            Assert.Null(fixture.Repository.CreatedUser);
        }

        [Fact]
        public async Task RegisterAsync_WithDuplicateEmail_DoesNotHashOrCreateUser()
        {
            var fixture = new AuthFixture();
            fixture.Repository.UserByEmail = CreateUser();

            var response = await fixture.Service.RegisterAsync(CreateRegisterRequest());

            Assert.False(response.Success);
            Assert.Equal("Email already exists", response.ErrorMessage);
            Assert.Equal(1, fixture.Repository.EmailLookupCalls);
            Assert.Equal(0, fixture.PasswordHasher.HashCalls);
            Assert.Null(fixture.Repository.CreatedUser);
        }

        [Fact]
        public async Task RegisterAsync_WhenDependencyThrows_ReturnsGenericFailure()
        {
            var fixture = new AuthFixture();
            fixture.PasswordHasher.HashException = new InvalidOperationException("hasher failed");

            var response = await fixture.Service.RegisterAsync(CreateRegisterRequest());

            Assert.False(response.Success);
            Assert.Equal("Registration failed due to an unexpected error", response.ErrorMessage);
            Assert.Null(response.User);
        }

        [Fact]
        public async Task LoginAsync_WithUsername_UsesUsernameLookupAndReturnsTokens()
        {
            var fixture = new AuthFixture();
            fixture.Repository.UserByUsername = CreateUser();
            fixture.StatusService.Status = UserStatus.Away;

            var response = await fixture.Service.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = "alice",
                Password = "Secure123",
            });

            Assert.Equal(1, fixture.Repository.UsernameLookupCalls);
            Assert.Equal(0, fixture.Repository.EmailLookupCalls);
            Assert.Equal("access-token", response.AccessToken);
            Assert.Equal("refresh-token", response.RefreshToken);
            Assert.Equal(3600, response.ExpiresIn);
            Assert.Equal(UserStatus.Away, response.User.Status);
            Assert.Equal("user-1", fixture.RefreshTokenService.LastUserId);
        }

        [Fact]
        public async Task LoginAsync_WithEmail_UsesEmailLookup()
        {
            var fixture = new AuthFixture();
            fixture.Repository.UserByEmail = CreateUser();

            await fixture.Service.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = "alice@example.com",
                Password = "Secure123",
            });

            Assert.Equal(0, fixture.Repository.UsernameLookupCalls);
            Assert.Equal(1, fixture.Repository.EmailLookupCalls);
        }

        [Fact]
        public async Task LoginAsync_WithUnknownUser_ThrowsGenericAuthenticationException()
        {
            var fixture = new AuthFixture();

            var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
                fixture.Service.LoginAsync(new LoginRequest
                {
                    UsernameOrEmail = "unknown",
                    Password = "Secure123",
                }));

            Assert.Equal("Invalid username/email or password", exception.Message);
            Assert.Equal(0, fixture.PasswordHasher.VerifyCalls);
            Assert.Equal(0, fixture.JwtTokenService.GenerateCalls);
        }

        [Fact]
        public async Task LoginAsync_WithWrongPassword_ThrowsSameGenericAuthenticationException()
        {
            var fixture = new AuthFixture();
            fixture.Repository.UserByUsername = CreateUser();
            fixture.PasswordHasher.PasswordIsValid = false;

            var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
                fixture.Service.LoginAsync(new LoginRequest
                {
                    UsernameOrEmail = "alice",
                    Password = "wrong",
                }));

            Assert.Equal("Invalid username/email or password", exception.Message);
            Assert.Equal(0, fixture.JwtTokenService.GenerateCalls);
            Assert.Equal(0, fixture.RefreshTokenService.GenerateCalls);
        }

        [Fact]
        public async Task LoginAsync_WhenTokenGenerationFails_WrapsUnexpectedException()
        {
            var fixture = new AuthFixture();
            fixture.Repository.UserByUsername = CreateUser();
            var cause = new InvalidOperationException("signing key unavailable");
            fixture.JwtTokenService.ExceptionToThrow = cause;

            var exception = await Assert.ThrowsAsync<AuthenticationException>(() =>
                fixture.Service.LoginAsync(new LoginRequest
                {
                    UsernameOrEmail = "alice",
                    Password = "Secure123",
                }));

            Assert.Equal("Login failed due to an unexpected error", exception.Message);
            Assert.Same(cause, exception.InnerException);
        }

        [Fact]
        public async Task RegisterAndLogin_ForwardCancellationTokenToDependencies()
        {
            var fixture = new AuthFixture();
            fixture.Repository.UserByUsername = null;
            using var source = new CancellationTokenSource();

            await fixture.Service.RegisterAsync(CreateRegisterRequest(), source.Token);
            fixture.Repository.UserByUsername = CreateUser();
            await fixture.Service.LoginAsync(new LoginRequest
            {
                UsernameOrEmail = "alice",
                Password = "Secure123",
            }, source.Token);

            Assert.Equal(source.Token, fixture.Repository.LastCancellationToken);
            Assert.Equal(source.Token, fixture.RefreshTokenService.LastCancellationToken);
            Assert.Equal(source.Token, fixture.StatusService.LastCancellationToken);
        }

        private static RegisterRequest CreateRegisterRequest()
        {
            return new RegisterRequest
            {
                Username = "alice",
                Email = "alice@example.com",
                Password = "Secure123",
                DisplayName = "Alice",
            };
        }

        private static User CreateUser()
        {
            return new User
            {
                Id = "user-1",
                Username = "alice",
                Email = "alice@example.com",
                PasswordHash = "stored-hash",
                DisplayName = "Alice",
                AvatarUrl = "/avatar.png",
                Status = UserStatus.Offline,
                LastSeenAt = FixedNow.AddHours(-1),
            };
        }

        private sealed class AuthFixture
        {
            public AuthFixture()
            {
                this.Service = new AuthService(
                    this.Repository,
                    this.PasswordHasher,
                    this.JwtTokenService,
                    this.RefreshTokenService,
                    new FixedIdGenerator(),
                    new FixedClock(),
                    new LoggerConfiguration().CreateLogger(),
                    this.StatusService,
                    new UnusedAvatarService());
            }

            public FakeUserRepository Repository { get; } = new FakeUserRepository();

            public FakePasswordHasher PasswordHasher { get; } = new FakePasswordHasher();

            public FakeJwtTokenService JwtTokenService { get; } = new FakeJwtTokenService();

            public FakeRefreshTokenService RefreshTokenService { get; } = new FakeRefreshTokenService();

            public FakeUserStatusService StatusService { get; } = new FakeUserStatusService();

            public AuthService Service { get; }
        }

        private sealed class FakeUserRepository : IUserRepository
        {
            public User? UserByUsername { get; set; }

            public User? UserByEmail { get; set; }

            public User? CreatedUser { get; private set; }

            public int UsernameLookupCalls { get; private set; }

            public int EmailLookupCalls { get; private set; }

            public CancellationToken LastCancellationToken { get; private set; }

            public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
            {
                this.UsernameLookupCalls++;
                this.LastCancellationToken = cancellationToken;
                return Task.FromResult(this.UserByUsername);
            }

            public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
            {
                this.EmailLookupCalls++;
                this.LastCancellationToken = cancellationToken;
                return Task.FromResult(this.UserByEmail);
            }

            public Task CreateAsync(User user, CancellationToken cancellationToken = default)
            {
                this.CreatedUser = user;
                this.LastCancellationToken = cancellationToken;
                return Task.CompletedTask;
            }

            public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private sealed class FakePasswordHasher : IPasswordHasher
        {
            public int HashCalls { get; private set; }

            public int VerifyCalls { get; private set; }

            public bool PasswordIsValid { get; set; } = true;

            public Exception? HashException { get; set; }

            public Task<string> HashPasswordAsync(string password)
            {
                this.HashCalls++;
                return this.HashException == null
                    ? Task.FromResult("hashed:" + password)
                    : Task.FromException<string>(this.HashException);
            }

            public Task<bool> VerifyPasswordAsync(string password, string hash)
            {
                this.VerifyCalls++;
                return Task.FromResult(this.PasswordIsValid);
            }
        }

        private sealed class FakeJwtTokenService : IJwtTokenService
        {
            public int GenerateCalls { get; private set; }

            public Exception? ExceptionToThrow { get; set; }

            public Task<string> GenerateAccessTokenAsync(User user)
            {
                this.GenerateCalls++;
                return this.ExceptionToThrow == null
                    ? Task.FromResult("access-token")
                    : Task.FromException<string>(this.ExceptionToThrow);
            }

            public Task<string?> ValidateTokenAsync(string token) => throw new NotSupportedException();
        }

        private sealed class FakeRefreshTokenService : IRefreshTokenService
        {
            public int GenerateCalls { get; private set; }

            public string? LastUserId { get; private set; }

            public CancellationToken LastCancellationToken { get; private set; }

            public Task<string> GenerateRefreshTokenAsync(
                string userId,
                CancellationToken cancellationToken = default)
            {
                this.GenerateCalls++;
                this.LastUserId = userId;
                this.LastCancellationToken = cancellationToken;
                return Task.FromResult("refresh-token");
            }

            public Task<string?> ValidateRefreshTokenAsync(
                string refreshToken,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task RevokeRefreshTokenAsync(
                string refreshToken,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private sealed class FixedIdGenerator : IIdGenerator
        {
            public string GenerateId() => "generated-user-id";
        }

        private sealed class FixedClock : IClock
        {
            public DateTime UtcNow => FixedNow;
        }

        private sealed class FakeUserStatusService : IUserStatusService
        {
            public UserStatus Status { get; set; } = UserStatus.Offline;

            public CancellationToken LastCancellationToken { get; private set; }

            public Task<UserStatus> GetStatusAsync(
                string userId,
                CancellationToken cancellationToken = default)
            {
                this.LastCancellationToken = cancellationToken;
                return Task.FromResult(this.Status);
            }

            public Task<UserStatus> GetPublicStatusAsync(string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task SetStatusAsync(string userId, UserStatus status, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<bool> GetInvisiblePreferenceAsync(string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task SetInvisiblePreferenceAsync(string userId, bool isInvisible, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task RemoveStatusAsync(string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }

        private sealed class UnusedAvatarService : IAvatarService
        {
            public Task<string> SaveAvatarAsync(
                string userId,
                string fileName,
                Stream fileStream,
                CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<Stream?> GetAvatarStreamAsync(string userId, CancellationToken cancellationToken = default)
                => throw new NotSupportedException();

            public Task<Stream> GetDefaultAvatarStreamAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException();
        }
    }
}
