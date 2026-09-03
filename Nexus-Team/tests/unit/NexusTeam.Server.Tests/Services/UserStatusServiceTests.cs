namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Enums;
    using Serilog;
    using StackExchange.Redis;
    using Xunit;

    public class UserStatusServiceTests
    {
        [Fact]
        public async Task GetStatusAsync_WhenValueIsMissing_ReturnsOffline()
        {
            var (service, database) = CreateService();

            var result = await service.GetStatusAsync("user-1");

            Assert.Equal(UserStatus.Offline, result);
            Assert.Equal("NexusTeam:user:status:user-1", database.LastReadKey);
        }

        [Theory]
        [InlineData(UserStatus.Offline)]
        [InlineData(UserStatus.Online)]
        [InlineData(UserStatus.Away)]
        [InlineData(UserStatus.DoNotDisturb)]
        [InlineData(UserStatus.Invisible)]
        public async Task GetStatusAsync_WithDefinedValue_ReturnsStoredStatus(UserStatus status)
        {
            var (service, database) = CreateService();
            database.Values["NexusTeam:user:status:user-1"] = ((int)status).ToString();

            var result = await service.GetStatusAsync("user-1");

            Assert.Equal(status, result);
        }

        [Theory]
        [InlineData("not-a-number")]
        [InlineData("999")]
        [InlineData("-1")]
        public async Task GetStatusAsync_WithInvalidValue_ReturnsOffline(string storedValue)
        {
            var (service, database) = CreateService();
            database.Values["NexusTeam:user:status:user-1"] = storedValue;

            var result = await service.GetStatusAsync("user-1");

            Assert.Equal(UserStatus.Offline, result);
        }

        [Fact]
        public async Task GetPublicStatusAsync_WhenInvisible_ReturnsOffline()
        {
            var (service, database) = CreateService();
            database.Values["NexusTeam:user:status:user-1"] = ((int)UserStatus.Invisible).ToString();

            var result = await service.GetPublicStatusAsync("user-1");

            Assert.Equal(UserStatus.Offline, result);
        }

        [Fact]
        public async Task GetPublicStatusAsync_WhenVisible_PreservesStatus()
        {
            var (service, database) = CreateService();
            database.Values["NexusTeam:user:status:user-1"] = ((int)UserStatus.Away).ToString();

            var result = await service.GetPublicStatusAsync("user-1");

            Assert.Equal(UserStatus.Away, result);
        }

        [Fact]
        public async Task SetStatusAsync_WritesNumericStatusWithOneHourExpiration()
        {
            var (service, database) = CreateService();

            await service.SetStatusAsync("user-1", UserStatus.DoNotDisturb);

            Assert.Equal("NexusTeam:user:status:user-1", database.LastWriteKey);
            Assert.Equal("3", database.LastWriteValue);
            Assert.Equal(TimeSpan.FromHours(1), database.LastExpiration);
        }

        [Fact]
        public async Task SetInvisiblePreferenceAsync_WhenEnabled_WritesPersistentFlag()
        {
            var (service, database) = CreateService();

            await service.SetInvisiblePreferenceAsync("user-1", true);

            Assert.Equal("NexusTeam:user:invisible:user-1", database.LastWriteKey);
            Assert.Equal("1", database.LastWriteValue);
            Assert.Null(database.LastExpiration);
        }

        [Fact]
        public async Task GetInvisiblePreferenceAsync_OnlyOneMeansEnabled()
        {
            var (service, database) = CreateService();
            database.Values["NexusTeam:user:invisible:user-1"] = "1";

            var enabled = await service.GetInvisiblePreferenceAsync("user-1");
            database.Values["NexusTeam:user:invisible:user-1"] = "true";
            var otherValue = await service.GetInvisiblePreferenceAsync("user-1");

            Assert.True(enabled);
            Assert.False(otherValue);
        }

        [Fact]
        public async Task SetInvisiblePreferenceAsync_WhenDisabled_DeletesFlag()
        {
            var (service, database) = CreateService();
            database.Values["NexusTeam:user:invisible:user-1"] = "1";

            await service.SetInvisiblePreferenceAsync("user-1", false);

            Assert.Equal("NexusTeam:user:invisible:user-1", database.LastDeletedKey);
            Assert.False(database.Values.ContainsKey("NexusTeam:user:invisible:user-1"));
        }

        [Fact]
        public async Task RemoveStatusAsync_DeletesStatusKey()
        {
            var (service, database) = CreateService();
            database.Values["NexusTeam:user:status:user-1"] = "1";

            await service.RemoveStatusAsync("user-1");

            Assert.Equal("NexusTeam:user:status:user-1", database.LastDeletedKey);
            Assert.False(database.Values.ContainsKey("NexusTeam:user:status:user-1"));
        }

        [Fact]
        public async Task GetStatusAsync_WhenRedisFails_ReturnsOffline()
        {
            var (service, database) = CreateService();
            database.ExceptionToThrow = CreateRedisException();

            var result = await service.GetStatusAsync("user-1");

            Assert.Equal(UserStatus.Offline, result);
        }

        [Fact]
        public async Task GetInvisiblePreferenceAsync_WhenRedisFails_ReturnsFalse()
        {
            var (service, database) = CreateService();
            database.ExceptionToThrow = CreateRedisException();

            var result = await service.GetInvisiblePreferenceAsync("user-1");

            Assert.False(result);
        }

        [Theory]
        [InlineData("set-status")]
        [InlineData("set-invisible")]
        [InlineData("remove-status")]
        public async Task WriteOperation_WhenRedisFails_PropagatesException(string operation)
        {
            var (service, database) = CreateService();
            database.ExceptionToThrow = CreateRedisException();

            var exception = await Record.ExceptionAsync(() => operation switch
            {
                "set-status" => service.SetStatusAsync("user-1", UserStatus.Online),
                "set-invisible" => service.SetInvisiblePreferenceAsync("user-1", true),
                "remove-status" => service.RemoveStatusAsync("user-1"),
                _ => throw new Xunit.Sdk.XunitException($"Unknown operation: {operation}"),
            });

            Assert.IsType<RedisConnectionException>(exception);
        }

        private static (UserStatusService Service, StatusDatabaseProxy Database) CreateService()
        {
            var database = StatusDatabaseProxy.Create(out var proxy);
            var multiplexer = new FakeRedisMultiplexer(database);
            var logger = new LoggerConfiguration().CreateLogger();
            return (new UserStatusService(multiplexer, logger), proxy);
        }

        private static RedisConnectionException CreateRedisException()
        {
            return new RedisConnectionException(
                ConnectionFailureType.UnableToConnect,
                "Redis unavailable");
        }

        private sealed class FakeRedisMultiplexer : IRedisMultiplexer
        {
            private readonly IDatabase database;

            public FakeRedisMultiplexer(IDatabase database)
            {
                this.database = database;
            }

            public IConnectionMultiplexer GetMultiplexer() => throw new NotSupportedException();

            public IDatabase GetDatabase(int db = -1) => this.database;
        }

        private class StatusDatabaseProxy : DispatchProxy
        {
            public StatusDatabaseProxy()
            {
            }

            public Dictionary<string, string> Values { get; } = new Dictionary<string, string>();

            public string? LastReadKey { get; private set; }

            public string? LastWriteKey { get; private set; }

            public string? LastWriteValue { get; private set; }

            public TimeSpan? LastExpiration { get; private set; }

            public string? LastDeletedKey { get; private set; }

            public Exception? ExceptionToThrow { get; set; }

            public static IDatabase Create(out StatusDatabaseProxy proxy)
            {
                var database = DispatchProxy.Create<IDatabase, StatusDatabaseProxy>();
                proxy = (StatusDatabaseProxy)(object)database;
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
                    case "StringGetAsync":
                        this.LastReadKey = key;
                        RedisValue value = this.Values.TryGetValue(key, out var stored)
                            ? stored
                            : RedisValue.Null;
                        return Task.FromResult(value);
                    case "StringSetAsync":
                        this.LastWriteKey = key;
                        this.LastWriteValue = args[1]?.ToString();
                        this.LastExpiration = (TimeSpan?)args[2];
                        this.Values[key] = this.LastWriteValue ?? string.Empty;
                        return Task.FromResult(true);
                    case "KeyDeleteAsync":
                        this.LastDeletedKey = key;
                        return Task.FromResult(this.Values.Remove(key));
                    default:
                        throw new NotSupportedException(
                            $"Redis method {targetMethod.Name} is not configured for this test.");
                }
            }
        }
    }
}
