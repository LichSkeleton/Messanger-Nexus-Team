namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;
    using StackExchange.Redis;
    using Xunit;

    public class RateLimitServiceTests
    {
        [Fact]
        public async Task IsLoginAllowedAsync_AtLimitAllowsThenRejectsNextAttempt()
        {
            var (service, database) = CreateService(loginMaxAttempts: 2);

            var first = await service.IsLoginAllowedAsync("alice");
            var second = await service.IsLoginAllowedAsync("alice");
            var third = await service.IsLoginAllowedAsync("alice");

            Assert.True(first);
            Assert.True(second);
            Assert.False(third);
            Assert.Equal(3, database.Counters["ratelimit:login:alice"]);
        }

        [Fact]
        public async Task IsLoginAllowedAsync_OnFirstAttempt_SetsConfiguredExpirationOnce()
        {
            var (service, database) = CreateService(loginWindowSeconds: 45);

            await service.IsLoginAllowedAsync("alice");
            await service.IsLoginAllowedAsync("alice");

            Assert.Equal(1, database.ExpireCalls);
            Assert.Equal("ratelimit:login:alice", database.LastExpireKey);
            Assert.Equal(TimeSpan.FromSeconds(45), database.LastExpiration);
        }

        [Fact]
        public async Task IsMessageSendAllowedAsync_UsesIndependentMessageKeyAndLimit()
        {
            var (service, database) = CreateService(messageMaxAttempts: 1);

            var loginAllowed = await service.IsLoginAllowedAsync("user-1");
            var firstMessage = await service.IsMessageSendAllowedAsync("user-1");
            var secondMessage = await service.IsMessageSendAllowedAsync("user-1");

            Assert.True(loginAllowed);
            Assert.True(firstMessage);
            Assert.False(secondMessage);
            Assert.Equal(1, database.Counters["ratelimit:login:user-1"]);
            Assert.Equal(2, database.Counters["ratelimit:message:user-1"]);
        }

        [Fact]
        public async Task IsLoginAllowedAsync_UsesSeparateCounterPerIdentifier()
        {
            var (service, database) = CreateService(loginMaxAttempts: 1);

            var alice = await service.IsLoginAllowedAsync("alice");
            var bob = await service.IsLoginAllowedAsync("bob");

            Assert.True(alice);
            Assert.True(bob);
            Assert.Equal(1, database.Counters["ratelimit:login:alice"]);
            Assert.Equal(1, database.Counters["ratelimit:login:bob"]);
        }

        [Fact]
        public async Task GetLoginRateLimitResetTimeAsync_WithTtl_ReturnsWholeSeconds()
        {
            var (service, database) = CreateService();
            database.TimeToLive = TimeSpan.FromMilliseconds(12900);

            var result = await service.GetLoginRateLimitResetTimeAsync("alice");

            Assert.Equal(12, result);
            Assert.Equal("ratelimit:login:alice", database.LastTtlKey);
        }

        [Fact]
        public async Task GetMessageRateLimitResetTimeAsync_WithoutTtl_ReturnsZero()
        {
            var (service, database) = CreateService();
            database.TimeToLive = null;

            var result = await service.GetMessageRateLimitResetTimeAsync("user-1");

            Assert.Equal(0, result);
            Assert.Equal("ratelimit:message:user-1", database.LastTtlKey);
        }

        [Fact]
        public async Task IsLoginAllowedAsync_WhenRedisFails_FailsOpen()
        {
            var (service, database) = CreateService();
            database.ExceptionToThrow = new RedisConnectionException(
                ConnectionFailureType.UnableToConnect,
                "Redis unavailable");

            var result = await service.IsLoginAllowedAsync("alice");

            Assert.True(result);
        }

        [Fact]
        public async Task GetResetTimeAsync_WhenRedisFails_ReturnsZero()
        {
            var (service, database) = CreateService();
            database.ExceptionToThrow = new RedisConnectionException(
                ConnectionFailureType.UnableToConnect,
                "Redis unavailable");

            var result = await service.GetLoginRateLimitResetTimeAsync("alice");

            Assert.Equal(0, result);
        }

        private static (RateLimitService Service, RedisDatabaseProxy Database) CreateService(
            int loginMaxAttempts = 5,
            int loginWindowSeconds = 300,
            int messageMaxAttempts = 60,
            int messageWindowSeconds = 60)
        {
            var database = RedisDatabaseProxy.Create(out var proxy);
            var multiplexer = new FakeRedisMultiplexer(database);
            var options = Options.Create(new RateLimitOptions
            {
                LoginMaxAttempts = loginMaxAttempts,
                LoginWindowSeconds = loginWindowSeconds,
                MessageMaxAttempts = messageMaxAttempts,
                MessageWindowSeconds = messageWindowSeconds,
            });
            var logger = new LoggerConfiguration().CreateLogger();

            return (new RateLimitService(multiplexer, options, logger), proxy);
        }

        private sealed class FakeRedisMultiplexer : IRedisMultiplexer
        {
            private readonly IDatabase database;

            public FakeRedisMultiplexer(IDatabase database)
            {
                this.database = database;
            }

            public IConnectionMultiplexer GetMultiplexer()
            {
                throw new NotSupportedException();
            }

            public IDatabase GetDatabase(int db = -1)
            {
                return this.database;
            }
        }

        private class RedisDatabaseProxy : DispatchProxy
        {
            public RedisDatabaseProxy()
            {
            }

            public Dictionary<string, long> Counters { get; } = new Dictionary<string, long>();

            public int ExpireCalls { get; private set; }

            public string? LastExpireKey { get; private set; }

            public TimeSpan? LastExpiration { get; private set; }

            public string? LastTtlKey { get; private set; }

            public TimeSpan? TimeToLive { get; set; }

            public Exception? ExceptionToThrow { get; set; }

            public static IDatabase Create(out RedisDatabaseProxy proxy)
            {
                var database = DispatchProxy.Create<IDatabase, RedisDatabaseProxy>();
                proxy = (RedisDatabaseProxy)(object)database;
                return database;
            }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (this.ExceptionToThrow != null)
                {
                    throw this.ExceptionToThrow;
                }

                if (targetMethod == null || args == null)
                {
                    throw new InvalidOperationException("Redis proxy received an invalid invocation.");
                }

                var key = args[0]?.ToString() ?? string.Empty;
                switch (targetMethod.Name)
                {
                    case "StringIncrementAsync":
                        this.Counters.TryGetValue(key, out var current);
                        current++;
                        this.Counters[key] = current;
                        return Task.FromResult(current);
                    case "KeyExpireAsync":
                        this.ExpireCalls++;
                        this.LastExpireKey = key;
                        this.LastExpiration = (TimeSpan?)args[1];
                        return Task.FromResult(true);
                    case "KeyTimeToLiveAsync":
                        this.LastTtlKey = key;
                        return Task.FromResult(this.TimeToLive);
                    default:
                        throw new NotSupportedException(
                            $"Redis method {targetMethod.Name} is not configured for this test.");
                }
            }
        }
    }
}
