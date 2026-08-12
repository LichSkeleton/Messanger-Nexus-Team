namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Reflection;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using StackExchange.Redis;
    using Xunit;

    public class RedisCacheServiceTests
    {
        [Fact]
        public async Task GetAsync_WhenKeyMissing_ReturnsDefault()
        {
            var (service, proxy) = CreateService();
            proxy.Value = RedisValue.Null;

            Assert.Null(await service.GetAsync<Payload>("missing"));
        }

        [Fact]
        public async Task GetAsync_DeserializesStoredJson()
        {
            var (service, proxy) = CreateService();
            proxy.Value = "{\"Name\":\"Nexus\",\"Count\":3}";

            var result = await service.GetAsync<Payload>("key");

            Assert.NotNull(result);
            Assert.Equal("Nexus", result.Name);
            Assert.Equal(3, result.Count);
            Assert.Equal("key", proxy.LastKey);
        }

        [Fact]
        public async Task GetAsync_WithMalformedJson_PropagatesJsonError()
        {
            var (service, proxy) = CreateService();
            proxy.Value = "not-json";

            await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => service.GetAsync<Payload>("key"));
        }

        [Fact]
        public async Task SetAsync_SerializesValueAndForwardsExpiration()
        {
            var (service, proxy) = CreateService();
            var expiration = TimeSpan.FromMinutes(5);

            await service.SetAsync("key", new Payload { Name = "Nexus", Count = 3 }, expiration);

            Assert.Equal("key", proxy.LastKey);
            Assert.Equal("{\"Name\":\"Nexus\",\"Count\":3}", proxy.LastValue.ToString());
            Assert.Equal(expiration, proxy.LastExpiration);
        }

        [Fact]
        public async Task RemoveAsync_DeletesRequestedKey()
        {
            var (service, proxy) = CreateService();
            await service.RemoveAsync("key");
            Assert.Equal("key", proxy.DeletedKey);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ExistsAsync_ReturnsRedisResult(bool exists)
        {
            var (service, proxy) = CreateService();
            proxy.Exists = exists;
            Assert.Equal(exists, await service.ExistsAsync("key"));
        }

        private static (RedisCacheService Service, RedisProxy Proxy) CreateService()
        {
            var database = RedisProxy.Create(out var proxy);
            return (new RedisCacheService(new FakeMultiplexer(database)), proxy);
        }

        private sealed class Payload
        {
            public string Name { get; set; } = string.Empty;
            public int Count { get; set; }
        }

        private sealed class FakeMultiplexer : IRedisMultiplexer
        {
            private readonly IDatabase database;
            public FakeMultiplexer(IDatabase database) => this.database = database;
            public IConnectionMultiplexer GetMultiplexer() => throw new NotSupportedException();
            public IDatabase GetDatabase(int db = -1) => this.database;
        }

        private class RedisProxy : DispatchProxy
        {
            public RedisValue Value { get; set; }
            public bool Exists { get; set; }
            public string? LastKey { get; private set; }
            public RedisValue LastValue { get; private set; }
            public TimeSpan? LastExpiration { get; private set; }
            public string? DeletedKey { get; private set; }

            public static IDatabase Create(out RedisProxy proxy)
            {
                var database = DispatchProxy.Create<IDatabase, RedisProxy>();
                proxy = (RedisProxy)(object)database;
                return database;
            }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod == null || args == null) throw new InvalidOperationException();
                var key = args[0]?.ToString() ?? string.Empty;
                switch (targetMethod.Name)
                {
                    case "StringGetAsync":
                        this.LastKey = key;
                        return Task.FromResult(this.Value);
                    case "StringSetAsync":
                        this.LastKey = key;
                        this.LastValue = (RedisValue)args[1]!;
                        this.LastExpiration = (TimeSpan?)args[2];
                        return Task.FromResult(true);
                    case "KeyDeleteAsync":
                        this.DeletedKey = key;
                        return Task.FromResult(true);
                    case "KeyExistsAsync":
                        this.LastKey = key;
                        return Task.FromResult(this.Exists);
                    default:
                        throw new NotSupportedException(targetMethod.Name);
                }
            }
        }
    }
}
