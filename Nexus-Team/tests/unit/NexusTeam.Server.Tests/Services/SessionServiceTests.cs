namespace NexusTeam.Server.Tests.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using Serilog;
    using StackExchange.Redis;
    using Xunit;

    public class SessionServiceTests
    {
        [Fact]
        public async Task CreateSessionAsync_ReturnsAndStoresGeneratedResumeToken()
        {
            var (service, database, generator) = CreateService("resume-token-123");

            var token = await service.CreateSessionAsync("user-1", "connection-9");

            Assert.Equal("resume-token-123", token);
            Assert.Equal(1, generator.Calls);
            Assert.Equal("connection-9", database.Hashes["session:user-1"]["connectionId"]);
            Assert.Equal("resume-token-123", database.Hashes["session:user-1"]["resumeToken"]);
        }

        [Fact]
        public async Task CreateSessionAsync_StoresCurrentUnixHeartbeatAndOneHourTtl()
        {
            var (service, database, _) = CreateService();
            var before = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            await service.CreateSessionAsync("user-1", "connection-9");

            var after = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var heartbeat = long.Parse(database.Hashes["session:user-1"]["lastHeartbeat"]);
            Assert.InRange(heartbeat, before, after);
            Assert.Equal(TimeSpan.FromHours(1), database.Expirations["session:user-1"]);
        }

        [Fact]
        public async Task UpdateHeartbeatAsync_ReplacesHeartbeatAndRefreshesTtl()
        {
            var (service, database, _) = CreateService();
            await service.CreateSessionAsync("user-1", "connection-9");
            database.Hashes["session:user-1"]["lastHeartbeat"] = "0";
            database.Expirations["session:user-1"] = TimeSpan.FromMinutes(2);

            await service.UpdateHeartbeatAsync("user-1");

            Assert.NotEqual("0", database.Hashes["session:user-1"]["lastHeartbeat"]);
            Assert.Equal(TimeSpan.FromHours(1), database.Expirations["session:user-1"]);
        }

        [Fact]
        public async Task HasActiveSessionAsync_ReflectsCreateAndRemoveLifecycle()
        {
            var (service, _, _) = CreateService();

            var before = await service.HasActiveSessionAsync("user-1");
            await service.CreateSessionAsync("user-1", "connection-9");
            var active = await service.HasActiveSessionAsync("user-1");
            await service.RemoveSessionAsync("user-1");
            var removed = await service.HasActiveSessionAsync("user-1");

            Assert.False(before);
            Assert.True(active);
            Assert.False(removed);
        }

        [Fact]
        public async Task QueueMessageAsync_AppendsMessagesInFifoOrder()
        {
            var (service, _, _) = CreateService();

            await service.QueueMessageAsync("user-1", "message-1");
            await service.QueueMessageAsync("user-1", "message-2");
            await service.QueueMessageAsync("user-1", "message-3");
            var messages = (await service.GetQueuedMessagesAsync("user-1")).ToList();

            Assert.Equal(new[] { "message-1", "message-2", "message-3" }, messages);
        }

        [Fact]
        public async Task QueueMessageAsync_SetsSevenDayExpiration()
        {
            var (service, database, _) = CreateService();

            await service.QueueMessageAsync("user-1", "message-1");

            Assert.Equal(TimeSpan.FromDays(7), database.Expirations["queue:user-1"]);
        }

        [Fact]
        public async Task GetQueuedMessagesAsync_WhenQueueIsMissing_ReturnsEmptyCollection()
        {
            var (service, _, _) = CreateService();

            var messages = await service.GetQueuedMessagesAsync("user-1");

            Assert.Empty(messages);
        }

        [Fact]
        public async Task ClearMessageQueueAsync_RemovesMessagesAndExpiration()
        {
            var (service, database, _) = CreateService();
            await service.QueueMessageAsync("user-1", "message-1");

            await service.ClearMessageQueueAsync("user-1");
            var messages = await service.GetQueuedMessagesAsync("user-1");

            Assert.Empty(messages);
            Assert.False(database.Expirations.ContainsKey("queue:user-1"));
        }

        [Fact]
        public async Task SessionAndQueue_ForDifferentUsersRemainIndependent()
        {
            var (service, _, _) = CreateService();
            await service.CreateSessionAsync("user-1", "connection-1");
            await service.QueueMessageAsync("user-1", "message-for-one");
            await service.QueueMessageAsync("user-2", "message-for-two");

            var userOneMessages = await service.GetQueuedMessagesAsync("user-1");
            var userTwoMessages = await service.GetQueuedMessagesAsync("user-2");

            Assert.True(await service.HasActiveSessionAsync("user-1"));
            Assert.False(await service.HasActiveSessionAsync("user-2"));
            Assert.Equal(new[] { "message-for-one" }, userOneMessages);
            Assert.Equal(new[] { "message-for-two" }, userTwoMessages);
        }

        private static (SessionService Service, SessionDatabaseProxy Database, FakeIdGenerator Generator)
            CreateService(string id = "resume-token")
        {
            var database = SessionDatabaseProxy.Create(out var proxy);
            var multiplexer = new FakeRedisMultiplexer(database);
            var generator = new FakeIdGenerator(id);
            var logger = new LoggerConfiguration().CreateLogger();
            return (new SessionService(multiplexer, generator, logger), proxy, generator);
        }

        private sealed class FakeIdGenerator : IIdGenerator
        {
            private readonly string id;

            public FakeIdGenerator(string id)
            {
                this.id = id;
            }

            public int Calls { get; private set; }

            public string GenerateId()
            {
                this.Calls++;
                return this.id;
            }
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

        private class SessionDatabaseProxy : DispatchProxy
        {
            public SessionDatabaseProxy()
            {
            }

            public Dictionary<string, Dictionary<string, string>> Hashes { get; }
                = new Dictionary<string, Dictionary<string, string>>();

            public Dictionary<string, List<string>> Lists { get; }
                = new Dictionary<string, List<string>>();

            public Dictionary<string, TimeSpan> Expirations { get; }
                = new Dictionary<string, TimeSpan>();

            public static IDatabase Create(out SessionDatabaseProxy proxy)
            {
                var database = DispatchProxy.Create<IDatabase, SessionDatabaseProxy>();
                proxy = (SessionDatabaseProxy)(object)database;
                return database;
            }

            protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            {
                if (targetMethod == null || args == null)
                {
                    throw new InvalidOperationException("Redis proxy received an invalid invocation.");
                }

                var key = args[0]?.ToString() ?? string.Empty;
                switch (targetMethod.Name)
                {
                    case "HashSetAsync":
                        return this.HandleHashSet(key, args);
                    case "KeyExpireAsync":
                        this.Expirations[key] = (TimeSpan?)args[1] ?? TimeSpan.Zero;
                        return Task.FromResult(true);
                    case "KeyDeleteAsync":
                        var removed = this.Hashes.Remove(key) | this.Lists.Remove(key);
                        this.Expirations.Remove(key);
                        return Task.FromResult(removed);
                    case "KeyExistsAsync":
                        return Task.FromResult(this.Hashes.ContainsKey(key) || this.Lists.ContainsKey(key));
                    case "ListRightPushAsync":
                        if (!this.Lists.TryGetValue(key, out var list))
                        {
                            list = new List<string>();
                            this.Lists[key] = list;
                        }

                        list.Add(args[1]?.ToString() ?? string.Empty);
                        return Task.FromResult((long)list.Count);
                    case "ListRangeAsync":
                        var values = this.Lists.TryGetValue(key, out var stored)
                            ? stored.Select(value => (RedisValue)value).ToArray()
                            : Array.Empty<RedisValue>();
                        return Task.FromResult(values);
                    default:
                        throw new NotSupportedException(
                            $"Redis method {targetMethod.Name} is not configured for this test.");
                }
            }

            private object HandleHashSet(string key, object?[] args)
            {
                if (!this.Hashes.TryGetValue(key, out var hash))
                {
                    hash = new Dictionary<string, string>();
                    this.Hashes[key] = hash;
                }

                if (args[1] is HashEntry[] entries)
                {
                    foreach (var entry in entries)
                    {
                        hash[entry.Name.ToString()] = entry.Value.ToString();
                    }

                    return Task.CompletedTask;
                }

                hash[args[1]?.ToString() ?? string.Empty] = args[2]?.ToString() ?? string.Empty;
                return Task.FromResult(true);
            }
        }
    }
}
