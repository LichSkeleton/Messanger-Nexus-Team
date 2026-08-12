namespace NexusTeam.Server.Tests.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NexusTeam.Server.Data.Repositories;
    using NexusTeam.Server.Middleware;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Models;
    using Serilog;
    using Xunit;

    public class JwtAuthenticationMiddlewareTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Basic credentials")]
        [InlineData("BearerToken")]
        public async Task InvokeAsync_WithoutBearerToken_SkipsAuthenticationAndContinues(
            string? authorizationHeader)
        {
            var nextCalls = 0;
            var tokenService = new FakeJwtTokenService(_ => Task.FromResult<string?>("user-1"));
            var userRepository = new FakeUserRepository(_ => Task.FromResult<User?>(null));
            var context = new DefaultHttpContext();
            if (authorizationHeader != null)
            {
                context.Request.Headers.Authorization = authorizationHeader;
            }

            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new JwtAuthenticationMiddleware(
                _ =>
                {
                    nextCalls++;
                    return Task.CompletedTask;
                },
                logger);

            await middleware.InvokeAsync(context, tokenService, userRepository);

            Assert.Equal(0, tokenService.ValidateCalls);
            Assert.Equal(0, userRepository.GetByIdCalls);
            Assert.Equal(1, nextCalls);
            Assert.False(context.Items.ContainsKey("UserId"));
        }

        [Fact]
        public async Task InvokeAsync_WithValidTokenAndExistingUser_PopulatesContext()
        {
            var user = new User { Id = "user-1", Username = "alice" };
            var tokenService = new FakeJwtTokenService(_ => Task.FromResult<string?>(user.Id));
            var userRepository = new FakeUserRepository(_ => Task.FromResult<User?>(user));
            var context = new DefaultHttpContext();
            context.Request.Headers.Authorization = "bEaReR   valid-token  ";
            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new JwtAuthenticationMiddleware(_ => Task.CompletedTask, logger);

            await middleware.InvokeAsync(context, tokenService, userRepository);

            Assert.Equal("valid-token", tokenService.LastToken);
            Assert.Equal("user-1", userRepository.LastUserId);
            Assert.Equal("user-1", context.Items["UserId"]);
            Assert.Same(user, context.Items["User"]);
        }

        [Fact]
        public async Task InvokeAsync_WhenTokenReturnsNoUserId_SkipsRepositoryAndContinues()
        {
            var nextCalls = 0;
            var tokenService = new FakeJwtTokenService(_ => Task.FromResult<string?>(null));
            var userRepository = new FakeUserRepository(_ => Task.FromResult<User?>(null));
            var context = CreateBearerContext("valid-but-empty-token");
            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new JwtAuthenticationMiddleware(
                _ =>
                {
                    nextCalls++;
                    return Task.CompletedTask;
                },
                logger);

            await middleware.InvokeAsync(context, tokenService, userRepository);

            Assert.Equal(1, tokenService.ValidateCalls);
            Assert.Equal(0, userRepository.GetByIdCalls);
            Assert.Equal(1, nextCalls);
            Assert.Empty(context.Items);
        }

        [Fact]
        public async Task InvokeAsync_WhenUserDoesNotExist_DoesNotAuthenticateContext()
        {
            var tokenService = new FakeJwtTokenService(_ => Task.FromResult<string?>("missing-user"));
            var userRepository = new FakeUserRepository(_ => Task.FromResult<User?>(null));
            var context = CreateBearerContext("valid-token");
            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new JwtAuthenticationMiddleware(_ => Task.CompletedTask, logger);

            await middleware.InvokeAsync(context, tokenService, userRepository);

            Assert.Equal(1, userRepository.GetByIdCalls);
            Assert.False(context.Items.ContainsKey("UserId"));
            Assert.False(context.Items.ContainsKey("User"));
        }

        [Fact]
        public async Task InvokeAsync_WhenTokenValidationThrows_SwallowsFailureAndContinues()
        {
            var nextCalls = 0;
            var tokenService = new FakeJwtTokenService(
                _ => Task.FromException<string?>(new InvalidOperationException("invalid token")));
            var userRepository = new FakeUserRepository(_ => Task.FromResult<User?>(null));
            var context = CreateBearerContext("invalid-token");
            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new JwtAuthenticationMiddleware(
                _ =>
                {
                    nextCalls++;
                    return Task.CompletedTask;
                },
                logger);

            await middleware.InvokeAsync(context, tokenService, userRepository);

            Assert.Equal(0, userRepository.GetByIdCalls);
            Assert.Equal(1, nextCalls);
            Assert.Empty(context.Items);
        }

        [Fact]
        public async Task InvokeAsync_WhenUserLookupThrows_SwallowsFailureAndContinues()
        {
            var nextCalls = 0;
            var tokenService = new FakeJwtTokenService(_ => Task.FromResult<string?>("user-1"));
            var userRepository = new FakeUserRepository(
                _ => Task.FromException<User?>(new InvalidOperationException("database unavailable")));
            var context = CreateBearerContext("valid-token");
            using var logger = new LoggerConfiguration().CreateLogger();
            var middleware = new JwtAuthenticationMiddleware(
                _ =>
                {
                    nextCalls++;
                    return Task.CompletedTask;
                },
                logger);

            await middleware.InvokeAsync(context, tokenService, userRepository);

            Assert.Equal(1, userRepository.GetByIdCalls);
            Assert.Equal(1, nextCalls);
            Assert.Empty(context.Items);
        }

        private static DefaultHttpContext CreateBearerContext(string token)
        {
            var context = new DefaultHttpContext();
            context.Request.Headers.Authorization = $"Bearer {token}";
            return context;
        }

        private sealed class FakeJwtTokenService : IJwtTokenService
        {
            private readonly Func<string, Task<string?>> validate;

            public FakeJwtTokenService(Func<string, Task<string?>> validate)
            {
                this.validate = validate;
            }

            public int ValidateCalls { get; private set; }

            public string? LastToken { get; private set; }

            public Task<string> GenerateAccessTokenAsync(User user)
            {
                throw new NotSupportedException();
            }

            public Task<string?> ValidateTokenAsync(string token)
            {
                this.ValidateCalls++;
                this.LastToken = token;
                return this.validate(token);
            }
        }

        private sealed class FakeUserRepository : IUserRepository
        {
            private readonly Func<string, Task<User?>> getById;

            public FakeUserRepository(Func<string, Task<User?>> getById)
            {
                this.getById = getById;
            }

            public int GetByIdCalls { get; private set; }

            public string? LastUserId { get; private set; }

            public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
            {
                this.GetByIdCalls++;
                this.LastUserId = id;
                return this.getById(id);
            }

            public Task<User?> GetByUsernameAsync(
                string username,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<User?> GetByEmailAsync(
                string email,
                CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task CreateAsync(User user, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }

            public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
            {
                throw new NotSupportedException();
            }
        }
    }
}
