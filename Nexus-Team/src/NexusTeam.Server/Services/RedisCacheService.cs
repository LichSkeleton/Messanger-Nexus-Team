namespace NexusTeam.Server.Services
{
    using System;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using NexusTeam.Server.Services.Abstractions;

    /// <summary>
    /// Redis-based implementation of the cache service.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IRedisMultiplexer redisMultiplexer;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCacheService"/> class.
        /// </summary>
        /// <param name="redisMultiplexer">The Redis multiplexer.</param>
        public RedisCacheService(IRedisMultiplexer redisMultiplexer)
        {
            this.redisMultiplexer = redisMultiplexer;
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(value.ToString());
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            var serialized = JsonSerializer.Serialize(value);
            await db.StringSetAsync(key, serialized, expiration);
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(key);
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        {
            var db = this.redisMultiplexer.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
    }
}
