namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;
    using Xunit;

    public class RefreshTokenServiceTests
    {
        [Fact]
        public async Task GenerateRefreshTokenAsync_ReturnsCryptographicallySizedBase64Token()
        {
            var (service, _) = CreateService();

            var token = await service.GenerateRefreshTokenAsync("user-1");
            var bytes = Convert.FromBase64String(token);

            Assert.Equal(32, bytes.Length);
            Assert.Equal(44, token.Length);
        }

        [Fact]
        public async Task GenerateRefreshTokenAsync_AcrossCalls_ReturnsUniqueTokens()
        {
            var (service, _) = CreateService();
            var tokens = new HashSet<string>();

            for (var index = 0; index < 20; index++)
            {
                tokens.Add(await service.GenerateRefreshTokenAsync("user-1"));
            }

            Assert.Equal(20, tokens.Count);
        }

        [Fact]
        public async Task GenerateRefreshTokenAsync_StoresUserWithConfiguredExpiration()
        {
            var (service, cache) = CreateService(refreshExpirationDays: 14);
            using var source = new CancellationTokenSource();

            var token = await service.GenerateRefreshTokenAsync("user-1", source.Token);

            Assert.Equal("refresh_token:" + token, cache.LastSetKey);
            Assert.Equal("user-1", cache.LastSetValue);
            Assert.Equal(TimeSpan.FromDays(14), cache.LastExpiration);
            Assert.Equal(source.Token, cache.LastCancellationToken);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WithStoredToken_ReturnsUserId()
        {
            var (service, _) = CreateService();
            var token = await service.GenerateRefreshTokenAsync("user-1");

            var userId = await service.ValidateRefreshTokenAsync(token);

            Assert.Equal("user-1", userId);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WithUnknownToken_ReturnsNull()
        {
            var (service, cache) = CreateService();

            var userId = await service.ValidateRefreshTokenAsync("unknown-token");

            Assert.Null(userId);
            Assert.Equal("refresh_token:unknown-token", cache.LastGetKey);
        }

        [Fact]
        public async Task ValidateRefreshTokenAsync_WithEmptyCachedUser_ReturnsNull()
        {
            var (service, cache) = CreateService();
            cache.Values["refresh_token:token-1"] = string.Empty;

            var userId = await service.ValidateRefreshTokenAsync("token-1");

            Assert.Null(userId);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_RemovesNamespacedCacheEntry()
        {
            var (service, cache) = CreateService();
            var token = await service.GenerateRefreshTokenAsync("user-1");

            await service.RevokeRefreshTokenAsync(token);
            var userId = await service.ValidateRefreshTokenAsync(token);

            Assert.Equal("refresh_token:" + token, cache.LastRemovedKey);
            Assert.Null(userId);
        }

        [Fact]
        public async Task RevokeRefreshTokenAsync_ForwardsCancellationToken()
        {
            var (service, cache) = CreateService();
            using var source = new CancellationTokenSource();

            await service.RevokeRefreshTokenAsync("token-1", source.Token);

            Assert.Equal(source.Token, cache.LastCancellationToken);
        }

        private static (RefreshTokenService Service, FakeCacheService Cache) CreateService(
            int refreshExpirationDays = 7)
        {
            var cache = new FakeCacheService();
            var options = Options.Create(new JwtOptions
            {
                RefreshTokenExpirationDays = refreshExpirationDays,
            });
            var logger = new LoggerConfiguration().CreateLogger();
            return (new RefreshTokenService(cache, options, logger), cache);
        }

        private sealed class FakeCacheService : ICacheService
        {
            public Dictionary<string, object?> Values { get; } = new Dictionary<string, object?>();

            public string? LastSetKey { get; private set; }

            public object? LastSetValue { get; private set; }

            public TimeSpan? LastExpiration { get; private set; }

            public string? LastGetKey { get; private set; }

            public string? LastRemovedKey { get; private set; }

            public CancellationToken LastCancellationToken { get; private set; }

            public Task<T?> GetAsync<T>(
                string key,
                CancellationToken cancellationToken = default)
            {
                this.LastGetKey = key;
                this.LastCancellationToken = cancellationToken;
                return Task.FromResult(
                    this.Values.TryGetValue(key, out var value) && value is T typed
                        ? typed
                        : default);
            }

            public Task SetAsync<T>(
                string key,
                T value,
                TimeSpan? expiration = null,
                CancellationToken cancellationToken = default)
            {
                this.LastSetKey = key;
                this.LastSetValue = value;
                this.LastExpiration = expiration;
                this.LastCancellationToken = cancellationToken;
                this.Values[key] = value;
                return Task.CompletedTask;
            }

            public Task RemoveAsync(
                string key,
                CancellationToken cancellationToken = default)
            {
                this.LastRemovedKey = key;
                this.LastCancellationToken = cancellationToken;
                this.Values.Remove(key);
                return Task.CompletedTask;
            }

            public Task<bool> ExistsAsync(
                string key,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.Values.ContainsKey(key));
            }
        }
    }
}
