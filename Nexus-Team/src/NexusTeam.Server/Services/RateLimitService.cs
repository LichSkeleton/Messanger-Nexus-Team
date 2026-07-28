namespace NexusTeam.Server.Services
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Options;
    using NexusTeam.Server.Configuration.Options;
    using NexusTeam.Server.Services.Abstractions;
    using Serilog;
    using StackExchange.Redis;

    /// <summary>
    /// Service for rate limiting operations using Redis.
    /// </summary>
    public class RateLimitService : IRateLimitService
    {
        private readonly IRedisMultiplexer redisMultiplexer;
        private readonly RateLimitOptions options;
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RateLimitService"/> class.
        /// </summary>
        /// <param name="redisMultiplexer">Redis multiplexer.</param>
        /// <param name="options">Rate limit options.</param>
        /// <param name="logger">Logger instance.</param>
        public RateLimitService(
            IRedisMultiplexer redisMultiplexer,
            IOptions<RateLimitOptions> options,
            ILogger logger)
        {
            this.redisMultiplexer = redisMultiplexer;
            this.options = options.Value;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public async Task<bool> IsLoginAllowedAsync(string identifier, CancellationToken cancellationToken = default)
        {
            var key = $"ratelimit:login:{identifier}";
            return await this.CheckRateLimitAsync(
                key,
                this.options.LoginMaxAttempts,
                this.options.LoginWindowSeconds,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<bool> IsMessageSendAllowedAsync(string userId, CancellationToken cancellationToken = default)
        {
            var key = $"ratelimit:message:{userId}";
            return await this.CheckRateLimitAsync(
                key,
                this.options.MessageMaxAttempts,
                this.options.MessageWindowSeconds,
                cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> GetLoginRateLimitResetTimeAsync(string identifier, CancellationToken cancellationToken = default)
        {
            var key = $"ratelimit:login:{identifier}";
            return await this.GetResetTimeAsync(key, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<int> GetMessageRateLimitResetTimeAsync(string userId, CancellationToken cancellationToken = default)
        {
            var key = $"ratelimit:message:{userId}";
            return await this.GetResetTimeAsync(key, cancellationToken);
        }

        private async Task<bool> CheckRateLimitAsync(
            string key,
            int maxAttempts,
            int windowSeconds,
            CancellationToken cancellationToken)
        {
            try
            {
                var db = this.redisMultiplexer.GetDatabase();
                var current = await db.StringIncrementAsync(key);

                if (current == 1)
                {
                    await db.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds));
                }

                if (current > maxAttempts)
                {
                    this.logger.Warning("Rate limit exceeded for key {Key}: {Current}/{Max}", key, current, maxAttempts);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error checking rate limit for key {Key}", key);
                return true;
            }
        }

        private async Task<int> GetResetTimeAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                var db = this.redisMultiplexer.GetDatabase();
                var ttl = await db.KeyTimeToLiveAsync(key);
                return ttl.HasValue ? (int)ttl.Value.TotalSeconds : 0;
            }
            catch (Exception ex)
            {
                this.logger.Error(ex, "Error getting reset time for key {Key}", key);
                return 0;
            }
        }
    }
}
