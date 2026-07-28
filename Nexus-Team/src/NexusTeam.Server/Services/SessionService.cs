namespace NexusTeam.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services.Abstractions;
    using NexusTeam.Shared.Abstractions;
    using Serilog;
    using StackExchange.Redis;

    /// <summary>
    /// Redis-based session and message queue service.
    /// </summary>
    public class SessionService : ISessionService
    {
        private readonly IRedisMultiplexer redisMultiplexer;
        private readonly IIdGenerator idGenerator;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionService"/> class.
        /// </summary>
        /// <param name="redisMultiplexer">Redis multiplexer.</param>
        /// <param name="idGenerator">ID generator.</param>
        /// <param name="logger">Logger.</param>
        public SessionService(IRedisMultiplexer redisMultiplexer, IIdGenerator idGenerator, ILogger logger)
        {
            this.redisMultiplexer = redisMultiplexer;
            this.idGenerator = idGenerator;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<string> CreateSessionAsync(string userId, string connectionId, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var resumeToken = this.idGenerator.GenerateId();
            var sessionKey = $"session:{userId}";

            var sessionData = new HashEntry[]
            {
                new HashEntry("connectionId", connectionId),
                new HashEntry("resumeToken", resumeToken),
                new HashEntry("lastHeartbeat", DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
            };

            await db.HashSetAsync(sessionKey, sessionData);
            await db.KeyExpireAsync(sessionKey, TimeSpan.FromHours(1));

            this.logger.Information("Session created for user {UserId} with connection {ConnectionId}", userId, connectionId);

            return resumeToken;
        }

        /// <inheritdoc/>
        public async Task UpdateHeartbeatAsync(string userId, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var sessionKey = $"session:{userId}";

            await db.HashSetAsync(sessionKey, "lastHeartbeat", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            await db.KeyExpireAsync(sessionKey, TimeSpan.FromHours(1));
        }

        /// <inheritdoc/>
        public async Task RemoveSessionAsync(string userId, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var sessionKey = $"session:{userId}";

            await db.KeyDeleteAsync(sessionKey);

            this.logger.Information("Session removed for user {UserId}", userId);
        }

        /// <inheritdoc/>
        public async Task QueueMessageAsync(string userId, string message, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var queueKey = $"queue:{userId}";

            await db.ListRightPushAsync(queueKey, message);
            await db.KeyExpireAsync(queueKey, TimeSpan.FromDays(7));

            this.logger.Debug("Message queued for user {UserId}", userId);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<string>> GetQueuedMessagesAsync(string userId, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var queueKey = $"queue:{userId}";

            var messages = await db.ListRangeAsync(queueKey);
            return messages.Select(m => m.ToString());
        }

        /// <inheritdoc/>
        public async Task ClearMessageQueueAsync(string userId, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var queueKey = $"queue:{userId}";

            await db.KeyDeleteAsync(queueKey);

            this.logger.Debug("Message queue cleared for user {UserId}", userId);
        }

        /// <inheritdoc/>
        public async Task<bool> HasActiveSessionAsync(string userId, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var sessionKey = $"session:{userId}";

            return await db.KeyExistsAsync(sessionKey);
        }
    }
}
